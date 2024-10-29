using System.Text;

namespace ResourceUtilityLib
{
    public enum HashAlgorithm
    {
        HashCrc,
        HashId
    }

    enum CompressionTypes
    {
        NoCompression,
        RLECompression, // Not supported by resutil 4, but needs to be here so LZSS is 2.
        LZSSCompression
    }

    /// <summary>
    /// Describes a directory entry in the resource file.
    /// </summary>
    public struct DirectoryEntry
    {
        public uint hash;
        public uint offset;
        public byte extension;
        public char[] filename;
    }

    /// <summary>
    /// Describes the header of a resource within the resource file.
    /// </summary>
    public struct ResourceHeader
    {
        public uint startcode; // "RSRC"
        public uint cbChunk;
        public uint cbCompressedData;
        public uint cbUncompressedData;
        public uint hash;
        public byte flags;
        public byte compressionCode;
        public byte extension;
        public char[] filename;
    }

    /// <summary>
    /// Manages a resource file.
    /// </summary>
    public class ResourceUtility : IDisposable
    {
        private static readonly int max_resource_name = 13;
        private static readonly string[] compression_type = ["not  compressed", "RLE  compressed", "LZSS compressed"];
        private static readonly string[] supported_extensions = ["", "PCX", "FLC", "WAV"];

        private readonly Int32 resutil_version = 0x00000400;
        private readonly uint resource_start_code = 1129468754; // "RSRC"
        private readonly uint max_resource_size = 0x7FFFFFFF;
        private readonly uint end_of_header = 12;
        private readonly uint size_of_rheader = 36;
        private readonly uint size_of_direntry = 22;

        private bool compress = true;
        private bool rotate = false;
        private bool restore = false;

        private uint file_version;
        private uint directory;
        private uint resources;
        private HashAlgorithm hash_alg = HashAlgorithm.HashCrc;

        private DirectoryEntry[] dirEntries = [];
        private readonly BinaryReader resource_file;

        // Constructors

        /// <summary>
        /// Loads the file and checks the file header and file index.
        /// </summary>
        /// <param name="filePath">The path to the resource file</param>
        public ResourceUtility(string filePath) : this(File.Open(filePath, FileMode.OpenOrCreate)) { }

        /// <summary>
        /// Loads the file and checks the file header and file index.
        /// </summary>
        /// <param name="fileStream">The stream of the resource file.</param>
        public ResourceUtility(Stream fileStream)
        {
            file_version = (uint)resutil_version;
            directory = end_of_header;
            resources = 0;
            resource_file = new BinaryReader(fileStream, Encoding.UTF8, false);

            if (resource_file.BaseStream.Length > 0)
            {
                try
                {

                    LoadFileHeader();
                    LoadDirectory();
                }
                catch (Exception ex)
                {
                    if (ex is UnsupportedVersionException || ex is IndexOutOfRangeException)
                    {
                        throw;
                    }
                    throw new InvalidResourceException("A data read exception occured while loading the resource file.");
                }
            }
        }

        // Initializers

        /// <summary>
        /// Loads the file header for a resource file and performs some sanity checks.
        /// </summary>
        /// <exception cref="UnsupportedVersionException"></exception>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public void LoadFileHeader()
        {
            resource_file.BaseStream.Position = 0;

            file_version = resource_file.ReadUInt32();
            if (file_version != resutil_version)
            {
                throw new UnsupportedVersionException(file_version);
            }
            directory = resource_file.ReadUInt32();
            if (directory > resource_file.BaseStream.Length)
            {
                throw new IndexOutOfRangeException();
            }

            resources = resource_file.ReadUInt32();
        }

        /// <summary>
        /// Saves the file header for a resource file.
        /// </summary>
        public void SaveFileHeader()
        {
            BinaryWriter resfile = new BinaryWriter(resource_file.BaseStream, Encoding.UTF8, true);
            resfile.BaseStream.Position = 0;
            resfile.Write((UInt32)file_version);
            resfile.Write((UInt32)directory);
            resfile.Write((UInt32)resources);
            resfile.Flush();
        }

        /// <summary>
        /// Loads the file index from the end of a resource file and performs some sanity checks.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public void LoadDirectory()
        {
            resource_file.BaseStream.Position = directory;

            dirEntries = new DirectoryEntry[resources];
            for (int i = 0; i < resources; i++)
            {
                DirectoryEntry entry;
                entry.hash = resource_file.ReadUInt32();
                entry.offset = resource_file.ReadUInt32();
                if (entry.offset > resource_file.BaseStream.Length)
                {
                    throw new IndexOutOfRangeException();
                }
                entry.extension = resource_file.ReadByte();
                entry.filename = resource_file.ReadChars(max_resource_name);

                dirEntries[i] = entry;
            }
        }

