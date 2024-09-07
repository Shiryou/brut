using System.Text;

namespace ResourceUtilityLib
{
    enum HashAlgorithm
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
    public class ResourceUtility
    {
        private readonly Int32 resutil_version = 0x00000400;
        private readonly int max_resource_name = 13;
        private readonly string[] compression_type = ["not  compressed", "RLE  compressed", "LZSS compressed"];
        private readonly string[] supported_extensions = ["", "PCX", "FLC", "WAV"];
        private readonly uint resource_start_code = 1129468754; // "RSRC"
        private readonly uint max_resource_size = 0x7FFFFFFF;
        private readonly uint end_of_header = 12;
        private readonly uint size_of_rheader = 36;

        private bool compress = true;
        private bool rotate = false;

        private uint file_version;
        private uint directory;
        private uint resources;
        private HashAlgorithm hash_alg = HashAlgorithm.HashCrc;

        private DirectoryEntry[] dirEntries = [];
        private readonly BinaryReader resource_file;

        /// <summary>
        /// Loads the file header for a resource file and performs some sanity checks.
        /// </summary>
        /// <exception cref="UnsupportedVersionException"></exception>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public void LoadFileHeader()
        {
            resource_file.BaseStream.Position = 0;

            file_version = resource_file.ReadUInt32();
            if (file_version > resutil_version)
            {
                throw new UnsupportedVersionException();
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
        /// Loads the header for a resource contained in a resource file and performs some sanity checks.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        /// <exception cref="InvalidResourceException"></exception>
        public ResourceHeader LoadResourceHeader(uint offset)
        {
            resource_file.BaseStream.Position = offset;

            ResourceHeader header;

            header.startcode = resource_file.ReadUInt32();
            if (header.startcode != resource_start_code)
            {
                throw new InvalidResourceException();
            }

            header.cbChunk = resource_file.ReadUInt32();
            header.cbCompressedData = resource_file.ReadUInt32();
            header.cbUncompressedData = resource_file.ReadUInt32();
            if (header.cbCompressedData > header.cbUncompressedData)
            {
                throw new InvalidResourceException();
            }
            header.hash = resource_file.ReadUInt32();
            header.flags = resource_file.ReadByte();
            header.compressionCode = resource_file.ReadByte();
            header.extension = resource_file.ReadByte();
            header.filename = resource_file.ReadChars(max_resource_name);

            return header;
        }

        /// <summary>
        /// Saves a resource metadata to the resource file.
        /// </summary>
        /// <param name="header">The metadata to write.</param>
        /// <param name="offset">The offset in the resource file to write at.</param>
        /// <returns></returns>
        public void SaveResourceHeader(ResourceHeader header, uint offset)
        {
            BinaryWriter resfile = new BinaryWriter(resource_file.BaseStream, Encoding.UTF8, true);
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
        /// Converts a character array to a string to simplify using it.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string CharArrayToString(char[] str)
        {
            return (new string(str, 0, str.Length)).Replace("\0", "");
        }

        /// <summary>
        /// Converts a string to a character array with null padding for writing to a resource file.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public char[] StringToCharArray(string str)
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
        /// Loads the file and checks the file header and file index.
        /// </summary>
        /// <param name="filePath"></param>
        public ResourceUtility(string filePath)
        {
            file_version = (uint)resutil_version;
            directory = end_of_header;
            resources = 0;
            resource_file = new BinaryReader(File.Open(filePath, FileMode.OpenOrCreate), Encoding.UTF8, false);

            if (resource_file.BaseStream.Length > 0)
            {
                LoadFileHeader();
                LoadDirectory();
            }
        }

        /// <summary>
        /// Lists the files within the resource file in fast or detailed modes.
        /// </summary>
        /// <param name="verify"></param>
        /// <returns></returns>
        public string[] ListContents(bool verify = false)
        {
            string[] strings = new string[resources];
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
                        ResourceHeader header = LoadResourceHeader(position);
                        strings[i] = String.Format("{0,4} {1,12} {2,6} {3} {4} {5,6}", i, CharArrayToString(header.filename).PadRight(12), header.cbUncompressedData, header.flags, compression_type[header.compressionCode], header.cbCompressedData);
                        position = position + header.cbChunk;
                    }
                    catch (InvalidResourceException)
                    {
                        strings[i] = "Item " + i + " is invalid";
                    }

                }
            }
            else
            {
                for (int i = 0; i < resources; i++)
                {
                    strings[i] = String.Format("{0,4} {1,12} {2,6}", i, CharArrayToString(dirEntries[i].filename).PadRight(12), dirEntries[i].offset);
                }
            }
            return strings;
        }

