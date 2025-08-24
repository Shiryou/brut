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
    /// 
    /// Note: "Bitmap" in this context refers to a non-standard format used as an intermediary.
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
    public class ImageHandler
    {
        protected readonly BinaryReader data;
        protected readonly BinaryWriter output;
        protected readonly uint pcx_reserved = 128;
        protected readonly uint bitmap_header_length = 10;

        /// <summary>
        /// Initializes the data streams.
        /// </summary>
        /// <param name="input"></param>
        public ImageHandler(byte[] input)
        {
            data = new BinaryReader(new MemoryStream(input), Encoding.UTF8, false);
            output = new BinaryWriter(new MemoryStream(), Encoding.UTF8, false);
        }

        /// <summary>
        /// Converts PCX data to a bitmap
        /// </summary>
        /// <param name="data">PCX data.</param>
        /// <param name="rotate">Whether to rotate the image.</param>
        /// <returns>The bitmap file data as a byte array.</returns>
        public static byte[] ConvertPCXToBitmap(byte[] data, bool rotate = false)
        {
            return new PCX(data).ConvertToBitmap(rotate);
        }

        /// <summary>
        /// Converts bitmap data to a PCX
        /// </summary>
        /// <param name="data">bitmap data.</param>
        /// <param name="rotate">Whether to unrotate the image.</param>
        /// <returns>The PCX file data as a byte array.</returns>

        public static byte[] ConvertBitmapToPCX(byte[] data, bool rotate = false)
        {
            return new Bitmap(data).ConvertToPCX(rotate);
        }
    }

    /// <summary>
    /// Provides functions for PCX conversion to bitmap.
    /// </summary>
    public class PCX : ImageHandler
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
            final_size = decompressed_length + bitmap_header_length; // The VGA palette starts after this, which is apparently ignored.
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
            data.BaseStream.Position = 0;
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
                        while (run_length-- > 0)
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

    /// <summary>
    /// Provides functions for bitmap conversion to PCX.
    /// </summary>
    public class Bitmap : ImageHandler
    {
        private readonly BitmapHeader header;
        private PCXHeader pcx_header;
        private readonly uint decompressed_length; // expected length of bitmap data
        private readonly uint min_run = 2;
        private readonly uint max_run = 63;
        private bool short_width = true;

        /// <summary>
        /// Inititalizes the image and buffer dimensions.
        /// </summary>
        /// <param name="input">PCX data.</param>
        public Bitmap(byte[] input) : base(input)
        {
            header = ReadBitmapHeader();
            decompressed_length = (uint)input.Length - bitmap_header_length;
        }

        /// <summary>
        /// Perform the converstion from PCX to bitmap.
        /// </summary>
        /// <param name="rotate">Whether to rotate the image.</param>
        /// <returns>The PCX image data as a byte array.</returns>
        public byte[] ConvertToPCX(bool derotate)
        {
            if (derotate)
            {
                Derotate();
            }
            else
            {
                Recompress();
            }
            output.Write((byte)0x0C); // palette separator
            output.Write(Convert.FromBase64String("AAD/CwsLExMTGxsbIyMjKysrNzc3Pz8/R0dHT09PV1dXY2Nja2trc3Nze3t7g4ODj4+Pk5OTm5ubo6Ojq6urs7Ozu7u7w8PDy8vLz8/P19fX39/f5+fn7+/v9/f3////IxsvJx83Lyc/NytHPzNTRzdbTz9jV0drX0t3Z1N/b1uHd2OTf2ubh3Ojk3urm4O3o4u/r5PHt5vPw6Pbz6vj17Pr47vz78f/Fxc7GxtHHx9XIyNnKyd3LyuDMy+TNzOjOzezPzu3R0O7T0u/V1PHX1vLZ2PPb2vTd3fbf3/fi4vjk5Prn5/vp6fzs7P3v7//Lws7NwtHQw9TSxNfVxNvYxd7axeHdxuTgx+jix+vlyO7nyPLqyfXtyvjvyvvyy//yzf/z0P/00v/11f/21//32v/33P/43//OwsLRwsLUwsLXw8Paw8Pdw8Pgw8Pjw8Plw8PoxMTrxcXtxsbwyMjyycn1y8v2zMz3zc34zs750ND60dH809P91NT+1dX/19fLxcAOxsARyMAUysAYy8AbzcAezsAh0MAl0sAo1MAr1cAu18Hy2cH12sH43MH73sH/4ML/48L/5sP/6cT/7MT/78X/8sb/9cfLyMLNysLQzMPTz8TW0sXZ1MXc18bf2sfi3Mjl38jo4snr5Mru58vx6sz07c338M748s/69NH899P8+Nj9+d79+uT+++v//fHACMLACsLADMPADsPAEMTAEsTAFMXAFsXAGMbAGsbAHMfAHsfAH8fAIcfAI8jAJcjB58nC6svE7c7G8NDJ8tLM9dXP+NjS+9vLyMXNycbPy8fRzcjUz8nW0crY08va1c3d187f2M/g2tDi3NHk3tPm39To4dXq49bs5dju59nw6dvy7N317t/38eH58+P89uXLx8bNyMfQysjSy8nVzcrXz8va0Mzd0s3f087h1M/j1tDm19Ho2NLq2tPs29Tu3NXx3tfz4dn149v45t366N/86+H/7uT/8OX64cA76MA98MA/+MA//8A+/9////T////"));
            SavePCXHeader();
            return ((MemoryStream)output.BaseStream).ToArray();
        }

        /// <summary>
        /// Write the header of a PCX file.
        /// </summary>
        private void SavePCXHeader()
        {
            output.BaseStream.Position = 0;
            output.Write(0x0801050a);
            output.Write(pcx_header.XOrigin);
            output.Write(pcx_header.YOrigin);
            output.Write((short)(pcx_header.Width - 1));
            output.Write((short)(pcx_header.Height - 1));
            output.Write(0x01e00280);
            output.Write(new byte[2]);
            // write a common palette
            output.Write(Convert.FromBase64String("/wsLCxMTExsbGyMjIysrKzc3Nz8/P0dHR09PT1dXV2NjY2tra3Nzc3t7e4ODgw=="));
            output.Write((byte)0);
            output.Write((byte)1);
            output.Write(pcx_header.LineLength);
        }

        /// <summary>
        /// Read the header of a bitmap file.
        /// </summary>
        /// <returns>The bitmap header</returns>
        private BitmapHeader ReadBitmapHeader()
        {
            data.BaseStream.Position = 0;
            BitmapHeader header = new()
            {
                Width = data.ReadInt16(),
                Height = data.ReadUInt16(),
                Scale = data.ReadInt16(),
                CenterPoint = data.ReadInt16(),
                Type = data.ReadInt16()
            };

            return header;
        }

        /// <summary>
        /// Derotate the PCX data into a bitmap.
        /// </summary>
        private void Derotate()
        {
        }

        /// <summary>
        /// Convert the bitmap data into a PCX without rotating.
        /// </summary>
        private void Recompress()
        {
            pcx_header = new()
            {
                Code = 10,
                XOrigin = 0,
                YOrigin = 0,
                Width = header.Width,
                Height = (short)header.Height,
                LineLength = (ushort)header.Width
            };

            // Check if the last byte of each line is 0 to adjust the width.
            for (int i = 0; i < pcx_header.Height; i++)
            {
                data.BaseStream.Position = (i * pcx_header.Width) + (pcx_header.Width - 1);
                if (data.ReadByte() != 0)
                {
                    short_width = false;
                    break;
                }
            }
            if (short_width)
            {
                pcx_header.Width = (short)(pcx_header.Width - 1);
            }

            // decompress
            data.BaseStream.Position = bitmap_header_length;
            output.BaseStream.Position = pcx_reserved;
            while (data.BaseStream.Position < data.BaseStream.Length)
            {
                //if (data.BaseStream.Position % pcx_header.LineLength == 0)
                //{
                //    data.ReadByte();
                //}
                output.Write(EvaluateRun());
            }
        }

        /// <summary>
        /// Check whether a single byte or a run length/byte pair should be written
        /// </summary>
        private byte[] EvaluateRun()
        {
            long start_pos = data.BaseStream.Position;
            byte start = data.ReadByte();
            int count = 1;
            while (
                data.BaseStream.Position < data.BaseStream.Length &&
                (data.BaseStream.Position - bitmap_header_length) % header.Width != 0 &&
                data.ReadByte() == start &&
                count < max_run)
            {
                count++;
            }

            if (count < min_run)
            {
                count = 1;
            }
            data.BaseStream.Position = start_pos + count;
            // 1 in two most significant bits indicates a run, so any single value greater
            // than 191 must be stored in a byte pair
            if (count == 1 && start < 192)
            {
                return [start];
            }
            return [(byte)(192 + count), start];
        }
    }
}
