using Tiracompress.Algorithms;

namespace Tiracompress.Tests;

public class DeflateTests
{
    private byte[] _testinput_1 = new byte[] 
    {
        0xB,0xC,0xA,0xA,0xD,0xD,0xD,0xC,0xC,0xA,0XC,0xA,0XC,0xA,0XC
    };

    private byte[] _testinput_2 = new byte[] 
    {
        0xA,0xA,0xA,0xA,0xA,
        0xB,0xB,0xB,0xB,0xB,0XB,0xB,0XB,0xB,
        0xC,0xC,0xC,0xC,0xC,0xC,0xC,0xC,0xC,0xC,0xC,0xC,
        0xD,0xD,0xD,0xD,0xD,0xD,0xD,0xD,0xD,0xD,0xD,0xD,0xD,
        0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,
        0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,
        0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,0xF,
        0xF,0xF,0xF,0xF,0xF
    };

    private byte[] _testinput_3 = new byte[] 
    {
        0xB,0xC,0xA,0xA,0xD,0xD,0xD,0xC,0xC,0xA,0XC,0xA,0XC,0xA,0XC,
        0xD,0xD,0xD,0xD,0xD,0xD,0xD,0xD,0xD,0xD,0xD,0xD,0xD,
        0xB,0xC,0xA,0xA,0xD,0xD,0xD,0xC,0xC,0xA,0XC,0xA,0XC,0xA,0XC,
        0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,0xE,
        0xA,0xA,0xA,0xA,0xA
    };

    [Fact]
    public void TestBackreferenceLengthWithinRange()
    {
        var deflate = new Deflate();

        foreach (var inputBlock in new[] { _testinput_1, _testinput_2, _testinput_3 })
        {
            byte[] window = new byte[32*1024];

            long windowFrontPointer = 0;
            long windowBackPointer = 0;

            int inputPointer = 0;

            var symbol = default(Deflate.Symbol);

            while ((symbol = deflate.LzScanWindowForMatch(inputBlock, ref inputPointer, inputBlock.Length, window, ref windowFrontPointer, ref windowBackPointer)) != null)
            {
                if (symbol is Deflate.Backreference br)
                {
                    Assert.InRange<ushort>(br.Length, 3, 258);
                }
            }
        }
    }

    [Fact]
    public void TestInputPointerIncreasesCorrectlyAfterMatch()
    {
        var deflate = new Deflate();

        foreach (var inputBlock in new[] { _testinput_1, _testinput_2, _testinput_3 })
        {
            byte[] window = new byte[32*1024];

            long windowFrontPointer = 0;
            long windowBackPointer = 0;

            int lastInputPointer = 0;
            int inputPointer = 0;

            var symbol = default(Deflate.Symbol);

            while ((symbol = deflate.LzScanWindowForMatch(inputBlock, ref inputPointer, inputBlock.Length, window, ref windowFrontPointer, ref windowBackPointer)) != null)
            {
                if (symbol is Deflate.Backreference br)
                {
                    Assert.Equal(lastInputPointer + br.Length, inputPointer);
                } else if (symbol is Deflate.Literal lit)
                {
                    Assert.Equal(lastInputPointer + 1, inputPointer);
                }

                lastInputPointer = inputPointer;
            }
        }
    }

    [Fact]
    public void TestHuffmanCodesAreRecalculatedCorrectly()
    {
        var deflate = new Deflate();

        // Käytetään RFC 1951 kohdan 3.2.2. mukaista esimerkkiä
        var codeTable = new Dictionary<ushort, (uint, int)>()
        {
            { 0XA, (0, 3) },
            { 0XB, (0, 3) },
            { 0XC, (0, 3) },
            { 0XD, (0, 3) },
            { 0XE, (0, 3) },
            { 0XF, (0, 2) },
            { 0X1, (0, 4) }, // syboli G
            { 0X2, (0, 4) }  // symbli H
        };

        deflate.RecalculateHuffmanCodeTable(codeTable);

        /*
        Koodatut esitykset pitäisi mennä allaolevan mukaisesti:
        Symbol Length   Code
            ------ ------   ----
            A       3        010
            B       3        011
            C       3        100
            D       3        101
            E       3        110
            F       2         00
            G       4       1110
            H       4       1111
        */

        Assert.Equal(0x2U, codeTable[0xA].Item1); // 010
        Assert.Equal(3, codeTable[0xA].Item2);

        Assert.Equal(0x3U, codeTable[0xB].Item1); // 011
        Assert.Equal(3, codeTable[0xB].Item2);

        Assert.Equal(0x4U, codeTable[0xC].Item1); // 100
        Assert.Equal(3, codeTable[0xC].Item2);

        Assert.Equal(0x5U, codeTable[0xD].Item1); // 101
        Assert.Equal(3, codeTable[0xD].Item2);

        Assert.Equal(0x6U, codeTable[0xE].Item1); // 110
        Assert.Equal(3, codeTable[0xE].Item2);

        Assert.Equal(0x0U, codeTable[0xF].Item1); // 00
        Assert.Equal(2, codeTable[0xF].Item2);

        Assert.Equal(0xEU, codeTable[0x1].Item1); // 1110
        Assert.Equal(4, codeTable[0x1].Item2);

        Assert.Equal(0xFU, codeTable[0x2].Item1); // 1111
        Assert.Equal(4, codeTable[0x2].Item2);
    }


    [Fact]
    public void TestEncodingDoesNotOverflow()
    {
        var deflate = new Deflate();

        byte[] inputBlock = { 0x1, 0x1 };
        byte[] window = new byte[32*1024];

        long windowFrontPointer = 0;
        long windowBackPointer = 0;

        int inputPointer = 0;

        foreach (byte b in inputBlock)
        {
            _ = deflate.LzScanWindowForMatch(inputBlock, ref inputPointer, inputBlock.Length, window, ref windowFrontPointer, ref windowBackPointer);
        }

        Assert.Null(deflate.LzScanWindowForMatch(inputBlock, ref inputPointer, inputBlock.Length, window, ref windowFrontPointer, ref windowBackPointer));
    }

    [Fact]
    public void TestEncodeDecodeRoundtrip_Input1()
    {
        var deflate = new Deflate();

        using var inputStream = new MemoryStream(_testinput_1);
        using var outputStream = new MemoryStream();

        (ulong uncompressed,
         ulong compressed,
         ulong literals,
         ulong references,
         ulong compressedBlocks,
         ulong uncompressedBlocks,
         TimeSpan timing) = deflate.Encode(inputStream, outputStream);

        Assert.NotEqual(0UL, literals);
        Assert.NotEqual(0UL, references);
    }
}