        /// <summary>
        /// Saves the file index from the end of a resource file.
        /// </summary>
        public void SaveDirectory()
        {
            BinaryWriter resfile = new BinaryWriter(resource_file.BaseStream, Encoding.UTF8, true);
            resfile.BaseStream.Position = directory;
            for (int i = 0; i < resources; i++)
            {
                resfile.Write((UInt32)dirEntries[i].hash);
                resfile.Write((UInt32)dirEntries[i].offset);
                resfile.Write((byte)dirEntries[i].extension);
                resfile.Write(dirEntries[i].filename);
            }
            resfile.Flush();
        }

        // Settings management



        /// <summary>
        /// Sets the current hashing algorithm to use CRC.
        /// </summary>
        public void UseCRCHash()
        {
            hash_alg = HashAlgorithm.HashCrc;
        }

        /// <summary>
        /// Sets the current hashing algorithm to use IDs.
        /// </summary>
        public void UseIDHash()
        {
            hash_alg = HashAlgorithm.HashId;
        }

        /// <summary>
        /// Sets the current hashing algorithm to use IDs.
        /// </summary>
        public HashAlgorithm GetHashType()
        {
            return hash_alg;
        }

        /// <summary>
        /// Enables compressing files.
        /// </summary>
        public void EnableCompression()
        {
            compress = true;
        }

        /// <summary>
        /// Disables compressing files.
        /// </summary>
        public void DisableCompression()
        {
            compress = false;
        }

        /// <summary>
        /// Enables rotating PCX files.
        /// </summary>
        public void EnablePCXRotation()
        {
            rotate = true;
        }

        /// <summary>
        /// Disables rotating PCX files.
        /// </summary>
        public void DisablePCXRotation()
        {
            rotate = false;
        }

        /// <summary>
        /// Enables attempts to restore a PCX file using a default palette.
        /// </summary>
        public void RestorePCX()
        {
            restore = true;
        }

        /// <summary>
        /// Disables attempts to restore a PCX file and writes the bitmap format to file instead.
        /// </summary>
        public void RetainBitmap()
        {
            restore = false;
        }

        /// <summary>
        /// Return the value of the restore setting.
        /// </summary>
        /// <returns>The restore setting's value.</returns>
        public bool GetRestoreSetting()
        {
            return restore;
        }

        // Utility functions

        /// <summary>
        /// Converts a character array to a string to simplify using it.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string CharArrayToString(char[] str)
        {
            return (new string(str, 0, str.Length)).Replace("\0", "");
        }

        /// <summary>
        /// Converts a string to a character array with null padding for writing to a resource file.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static char[] StringToCharArray(string str)
        {
            return str.PadRight(max_resource_name, '\0').ToCharArray();
        }

        /// <summary>
        /// Gets the resource file version as an integer.
        /// </summary>
        /// <returns></returns>
        public uint FileVersion()
        {
            return (file_version >> 8);
        }

        /// <summary>
        /// Gets the number of resources contained in a resource file.
        /// </summary>
        /// <returns></returns>
        public uint Count()
        {
            return resources;
        }

        /// <summary>
        /// Adds a new directory entry to the list.
        /// </summary>
        /// <param name="header">The metadata for the resource to be added.</param
        public void AddDirectoryEntry(ResourceHeader header)
        {
            Array.Resize<DirectoryEntry>(ref dirEntries, (int)resources + 1);

            dirEntries[(int)resources] = new DirectoryEntry();
            dirEntries[(int)resources].hash = header.hash;
            dirEntries[(int)resources].extension = header.extension;
            dirEntries[(int)resources].filename = header.filename;
            dirEntries[(int)resources].offset = directory;

            resources++;
            directory += header.cbChunk;
        }