        /// <summary>
        /// Add a file to the resource file.
        /// </summary>
        /// <param name="file">The file path to add.</param>
        public void AddFile(string file)
        {
            ResourceHeader header = new ResourceHeader();
            string filename = Path.GetFileName(file).ToUpper();
            string extension = Path.GetExtension(filename)[1..];
            BinaryReader read_file = new BinaryReader(File.Open(file, FileMode.Open), Encoding.UTF8, false);

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

            // Handle PCX decompression and rotation.
            // Handle LZSS compression.

            header.compressionCode = (byte)CompressionTypes.NoCompression;
            header.cbCompressedData = header.cbUncompressedData;
            header.cbChunk = size_of_rheader + header.cbCompressedData;

            BinaryWriter resfile = new BinaryWriter(resource_file.BaseStream, Encoding.UTF8, true);
            SaveResourceHeader(header, directory);
            resfile.Write(uncompressed_data);
            resfile.Flush();
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
                ExtractFile(header.filename);
                position = position + header.cbChunk;
            }
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
                if (filename == dirEntries[i].filename)
                {
                    return dirEntries[i].offset;
                }
            }
            throw new FileNotFoundException();
        }

        /// <summary>
        /// Extracts a file from the resource file based on the filename.
        /// </summary>
        /// <param name="filename"></param>
        public void ExtractFile(string filename)
        {
            ExtractFile(StringToCharArray(filename));
        }

        /// <summary>
        /// Extracts a file from the resource file based on the file name.
        /// </summary>
        /// <param name="filename"></param>
        public void ExtractFile(char[] filename)
        {
            ExtractFile(GetFileInformation(filename));
        }

        /// <summary>
        /// Extracts a file from the resource file based on the file offset.
        /// </summary>
        /// <param name="offset"></param>
        public void ExtractFile(uint offset)
        {
            ExtractFile(LoadResourceHeader(offset));

        }

        /// <summary>
        /// Extracts a file from the resource file based on the file header.
        /// </summary>
        /// <remarks>This function requires that the offset in the resource file already be set.
        /// i.e., it must be run after LoadResourceHeader and before anything changes the offset. This generally shouldn't be an issue.</remarks>
        /// <param name="header"></param>
        private void ExtractFile(ResourceHeader header)
        {
            string filename_str = CharArrayToString(header.filename);
            byte[] compressed_data = resource_file.ReadBytes((int)header.cbCompressedData);
            if ((CompressionTypes)header.compressionCode == CompressionTypes.NoCompression)
            {
                using (BinaryWriter save_file = new BinaryWriter(File.Open(filename_str, FileMode.Create), Encoding.UTF8, false))
                {
                    save_file.Write(compressed_data);
                    save_file.Flush();
                }
            }
            else if ((CompressionTypes)header.compressionCode == CompressionTypes.LZSSCompression)
            {
                using (BinaryWriter save_file = new BinaryWriter(File.Open(filename_str, FileMode.Create), Encoding.UTF8, false))
                {
                    save_file.Write(LZSS.Decode(compressed_data, header.cbUncompressedData));
                    save_file.Flush();
                }
            }
            return;
        }
    }
}
