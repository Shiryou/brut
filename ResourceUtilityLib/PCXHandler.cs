using System.Diagnostics;
using System.Text;

namespace ResourceUtilityLib
{
    /// <summary>
    /// Describes the header of a PCX file.
    /// </summary>
    public struct PCXHeader
    {
        public uint Code;
        public short XOrigin;
        public short YOrigin;
        public short Width;
        public short Height;
        public ushort LineLength;
    }

    /// <summary>
    /// Describes the header of a bitmap file.
    /// </summary>
    public struct BitmapHeader
    {
        public short Width;
        public ushort Height;
        public short Scale;
        public short CenterPoint;
        public short Type;
    }

    /// <summary>
    /// Provides functions for PCX handling.
    /// </summary>
    public class PCXHandler
    {
        protected readonly BinaryReader data;
        protected readonly BinaryWriter output;
        protected readonly uint pcx_reserved = 128;
        protected readonly uint bitmap_header_length = 10;

        /// <summary>
        /// Initializes the data streams.
        /// </summary>
        /// <param name="input"></param>
        public PCXHandler(byte[] input)
        {
            data = new BinaryReader(new MemoryStream(input), Encoding.UTF8, false);
            output = new BinaryWriter(new MemoryStream(), Encoding.UTF8, false);
        }

        /// <summary>
        /// Converts PCX data to a bitmap
        /// </summary>
        /// <param name="data">PCX data.</param>
        /// <param name="rotate">Whether to rotate the image.</param>
        /// <returns></returns>
        public static byte[] ConvertToBitmap(byte[] data, bool rotate = false)
        {
            return new PCX(data).ConvertToBitmap(rotate);
        }
    }

    /// <summary>
    /// Provides functions for PCX conversion to bitmap.
    /// </summary>
    public class PCX : PCXHandler
    {
        private readonly PCXHeader header;
        private BitmapHeader bitmap_header;
        private readonly uint decompressed_length; // expected length of bitmap data
        private readonly uint final_size; // expected bytes to write to bitmap (10 bytes of bitmap header)

        /// <summary>
        /// Inititalizes the image and buffer dimensions.
        /// </summary>
        /// <param name="input">PCX data.</param>
        public PCX(byte[] input) : base(input)
        {
            header = ReadPCXHeader();

            header.Width = (short)(header.Width + (short)1 - header.XOrigin);
            header.Height = (short)(header.Height + (short)1 - header.YOrigin);

            data.ReadBytes((int)pcx_reserved);
            uint original_length = (uint)data.BaseStream.Length - pcx_reserved;
            decompressed_length = (uint)header.LineLength * (uint)header.Height;
            final_size = decompressed_length + bitmap_header_length;
            uint buffer_size = (uint)Math.Max(final_size, original_length) + 2048;
        }

        /// <summary>
        /// Perform the converstion from PCX to bitmap.
        /// </summary>
        /// <param name="rotate">Whether to rotate the image.</param>
        /// <returns></returns>
        public byte[] ConvertToBitmap(bool rotate)
        {
            if (rotate)
            {
                Rotate();
            }
            else
            {
                Decompress();
            }
            SaveBitmapHeader();
            return ((MemoryStream)output.BaseStream).ToArray();
        }

        /// <summary>
        /// Read the header of a PCX file.
        /// </summary>
        /// <returns></returns>
        private PCXHeader ReadPCXHeader()
        {
            PCXHeader header = new()
            {
                Code = data.ReadUInt32(),
                XOrigin = data.ReadInt16(),
                YOrigin = data.ReadInt16(),
                Width = data.ReadInt16(),
                Height = data.ReadInt16()
            };
            data.ReadBytes(54);
            header.LineLength = data.ReadUInt16();

            return header;
        }

        /// <summary>
        /// Save the header of a bitmap file.
        /// </summary>
        private void SaveBitmapHeader()
        {
            output.BaseStream.Position = 0;
            output.Write(bitmap_header.Width);
            output.Write(bitmap_header.Height);
            output.Write(bitmap_header.Scale);
            output.Write(bitmap_header.CenterPoint);
            output.Write(bitmap_header.Type);
        }

        /// <summary>
        /// Rotate the PCX data into a bitmap.
        /// </summary>
        private void Rotate()
        {
            PCXHeader new_header = header;
            data.BaseStream.Position = pcx_reserved;

            ulong l, w;
            ulong ll = (ulong)(header.LineLength * header.Height);

            for (ushort j = 0; j < new_header.Height; j++)
            {
                uint loc = bitmap_header_length + j;
                w = l = 0;
                do
                {
                    byte c = data.ReadByte();

                    if ((c & 0xC0) == 0xC0)
                    {
                        ushort run_length = (ushort)(c & 0x3F);
                        c = data.ReadByte();

                        w += run_length;
                        while(run_length-- > 0)
                        {
                            output.BaseStream.Position = (long)(loc + l);
                            output.Write(c);
                            l += (ulong)new_header.Height;
                        }
                    }
                    else
                    {
                        output.BaseStream.Position = (long)(loc + l);
                        output.Write(c);
                        l += (ulong)new_header.Height;
                        w++;
                    }
                }
                while (l < ll);
                if (new_header.Width < new_header.LineLength)
                {
                    output.BaseStream.Position = (long)(loc + l - (ulong)new_header.Height);
                    output.Write((byte)0);
                }
            }

            new_header.Width = new_header.Height;
            new_header.Height = (short)new_header.LineLength;
            bitmap_header = new()
            {
                Width = new_header.Width,
                Height = (ushort)new_header.Height,
                Scale = 5,
                CenterPoint = 0,
                Type = 0x000B
            };
        }

        /// <summary>
        /// Convert the PCX data into a bitmap without rotating.
        /// </summary>
        private void Decompress()
        {
            PCXHeader new_header = header;
            data.BaseStream.Position = pcx_reserved;
            output.BaseStream.Position = bitmap_header_length;

            for (ulong l = 0; l < decompressed_length;)
            {
                byte c = data.ReadByte();
                if ((c & 0xC0) == 0xC0)
                {
                    ushort run_length = (ushort)(c & 0x3F);
                    c = data.ReadByte();
                    l += run_length;
                    for (ushort run_index = 0; run_index < run_length; run_index++)
                    {
                        output.BaseStream.WriteByte(c);
                    }
                }
                else
                {
                    output.BaseStream.WriteByte(c);
                    l++;
                }
            }
            if (new_header.Width < new_header.LineLength)
            {
                ulong w = (ulong)new_header.Width;
                for (ulong h = 0; h < (ulong)new_header.Height; h++, w += new_header.LineLength)
                {
                    output.BaseStream.Position = (long)(bitmap_header_length + w);
                    output.BaseStream.WriteByte(0);
                }
            }
            new_header.Width = (short)new_header.LineLength;
            bitmap_header = new()
            {
                Width = new_header.Width,
                Height = (ushort)new_header.Height,
                Scale = 5,
                CenterPoint = 0,
                Type = 0x000B
            };
        }
    }
}
