using System;
namespace ResourceUtilityLib
{
    /// <summary>
    /// Calculate hashes used to identify files within resource files.
    /// </summary>
    public class HashCalculator
    {
        /// <summary>
        /// Calculate a "CRC" hash based on the filename, including the extension.
        /// </summary>
        /// <remarks>
        /// The original hash function adds a NULL byte to the end of the string, so we do that here for compatibility.
        /// </remarks>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static uint HashCRC(string filename)
        {
            string[] parts = filename.Split('\\');
            string name = parts[parts.Length - 1] + "\0"; // add the NULL byte for compatibility

            uint accumCRC = 0;
            uint accumXOR = 0;

            foreach (char ci in name)
            {
                char c = ci;
                if (c >= 'a' && c <= 'z')
                {
                    c -= (char)('a' - 'A');
                }

                accumXOR = accumXOR ^ c;
                accumCRC = (accumCRC << 6) + ((uint)(c - ' ') & 63);

                for (int i = 5; i >= 0; i--)
                {
                    if ((accumCRC & (1 << (16 + i))) != 0)
                    {
                        accumCRC ^= (uint)(0x1021 << i);
                    }
                }
            }

            return ((0x00000000 | (accumXOR) << 16) | (ushort)(accumCRC & 0xFFFF));
        }

        /// <summary>
        /// Calculate a hash based on the ID in the filename, not including the extension.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static uint HashID(string filename)
        {
            string[] parts = filename.Split('\\');
            string name = parts[parts.Length - 1];

            uint result = 0;

            foreach (char c in name)
            {
                if (!Char.IsDigit(c))
                {
                    continue;
                }
                if (c == '.')
                {
                    break;
                }
                result = (result * 10) + (uint)(c - '0');
            }
            return result;
        }
    }
}