        /// <summary>
        /// Loads the header for a resource contained in a resource file and performs some sanity checks.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        /// <exception cref="InvalidResourceException"></exception>
        public ResourceHeader LoadResourceHeader(uint offset)
        {
            resource_file.BaseStream.Position = offset;

            ResourceHeader header;

            try
            {
                header.startcode = resource_file.ReadUInt32();
                if (header.startcode != resource_start_code)
                {
                    throw new InvalidResourceException("Invalid resource start code.");
                }

                header.cbChunk = resource_file.ReadUInt32();
                header.cbCompressedData = resource_file.ReadUInt32();
                header.cbUncompressedData = resource_file.ReadUInt32();
                if (header.cbCompressedData > header.cbUncompressedData)
                {
                    throw new InvalidResourceException("The resource compressed data is larger than uncompressed data.");
                }
                header.hash = resource_file.ReadUInt32();
                header.flags = resource_file.ReadByte();
                header.compressionCode = resource_file.ReadByte();
                header.extension = resource_file.ReadByte();
                header.filename = resource_file.ReadChars(max_resource_name);
            }
            catch (Exception ex)
            {
                if (ex is InvalidResourceException)
                {
                    throw;
                }
                else
                {
                    throw new InvalidResourceException("A data read exception occured while reading a resource.", ex);
                }
            }

            return header;
        }

        /// <summary>
        /// Saves a resource metadata to the resource file.
        /// </summary>
        /// <param name="header">The metadata to write.</param>
        /// <param name="offset">The offset in the resource file to write at.</param>
        /// <returns></returns>
        public void SaveResourceHeader(ResourceHeader header, uint offset, BinaryWriter? resfile = null)
        {
            if (resfile == null)
            {
                resfile = new BinaryWriter(resource_file.BaseStream, Encoding.UTF8, true);
            }
            resfile.BaseStream.Position = offset;

            resfile.Write((UInt32)header.startcode);
            resfile.Write((UInt32)header.cbChunk);
            resfile.Write((UInt32)header.cbCompressedData);
            resfile.Write((UInt32)header.cbUncompressedData);
            resfile.Write((UInt32)header.hash);
            resfile.Write((byte)header.flags);
            resfile.Write((byte)header.compressionCode);
            resfile.Write((byte)header.extension);
            resfile.Write(header.filename);

            resfile.Flush();
        }

        /// <summary>
        /// Lists the files within the resource file in fast or detailed modes.
        /// </summary>
        /// <param name="verify"></param>
        /// <returns></returns>
        public ResourceHeader[] ListContents(bool verify = false)
        {
            ResourceHeader[] headers = new ResourceHeader[resources];
            if (verify)
            {

                if (file_version != resutil_version)
                {
                    return [];
                }

                uint position = end_of_header;
                for (int i = 0; i < resources; i++)
                {
                    try
                    {
                        headers[i] = LoadResourceHeader(position);
                        position = position + headers[i].cbChunk;
                    }
                    catch (InvalidResourceException)
                    {
                        headers[i] = new ResourceHeader
                        {
                            filename = StringToCharArray("Item " + i + " is invalid")
                        };
                    }

                }
            }
            else
            {
                for (int i = 0; i < resources; i++)
                {
                    headers[i] = new ResourceHeader
                    {
                        filename = dirEntries[i].filename,
                        cbChunk = dirEntries[i].offset
                    };
                }
            }
            return headers;
        }

