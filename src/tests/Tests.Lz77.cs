using Tiracompress.Algorithms;

namespace Tiracompress.Tests;

public class Lz77Tests
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
    public void TestWindowSizeNotChangeDecodedResults()
    {
        for (var windowSize = 4; windowSize <= 1024; windowSize += 10)
        {
            var lz77 = new Lz77(windowSize);
            var window = new byte[windowSize];

            using var inputStream = new MemoryStream(_testinput_2);
            using var compressedStream = new MemoryStream();

            (ulong compressed, ulong literals, ulong references) = lz77.Encode(window, inputStream, compressedStream);

            Assert.Equal(compressed, (ulong)compressedStream.Length);

            compressedStream.Position = 0;

            using var uncompressedStream = new MemoryStream();

            _ = lz77.Decode(window, compressedStream, uncompressedStream);

            uncompressedStream.Position = 0;

            for (int i=0;i<_testinput_2.Length;i++)
            {
                var nextUncompressed = uncompressedStream.ReadByte();
                Assert.NotEqual(-1, nextUncompressed);
                Assert.Equal(_testinput_2[i], (byte)nextUncompressed);
            }
        }
    }

    [Fact]
    public void TestLiteralDataOutputIsCorrectAndFitsBuffer()
    {
        var lz77 = new Lz77();
        var buffer = new byte[10];
        var outputPointer = 0;
        ulong compressedBytes = 0;

        using var outputStream = new MemoryStream();

        lz77.OutputLiteralData(0x0A, buffer, ref outputPointer, outputStream, ref compressedBytes);

        // Koska puskurissa tilaa, ei pitäisi aiheuttaa uloskirjoitusta
        Assert.Equal(0L, outputStream.Length);
        Assert.Equal(0UL, compressedBytes);
        Assert.Equal(3, outputPointer);

        Assert.Equal(0, buffer[0]);
        Assert.Equal(0, buffer[1]);
        Assert.Equal(0xA, buffer[2]);
    }

    [Fact]
    public void TestLiteralDataOutputIsCorrectAndOverflowsBuffer()
    {
        var lz77 = new Lz77();
        var buffer = new byte[2];
        var outputPointer = 0;
        ulong compressedBytes = 0;

        using var outputStream = new MemoryStream();

        lz77.OutputLiteralData(0x0A, buffer, ref outputPointer, outputStream, ref compressedBytes);

        // Koska puskurissa ei riittävästi tilaa, pitäisi aiheuttaa uloskirjoituksen osalle dataa
        Assert.Equal(2L, outputStream.Length);
        Assert.Equal(2UL, compressedBytes);
        Assert.Equal(1, outputPointer);

        Assert.Equal(0xA, buffer[0]);
    }

    [Fact]
    public void TestReferencedDataOutputIsCorrectAndFitsBuffer()
    {
        var lz77 = new Lz77();
        var buffer = new byte[10];
        var outputPointer = 0;
        ulong compressedBytes = 0;
        ushort distance = 0x1FF;
        byte length = 0x6;

        using var outputStream = new MemoryStream();

        lz77.OutputReferencedData(distance, length, buffer, ref outputPointer, outputStream, ref compressedBytes, 0xA);

        // Koska puskurissa tilaa, ei pitäisi aiheuttaa uloskirjoitusta
        Assert.Equal(0L, outputStream.Length);
        Assert.Equal(0UL, compressedBytes);
        Assert.Equal(4, outputPointer);

        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal(0x01, buffer[1]);
        Assert.Equal(0x6, buffer[2]);
        Assert.Equal(0xA, buffer[3]);
    }

    [Fact]
    public void TestReferencedDataOutputIsCorrectAndOverflowsBuffer()
    {
        var lz77 = new Lz77();
        var buffer = new byte[2];
        var outputPointer = 0;
        ulong compressedBytes = 0;
        ushort distance = 0x1FF;
        byte length = 0x6;

        using var outputStream = new MemoryStream();

        lz77.OutputReferencedData(distance, length, buffer, ref outputPointer, outputStream, ref compressedBytes, 0xA);

        // Pitäisi aiheuttaa uloskirjoituksen ensimmäiselle puoliskolle dataa
        Assert.Equal(2L, outputStream.Length);
        Assert.Equal(2UL, compressedBytes);
        Assert.Equal(2, outputPointer);

        Assert.Equal(0x6, buffer[0]);
        Assert.Equal(0xA, buffer[1]);
    }


    [Fact]
    public void TestEncodingAndDecodingWorksWithCustomBufferSizes()
    {
        for (var bufferSize = 4; bufferSize <= 1024; bufferSize += 10)
        {
            var lz77 = new Lz77(null, bufferSize, bufferSize);
            
            using var inputStream = new MemoryStream(_testinput_3);
            using var compressedStream = new MemoryStream();

            (ulong uncompressed, ulong compressed, _, _, _) = lz77.Encode(inputStream, compressedStream);

            Assert.Equal(uncompressed, (ulong)inputStream.Length);
            Assert.Equal(compressed, (ulong)compressedStream.Length);

            compressedStream.Position = 0;

            using var uncompressedStream = new MemoryStream();

            _ = lz77.Decode(compressedStream, uncompressedStream);

            uncompressedStream.Position = 0;

            for (int i=0;i<_testinput_3.Length;i++)
            {
                var nextUncompressed = uncompressedStream.ReadByte();
                Assert.NotEqual(-1, nextUncompressed);
                Assert.Equal(_testinput_3[i], (byte)nextUncompressed);
            }
        }
    }


    [Fact]
    public void TestEncodeDecodeRoundtrip_Input1()
    {
        // Käytetään pientä pakkausikkunaa
        var lz77 = new Lz77(10);

        using var inputStream = new MemoryStream(_testinput_1);
        using var outputStream = new MemoryStream();

        var results = lz77.Encode(inputStream, outputStream);

        Assert.NotEqual(0, outputStream.Length);

        outputStream.Position = 0;

        var distance = default(ushort);
        var buffer = new byte[2];

        var decodeBuffer = new byte[_testinput_1.Length];
        var decodePointer = 0;

        while (outputStream.Position < outputStream.Length)
        {
            Assert.InRange<int>(decodePointer, 0, decodeBuffer.Length - 1);

            // Luetaan distance
            Assert.Equal(buffer.Length, outputStream.Read(buffer, 0, buffer.Length));
            distance = BitConverter.ToUInt16(buffer);

            switch (distance)
            {
                case 0:
                    // Literaali
                    var symbol = outputStream.ReadByte();
                    Assert.NotEqual(-1, symbol);

                    decodeBuffer[decodePointer] = (byte)symbol;
                    decodePointer++;
                    break;
                default:
                    Assert.InRange<int>(decodePointer - distance, 0, decodePointer);
                    Assert.NotEqual(0, decodePointer);

                    // Luetaan length
                    var length = outputStream.ReadByte();
                    Assert.NotEqual(-1, length);
                    Assert.InRange<int>(decodePointer - distance + length, 0, decodeBuffer.Length - 1);

                    var start = decodePointer - distance;

                    for (int p = 0; p < length; p++)
                    {
                        decodeBuffer[decodePointer] = decodeBuffer[start + p];
                        decodePointer++;
                    }

                    // Luetaan literaali
                    var literalByte = outputStream.ReadByte();
                    Assert.NotEqual(-1, literalByte);

                    decodeBuffer[decodePointer] = (byte)literalByte;
                    decodePointer++;

                    break;
            }
        }

        for (int i=0;i<decodeBuffer.Length;i++)
        {
            Assert.Equal(decodeBuffer[i], _testinput_1[i]);
        }
    }

    [Fact]
    public void TestEncodeDecodeRoundtrip_Input2()
    {
        // Käytetään pientä pakkausikkunaa
        var lz77 = new Lz77(10);

        using var inputStream = new MemoryStream(_testinput_2);
        using var outputStream = new MemoryStream();

        var results = lz77.Encode(inputStream, outputStream);

        Assert.NotEqual(0, outputStream.Length);

        outputStream.Position = 0;

        var distance = default(ushort);
        var buffer = new byte[2];

        var decodeBuffer = new byte[_testinput_2.Length];
        var decodePointer = 0;

        while (outputStream.Position < outputStream.Length)
        {
            Assert.InRange<int>(decodePointer, 0, decodeBuffer.Length - 1);

            // Luetaan distance
            Assert.Equal(buffer.Length, outputStream.Read(buffer, 0, buffer.Length));
            distance = BitConverter.ToUInt16(buffer);

            switch (distance)
            {
                case 0:
                    // Literaali
                    var symbol = outputStream.ReadByte();
                    Assert.NotEqual(-1, symbol);

                    decodeBuffer[decodePointer] = (byte)symbol;
                    decodePointer++;
                    break;
                default:
                    Assert.InRange<int>(decodePointer - distance, 0, decodePointer);
                    Assert.NotEqual(0, decodePointer);

                    // Luetaan length
                    var length = outputStream.ReadByte();
                    Assert.NotEqual(-1, length);
                    Assert.InRange<int>(decodePointer - distance + length, 0, decodeBuffer.Length - 1);

                    var start = decodePointer - distance;

                    for (int p = 0; p < length; p++)
                    {
                        decodeBuffer[decodePointer] = decodeBuffer[start + p];
                        decodePointer++;
                    }

                    // Luetaan literaali
                    var literalByte = outputStream.ReadByte();
                    Assert.NotEqual(-1, literalByte);

                    decodeBuffer[decodePointer] = (byte)literalByte;
                    decodePointer++;

                    break;
            }
        }

        for (int i=0;i<decodeBuffer.Length;i++)
        {
            Assert.Equal(decodeBuffer[i], _testinput_2[i]);
        }
    }
}