using ResourceUtilityLib.Logging;
namespace ResourceUtilityLib
{
    /// <summary>
    /// Calculates hashes used to identify files within resource files.
    /// </summary>
    public class HashCalculator
    {
        /// <summary>
        /// Calculates a "CRC" hash based on the filename, including the extension.
        /// </summary>
        /// <remarks>
        /// The original hash function adds a NULL byte to the end of the string, so we do that here for compatibility.
        /// </remarks>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static uint HashCRC(string filename)
        {
            string[] parts = filename.Split('\\');
            string name = parts[^1] + "\0"; // add the NULL byte for compatibility
            LogHelper.Debug("Getting CRC hash of {name}", parts[^1]);

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

            uint crc = ((0x00000000 | (accumXOR) << 16) | (ushort)(accumCRC & 0xFFFF));
            LogHelper.Verbose("Calculated CRC {crc}", crc.ToString("X"));

            return crc;
        }

        /// <summary>
        /// Calculates a hash based on the ID in the filename, not including the extension.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static uint HashID(string filename)
        {
            string[] parts = filename.Split('\\');
            string name = parts[^1];
            LogHelper.Debug("Getting ID hash of {name}", parts[^1]);

            uint result = 0;

            bool found_digit = false;
            foreach (char c in name)
            {
                if (!Char.IsDigit(c))
                {
                    if (found_digit)
                    {
                        break;
                    }
                    continue;
                }
                found_digit = true;
                result = (result * 10) + (uint)(c - '0');
            }

            LogHelper.Verbose("Calculated ID {result}", result);

            return result;
        }
    }
}