        /// <summary>
        /// Add a file to the resource file.
        /// </summary>
        /// <param name="file">The file path to add.</param>
        /// <param name="fileStream">A stream of file data to add.</param>
        public void AddFile(string file, Stream? fileStream = null)
        {
            ResourceHeader header = new ResourceHeader();
            string filename = Path.GetFileName(file).ToUpper();
            string extension = Path.GetExtension(filename)[1..];
            if (fileStream == null)
            {
                fileStream = File.Open(file, FileMode.Open);
            }
            BinaryReader read_file = new BinaryReader(fileStream, Encoding.UTF8, false);

            header.flags = 0;
            header.cbUncompressedData = (uint)read_file.BaseStream.Length;
            header.extension = (byte)Array.IndexOf(supported_extensions, extension);
            header.startcode = resource_start_code;

            if (header.cbUncompressedData > max_resource_size)
            {
                throw new Exception(String.Format("File size ({0}) is greater than max ({1}): not adding to file", header.cbUncompressedData, max_resource_size));
            }

            if (filename.Length > max_resource_name)
            {
                header.filename = StringToCharArray(filename[..max_resource_name]);
            }
            else
            {
                header.filename = StringToCharArray(filename);
            }

            if (hash_alg == HashAlgorithm.HashCrc)
            {
                header.hash = HashCalculator.HashCRC(filename);
            }
            else
            {
                header.hash = HashCalculator.HashID(filename);
                header.flags |= 16;
            }

            byte[] uncompressed_data = read_file.ReadBytes((int)read_file.BaseStream.Length);

            if (supported_extensions[header.extension] == "PCX")
            {
                uncompressed_data = ImageHandler.ConvertPCXToBitmap(uncompressed_data, rotate);
                header.cbUncompressedData = (uint)uncompressed_data.Length;
                header.flags |= 1;
                if (rotate)
                {
                    header.flags |= 2;
                }
            }

            using (BinaryWriter resfile = new BinaryWriter(resource_file.BaseStream, Encoding.UTF8, true))
            {
                byte[] compressed_data;
                if (compress)
                {
                    compressed_data = LZSS.Encode(uncompressed_data);
                    header.cbCompressedData = (uint)compressed_data.Length;
                }
                else
                {
                    compressed_data = new byte[0];
                    header.cbCompressedData = header.cbUncompressedData;
                }

                if (header.cbCompressedData >= header.cbUncompressedData)
                {
                    header.compressionCode = (byte)CompressionTypes.NoCompression;
                    header.cbCompressedData = header.cbUncompressedData;
                    header.cbChunk = size_of_rheader + header.cbCompressedData;
                    AddFile(header, uncompressed_data);
                }
                else
                {
                    header.compressionCode = (byte)CompressionTypes.LZSSCompression;
                    header.cbChunk = size_of_rheader + header.cbCompressedData;
                    AddFile(header, compressed_data);
                }
            }
        }

        public void AddFile (ResourceHeader header, byte[] data)
        {
            using (BinaryWriter resfile = new BinaryWriter(resource_file.BaseStream, Encoding.UTF8, true))
            {
                SaveResourceHeader(header, directory);
                resfile.Write(data);
                resfile.Flush();
            }
            AddDirectoryEntry(header);
        }

        /// <summary>
        /// Add one or more files to the resource file based on a file pattern.
        /// </summary>
        /// <param name="file">The file pattern of the files to add.</param>
        public void AddFiles(string file_pattern)
        {
            string[] fileEntries = Directory.GetFiles(Path.GetDirectoryName(file_pattern) ?? ".", Path.GetFileName(file_pattern));

            if (fileEntries.Length == 0)
            {
                throw new FileNotFoundException();
            }

            foreach (string file in fileEntries)
            {
                AddFile(file);
            }

            SaveFileHeader();
            SaveDirectory();
        }

        /// <summary>
        /// Remove a file from the resource file.
        /// </summary>
        /// <param name="filename">The filename to remove.</param>
        public void RemoveFile(string filename)
        {
            RemoveFile(StringToCharArray(filename));
        }

        /// <summary>
        /// Remove a file from the resource file.
        /// </summary>
        /// <param name="filename">The filename to remove.</param>
        public void RemoveFile(char[] filename)
        {
            RemoveFiles(new[] { filename });
        }

        /// <summary>
        /// Remove multiple files from the resource file.
        /// </summary>
        /// <param name="filenames">The array of filenames.</param>
        public void RemoveFiles(string[] filenames)
        {
            char[][] char_names = new char[filenames.Length][];;
            for (int i = 0; i < filenames.Length; i++)
            {
                char_names[i] = StringToCharArray(filenames[i]);
            }
            RemoveFiles(char_names);
        }

        /// <summary>
        /// Remove multiple files from the resource file.
        /// </summary>
        /// <param name="filenames">The array of filenames.</param>

