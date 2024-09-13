using System;
using System.Collections;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Provides a simple API for encoding and decoding.
/// </summary>
public class LZSS
{
    /// <summary>
    /// Encodes data with LZSS.
    /// </summary>
    /// <param name="pData">The byte array to compress.</param>
    /// <returns>The compressed byte array.</returns>
    public static byte[] Encode(byte[] pData)
    {
        return (new LZSSEncoder(pData)).Encode();
    }

    /// <summary>
    /// Decodes data that was previously encoded withg LZSS.
    /// </summary>
    /// <param name="pData">The byte array to decompress.</param>
    /// <param name="myLength">The expected length of the decompressed byte array.</param>
    /// <returns>The decompressed byte array.</returns>
    public static byte[] Decode(byte[] pData, uint myLength)
    {
        return (new LZSSDecoder(pData, myLength)).Decode();
    }
}

/// <summary>
/// Provides LZSS encoding functions.
/// </summary>
public class LZSSEncoder
{
    private readonly uint uncompressed_length;
    private readonly byte[] pInput;
    private byte[] pOutput;

    /// <summary>
    /// Initializes the base data structures.
    /// </summary>
    /// <param name="pData">The byte array to compress.</param>
    public LZSSEncoder(byte[] pData)
    {
        Console.WriteLine("Initializing");
        pInput = pData;
        uncompressed_length = (uint)pInput.Length;
        // We don't know the compressed length, so we'll use the uncompressed length and rebuild the array after we're done.
        pOutput = new byte[uncompressed_length];
        Console.WriteLine(String.Format("{0} bytes of data submitted.", uncompressed_length));
    }

    public byte[] Encode()
    {
        return pInput;
    }

}

/// <summary>
/// Provides LZSS decoding functions.
/// </summary>
public class LZSSDecoder
{
    /// <summary>
    /// States to support emulating the original imperative algorithm design.
    /// </summary>
    enum States
    {
        Start,
        MoveByte,
        Decompress,
        PrepareLoop,
        Done
    }

    private uint compressed_index = 0;
    private uint uncompressed_index = 0;
    private readonly uint uncompressed_length;
    private readonly byte[] pInput;
    private ushort current_word;
    private ushort bitflags;
    private byte[] pOutput;

    /// <summary>
    /// Initializes the base data structures.
    /// </summary>
    /// <param name="pData">The byte array to compress.</param>
    public LZSSDecoder(byte[] pData, uint myLength)
    {
        pInput = pData;
        uncompressed_length = myLength;
        pOutput = new byte[uncompressed_length];
    }

    /// <summary>
    /// Manages the decoding of the input.
    /// </summary>
    /// <returns>Length of output stream.</returns>
    public byte[] Decode()
    {
        States state = States.Start;
        while (state != States.Done)
        {
            switch (state)
            {
                case States.Start:
                    state = Start();
                    break;
                case States.MoveByte:
                    state = MoveByte();
                    break;
                case States.Decompress:
                    state = Decompress();
                    break;
                case States.PrepareLoop:
                    state = PrepareLoop();
                    break;
            }
        }
        return pOutput;
    }

    /// <summary>
    /// Retrieves two bytes from the input array to use as bitflags.
    /// </summary>
    /// <returns>The bitflags.</returns>
    private ushort GetFlags()
    {
        ushort next = (ushort)((pInput[compressed_index]) + (pInput[compressed_index + 1] << 8));
        compressed_index += 2;
        return next;
    }

    /// <summary>
    /// Sets the bitflags and checks whether we move a byte or decompress from the sliding window.
    /// </summary>
    /// <returns>The next step to run.</returns>
    private States Start()
    {
        current_word = GetFlags();
        bitflags = current_word;
        ushort test_msb = (ushort)(bitflags >> 15);
        bitflags = (ushort)((bitflags << 1) | 1);

        if ((test_msb & 1) == 0)
        {
            return States.Decompress;
        }
        return States.MoveByte;
    }

    /// <summary>
    /// Moves a byte directly from the input to the output and decides on the next step.
    /// </summary>
    /// <returns>The next step to run.</returns>
    private States MoveByte()
    {
        pOutput[uncompressed_index++] = pInput[compressed_index++];
        ushort test_msb = (ushort)(bitflags >> 15); // The state of the Carry Flag after the bitshift.
        bitflags <<= 1;
        if (bitflags == 0)
        {
            return States.Start;
        }
        if ((test_msb & 1) == 1)
        {
            return States.MoveByte;
        }
        return States.Decompress;
    }

    /// <summary>
    /// Repeats a previous section of the output to the end of the output and decides on the next step.
    /// </summary>
    /// <returns>The next step to run.</returns>
    private States Decompress()
    {
        current_word = GetFlags();
        if (current_word == 0)
        {
            return States.Done;
        }

        ushort run_length = (ushort)(((current_word >> 10) & 0x3F) + 3);
        current_word = (ushort)((current_word & 0x03FF) + 1);
        uint back_index = uncompressed_index - current_word;
        for (int i = 0; i < run_length; i++)
        {
            pOutput[uncompressed_index++] = pOutput[back_index++];
        }

        return States.PrepareLoop;
    }

    /// <summary>
    /// Checks if the decoding process is done and decides on the next step.
    /// </summary>
    /// <returns>The next step to run.</returns>
    private States PrepareLoop()
    {
        if (uncompressed_index > uncompressed_length)
        {
            return States.Done;
        }
        ushort test_msb = (ushort)(bitflags >> 15); // The state of the Carry Flag after the bitshift.
        bitflags <<= 1;
        if ((test_msb & 1) == 0)
        {
            return States.Decompress;
        }
        if (bitflags == 0)
        {
            return States.Start;
        }
        if ((test_msb & 1) == 1)
        {
            return States.MoveByte;
        }
        return States.Done;
    }
}

