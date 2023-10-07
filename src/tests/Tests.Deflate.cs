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
    public void TestMinimumBackreferenceLengthWorks()
    {
        var deflate = new Deflate();

        byte[] inputBlock = { 0x1, 0x1, 0x1, 0x1, 0x1, 0x1, 0x1 };
        byte[] window = new byte[32*1024];

        long windowFrontPointer = 0;
        long windowBackPointer = 0;

        int inputPointer = 0;

        // Ensimmäinen symboli, pitäisi olla literaali
        var symbol = deflate.LzScanWindowForMatch(inputBlock, ref inputPointer, inputBlock.Length, window, ref windowFrontPointer, ref windowBackPointer);
        Assert.IsAssignableFrom<Deflate.Literal>(symbol);

        // Toinen symboli, pitäisi olla literaali
        symbol = deflate.LzScanWindowForMatch(inputBlock, ref inputPointer, inputBlock.Length, window, ref windowFrontPointer, ref windowBackPointer);
        Assert.IsAssignableFrom<Deflate.Literal>(symbol);

        // Kolmas symboli, pitäisi olla literaali
        symbol = deflate.LzScanWindowForMatch(inputBlock, ref inputPointer, inputBlock.Length, window, ref windowFrontPointer, ref windowBackPointer);
        Assert.IsAssignableFrom<Deflate.Literal>(symbol);

        // Neljäs symboli, pitäisi olla viittaus
        symbol = deflate.LzScanWindowForMatch(inputBlock, ref inputPointer, inputBlock.Length, window, ref windowFrontPointer, ref windowBackPointer);
        Assert.IsAssignableFrom<Deflate.Backreference>(symbol);
        if (symbol is Deflate.Backreference backreference)
        {
            Assert.Equal(3, backreference.Length);
            Assert.Equal(3, backreference.Distance);
            Assert.Equal(0x1, backreference.Next);
        }
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

        (ulong uncompressed, ulong compressed, ulong literals, ulong references, TimeSpan timing) = deflate.Encode(inputStream, outputStream);

        Assert.NotEqual(0UL, literals);
        Assert.NotEqual(0UL, references);
    }
}