        public void RemoveFiles(char[][] filenames)
        {
            List<uint> item = new();
            uint length = 0;
            Dictionary<uint, bool> hashes = new();
            uint original_directory = directory;
            List<DirectoryEntry> newDir = new();
            for (int i = 0; i < filenames.Length; i++)
            {
                if (hash_alg == HashAlgorithm.HashCrc)
                {
                    hashes.Add(HashCalculator.HashCRC(CharArrayToString(filenames[i])), false);
                }
                else
                {
                    hashes.Add(HashCalculator.HashID(CharArrayToString(filenames[i])), false);
                }
            }

            // Verify the file exists before we start doing any writing.
            uint position = end_of_header;
            for (uint i = 0; i < resources; i++)
            {
                ResourceHeader header = LoadResourceHeader(position);
                position = position + header.cbChunk;

                if (hashes.ContainsKey(header.hash))
                {
                    hashes[header.hash] = true;
                    item.Add(i);
                    length += header.cbChunk + size_of_direntry;
                }
            }
            foreach (bool found in hashes.Values)
            {
                if (!found)
                {
                    directory = original_directory;
                    throw new FileNotFoundException();
                }
            }

            position = end_of_header;
            directory = end_of_header;
            using (BinaryWriter resfile = new BinaryWriter(resource_file.BaseStream, Encoding.UTF8, true))
            {
                for (uint i = 0; i < resources; i++)
                {
                    ResourceHeader header = LoadResourceHeader(position);

                    if (!hashes.ContainsKey(header.hash))
                    {
                        byte[] data = resource_file.ReadBytes((int)header.cbCompressedData);

                        SaveResourceHeader(header, directory, resfile);
                        resfile.Write(data);
                        resfile.Flush();

                        DirectoryEntry newEntry = dirEntries[i];
                        newEntry.offset = directory;
                        newDir.Add(newEntry);
                        directory = directory + header.cbChunk;
                    }
                    position = position + header.cbChunk;
                }
            }

            resource_file.BaseStream.SetLength(resource_file.BaseStream.Length - length);
            dirEntries = newDir.ToArray();
            resources = (uint)newDir.Count;
            SaveDirectory();
            SaveFileHeader();
        }

        /// <summary>
        /// Gets the file header for a resource.
        /// </summary>
        /// <param name="filename">The filename</param>
        /// <returns>The resource header containing pertinent file information.</returns>
        public ResourceHeader GetFileInformation(string filename)
        {
            return GetFileInformation(StringToCharArray(filename));
        }

        /// <summary>
        /// Gets the file header for a resource.
        /// </summary>
        /// <param name="filename">The filename</param>
        /// <returns>The resource header containing pertinent file information.</returns>
        public ResourceHeader GetFileInformation(char[] filename)
        {
            string filename_str = CharArrayToString(filename);
            uint hash = 0;
            if (hash_alg == HashAlgorithm.HashCrc)
            {
                hash = HashCalculator.HashCRC(filename_str);
            }
            else
            {
                hash = HashCalculator.HashID(filename_str);
            }

            uint position = end_of_header;
            for (int i = 0; i < resources; i++)
            {
                ResourceHeader header = LoadResourceHeader(position);

                if (header.hash == hash)
                {
                    return header;
                }

                position = position + header.cbChunk;
            }
            throw new FileNotFoundException();
        }

        /// <summary>
        /// Gets the file offset for a resource.
        /// </summary>
        /// <param name="filename">The filename</param>
        /// <returns>The offset for the file.</returns>
        public uint GetFileOffset(string filename)
        {
            return GetFileOffset(StringToCharArray(filename));
        }

        /// <summary>
        /// Gets the file offset for a resource.
        /// </summary>
        /// <param name="filename">The filename</param>
        /// <returns>The offset for the file.</returns>
        public uint GetFileOffset(char[] filename)
        {
            for (int i = 0; i < resources; i++)
            {
                if (filename.SequenceEqual(dirEntries[i].filename))
                {
                    return dirEntries[i].offset;
                }
            }
            throw new FileNotFoundException();
        }

        /// <summary>
        /// Extracts a file from the resource file based on the filename.
        /// </summary>
        /// <param name="filename">The filename of the resource in the resource file.</param>
        public void ExtractFile(string filename)
        {
            ExtractFile(StringToCharArray(filename));
        }

        /// <summary>
        /// Extracts a file from the resource file based on the file name.
        /// </summary>
        /// <param name="filename">The filename of the resource in the resource file.</param>
        public void ExtractFile(char[] filename)
        {
            ExtractFile(GetFileInformation(filename));
        }

        /// <summary>
        /// Extracts a file from the resource file based on the file offset.
        /// </summary>
        /// <param name="offset">The offset of the resource in the resource file.</param>
        public void ExtractFile(uint offset)
        {
            ExtractFile(LoadResourceHeader(offset));

        }

        /// <summary>
        /// Extracts a file from the resource file based on the file header.
        /// </summary>
        /// <remarks>This function requires that the offset in the resource file already be set.
        /// i.e., it must be run after LoadResourceHeader and before anything changes the offset. This generally shouldn't be an issue.</remarks>
        /// <param name="header">The resource header to get data for.</param>
        public void ExtractFile(ResourceHeader header)
        {
            SaveResourceToFile(CharArrayToString(header.filename), GetResourceData(header));
        }

