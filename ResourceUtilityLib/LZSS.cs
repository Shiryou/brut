using System;
using System.Collections;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// A holder class to provide a simple API for encoding and decoding.
/// </summary>
public class LZSS
{
    /// <summary>
    /// Encode data with LZSS.
    /// </summary>
    /// <param name="pData">The byte array to compress.</param>
    /// <returns>The compressed byte array.</returns>
    public static byte[] Encode(byte[] pData)
    {
        return (new LZSSEncoder(pData)).Encode();
    }

    /// <summary>
    /// Decode data that was previously encoded withg LZSS.
    /// </summary>
    /// <param name="pData">The byte array to decompress.</param>
    /// <param name="myLength">The expected length of the decompressed byte array.</param>
    /// <returns>The decompressed byte array.</returns>
    public static byte[] Decode(byte[] pData, uint myLength)
    {
        return (new LZSSDecoder(pData, myLength)).Decode();
    }
}

public class LZSSEncoder
{
    private readonly uint uncompressed_length;
    private readonly byte[] pInput;
    private byte[] pOutput;

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
        return pOutput;
    }

}

public class LZSSDecoder
{

    enum States
    {
        Start,
        MoveUnique,
        DoRun,
        NextBitFlag,
        Done
    }

    private uint compressed_index = 0;
    private uint uncompressed_index = 0;
    private readonly uint uncompressed_length;
    private readonly byte[] pInput;
    private ushort current_word;
    private ushort bitflags;
    private byte[] pOutput;

    public LZSSDecoder (byte[] pData, uint myLength )
    {
        pInput = pData;
        uncompressed_length = myLength;
        pOutput = new byte[uncompressed_length];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>Length of output stream</returns>
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
                case States.MoveUnique:
                    state = MoveUnique();
                    break;
                case States.DoRun:
                    state = DoRun();
                    break;
                case States.NextBitFlag:
                    state = NextBitFlag();
                    break;
            }
        }
        return pOutput;
    }

    private ushort GetFlags()
    {
        ushort next = (ushort)((pInput[compressed_index]) + (pInput[compressed_index + 1] << 8));
        compressed_index += 2;
        return next;
    }

    private States Start()
    {
        current_word = GetFlags();
        bitflags = current_word;
        ushort test_msb = (ushort)(bitflags >> 15);
        bitflags = (ushort)((bitflags << 1) | 1);

        if ((test_msb & 1) == 0)
        {
            return States.DoRun;
        }
        return States.MoveUnique;
    }

    private States MoveUnique()
    {
        pOutput[uncompressed_index++] = pInput[compressed_index++];
        ushort test_msb = (ushort)(bitflags >> 15);
        bitflags <<= 1;
        if (bitflags == 0)
        {
            return States.Start;
        }
        if ((test_msb & 1) == 1)
        {
            return States.MoveUnique;
        }
        return States.DoRun;

    }

    private States DoRun()
    {
        current_word = GetFlags();
        if (current_word == 0)
        {
            return States.Done;
        }

        ushort run_length = (ushort)(((current_word >> 10) & 0x3F) + 3);
        current_word = (ushort)((current_word & 0x03FF) + 1);
        uint back_index = uncompressed_index - current_word;
        try
        {
            for (int i = 0; i < run_length; i++)
            {
                pOutput[uncompressed_index++] = pOutput[back_index++];
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return States.Done;
        }

        return States.NextBitFlag;
    }

    private States NextBitFlag()
    {
        if (uncompressed_index > uncompressed_length)
        {
            return States.Done;
        }
        ushort test_msb = (ushort)(bitflags >> 15);
        bitflags <<= 1;
        if ((test_msb & 1) == 0)
        {
            return States.DoRun;
        }
        if (bitflags == 0)
        {
            return States.Start;
        }
        if ((test_msb & 1) == 1)
        {
            return States.MoveUnique;
        }
        return States.Done;
    }
}

