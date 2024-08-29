using System.Collections;
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
    /// Structure describing a directory entry in the resource file.
    /// </summary>
    public struct DirectoryEntry
    {
        public uint hash;
        public uint offset;
        public byte extension;
        public char[] filename;
    }

    /// <summary>
    /// Structure describing the header of a resource within the resource file.
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
    /// Class <c>ResourceUtility</c> manages a resource file.
    /// </summary>
    public class ResourceUtility
    {
        private readonly Int32 resutil_version = 0x00000400;
        private readonly int max_resource_name = 13;
        private readonly string[] gszCompressionType = ["not  compressed", "RLE  compressed", "LZSS compressed"];
        private readonly string[] supported_extensions = ["", "PCX", "FLC", "WAV"];
        private readonly uint resource_start_code = 1129468754;
        private readonly uint end_of_header = 12;
        //private uint size_of_rheader = 36;

        private uint file_version;
        private uint directory;
        private uint resources;
        private HashAlgorithm hash_alg = HashAlgorithm.HashId;

        private DirectoryEntry[] dirEntries = [];
        private readonly BinaryReader resource_file;

        /// <summary>
        /// Load the file header for a resource file and perform some sanity checks.
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
        /// Load the file index from the end of a resource file and perform some sanity checks.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public void LoadDirectory()
        {
            long original_position = resource_file.BaseStream.Position;
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
        /// Set the current hashing algorithm to use CRC.
        /// </summary>
        public void useCRCHash()
        {
            hash_alg = HashAlgorithm.HashCrc;
        }

        /// <summary>
        /// Set the current hashing algorithm to use IDs.
        /// </summary>
        public void useIDHash()
        {
            hash_alg = HashAlgorithm.HashId;
        }

        /// <summary>
        /// Load the header for a resource contained withint a resource file and perform some sanity checks.
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
        /// Convert a character array to a string to simplify using it.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string CharArrayToString(char[] str)
        {
            return (new string(str, 0, str.Length)).Replace("\0", "");
        }

        /// <summary>
        /// Convert a string to a character array with null padding for writing to a resource file.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public char[] StringToCharArray(string str)
        {
            return str.PadRight(max_resource_name, '\0').ToCharArray();
        }

        /// <summary>
        /// Get the resource file version as an integer.
        /// </summary>
        /// <returns></returns>
        public uint FileVersion()
        {
            return (file_version >> 8);
        }

        /// <summary>
        /// Get the number of resources contained in a resource file.
        /// </summary>
        /// <returns></returns>
        public uint Count()
        {
            return resources;
        }

        /// <summary>
        /// The constructor loads the file and checks the file header and file index.
        /// </summary>
        /// <param name="filePath"></param>
        public ResourceUtility(string filePath)
        {
            if (File.Exists(filePath))
            {
                resource_file = new BinaryReader(File.Open(filePath, FileMode.Open), Encoding.UTF8, false);

                LoadFileHeader();
                LoadDirectory();
            }
            else
            {
                throw new FileNotFoundException();
            }
        }

        /// <summary>
        /// List the files within the resource file in fast or detailed modes.
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
                        strings[i] = String.Format("{0,4} {1,12} {2,6} {3} {4} {5,6}", i, CharArrayToString(header.filename).PadRight(12), header.cbUncompressedData, header.flags, gszCompressionType[header.compressionCode], header.cbCompressedData);
                        position = position + header.cbChunk;
                    }
                    catch (InvalidResourceException)
                    {
                        strings[i] = "Item " + i + " is invalid";
                    }

                }
            }
            else
            {// Method 2
                for (int i = 0; i < resources; i++)
                {
                    strings[i] = String.Format("{0,4} {1,12} {2,6}", i, CharArrayToString(dirEntries[i].filename).PadRight(12), dirEntries[i].offset);
                }
            }
            return strings;
        }
        public void ExtractAll()
        {
            string[] strings = new string[resources];

            if (file_version != resutil_version)
            {
                return;
            }

            uint position = end_of_header;
            for (int i = 0; i < resources; i++)
            {
                try
                {
                    ResourceHeader header = LoadResourceHeader(position);
                    ExtractFile(header.filename);
                    position = position + header.cbChunk;
                }
                catch (InvalidResourceException)
                {
                    strings[i] = "Item " + i + " is invalid";
                }

            }
        }

        /// <summary>
        /// Extract a file from the resource file.
        /// </summary>
        /// <param name="filename"></param>
        public void ExtractFile(string filename)
        {
            ExtractFile(StringToCharArray(filename));
        }

        /// <summary>
        /// Extract a file from the resource file.
        /// </summary>
        /// <param name="filename"></param>
        public void ExtractFile(char[] filename)
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
                    byte[] compressed_data = resource_file.ReadBytes((int)header.cbCompressedData);
                    if ((CompressionTypes)header.compressionCode == CompressionTypes.NoCompression)
                    {
                        using (BinaryWriter save_file = new BinaryWriter(File.Open(filename_str, FileMode.Create), Encoding.UTF8, false))
                        {
                            save_file.Write(compressed_data);
                        }
                    }
                    else if ((CompressionTypes)header.compressionCode == CompressionTypes.LZSSCompression)
                    {
                        using (BinaryWriter save_file = new BinaryWriter(File.Open(filename_str, FileMode.Create), Encoding.UTF8, false))
                        {
                            save_file.Write(LZSS.Decode(compressed_data, header.cbUncompressedData));
                        }
                    }
                    return;
                }

                position = position + header.cbChunk;
            }
            throw new FileNotFoundException();
        }
    }
}