        /// <summary>
        /// Extracts all files in the resource file.
        /// </summary>
        /// <param name="verify"></param>
        /// <returns></returns>
        public void ExtractAll()
        {
            if (file_version != resutil_version)
            {
                return;
            }

            uint position = end_of_header;
            for (int i = 0; i < resources; i++)
            {
                ResourceHeader header = LoadResourceHeader(position);
                ExtractFile(header);
                position = position + header.cbChunk;
            }
        }

        /// <summary>
        /// Extract resource data from a resource file.
        /// </summary>
        /// <param name="header">The resource header to get data for.</param>
        /// <returns>The resource's data.</returns>
        public byte[] GetResourceData(ResourceHeader header)
        {
            resource_file.BaseStream.Position = GetFileOffset(header.filename) + size_of_rheader;
            byte[] compressed_data = resource_file.ReadBytes((int)header.cbCompressedData);
            byte[] uncompressed_data;
            if ((CompressionTypes)header.compressionCode == CompressionTypes.NoCompression)
            {
                uncompressed_data = compressed_data;
            }
            else if ((CompressionTypes)header.compressionCode == CompressionTypes.LZSSCompression)
            {
                uncompressed_data = LZSS.Decode(compressed_data, header.cbUncompressedData);
            }
            else
            {
                return new byte[0];
            }
            if (restore)
            {
                bool uncompressed = ((header.flags & (byte)1) == (byte)1);
                bool rotated = ((header.flags & (byte)2) == (byte)1);
                if (uncompressed)
                {
                    return ImageHandler.ConvertBitmapToPCX(uncompressed_data);
                }
            }
            return uncompressed_data;

        }

        /// <summary>
        /// Save resource data to disk.
        /// </summary>
        /// <param name="filename">The filename to save to.</param>
        /// <param name="resource">The resource's data.</param>
        public void SaveResourceToFile(string filename, byte[] resource)
        {
            using (BinaryWriter save_file = new BinaryWriter(File.Open(filename, FileMode.Create), Encoding.UTF8, false))
            {
                save_file.Write(resource);
                save_file.Flush();
            }
        }

        /// <summary>
        /// Return the supported extensions.
        /// </summary>
        /// <returns>The supported extensions.</returns>
        public static string[] GetSupportedExtensions()
        {
            return supported_extensions;
        }

        /// <summary>
        /// Return the supported compression types.
        /// </summary>
        /// <returns>The supported compression types.</returns>
        public static string[] GetCompressionTypes()
        {
            return compression_type;
        }

        /// <summary>
        /// Return the compression type used on a particular resource.
        /// </summary>
        /// <param name="header">The resource's header.</param>
        /// <returns>The resource's compression type.</returns>
        public static string GetCompressionType(ResourceHeader header)
        {
            return compression_type[header.compressionCode];
        }

        /// <summary>
        /// Check if a resource is compressed.
        /// </summary>
        /// <param name="header">The resource's header.</param>
        /// <returns>The resource's compression status.</returns>
        public static bool IsCompressed(ResourceHeader header)
        {
            return (header.compressionCode != (int)CompressionTypes.NoCompression);
        }

        /// <summary>
        /// Check if a resource uses ID hashes.
        /// </summary>
        /// <param name="header">The resource's header.</param>
        /// <returns>Whether the resource uses ID hashes.</returns>
        public static bool UsesIDHash(ResourceHeader header)
        {
            return CheckFlag(header.flags, 16);
        }

        /// <summary>
        /// Check if a PCX resource is rotated.
        /// </summary>
        /// <param name="header">The resource's header.</param>
        /// <returns>Whether the resource is rotated.</returns>
        public static bool IsRotated(ResourceHeader header)
        {
            return CheckFlag(header.flags, 2);
        }

        /// <summary>
        /// Check if a PCX resource is pre-compressed.
        /// </summary>
        /// <param name="header">The resource's header.</param>
        /// <returns>Whether the resource is pre-compressed.</returns>
        public static bool IsPCXCompressed(ResourceHeader header)
        {
            return !CheckFlag(header.flags, 1);
        }

        /// <summary>
        /// Check if a resource has a certain flag checked.
        /// </summary>
        /// <param name="bitfield">The flag bitfield.</param>
        /// <param name="value">The flag to check for.</param>
        /// <returns></returns>
        public static bool CheckFlag(byte bitfield, byte value)
        {
            return ((bitfield & value) == value);
        }

        /// <summary>
        /// Dispose of any unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            resource_file.Dispose();
        }
    }
}
