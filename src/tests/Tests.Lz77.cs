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
        foreach (var testinput in new [] { _testinput_1, _testinput_2, _testinput_3 })
        {
            for (var windowSize = 4; windowSize <= 1024; windowSize += 10)
            {
                var lz77 = new Lz77(windowSize);
                var window = new byte[windowSize];

                using var inputStream = new MemoryStream(testinput);
                using var compressedStream = new MemoryStream();

                (ulong compressed, ulong literals, ulong references) = lz77.Encode(window, inputStream, compressedStream);

                Assert.Equal(compressed + 8, (ulong)compressedStream.Length);

                // Aseta alkukohta varsinaisen pakatun datan alkuun
                compressedStream.Position = 8;

                using var uncompressedStream = new MemoryStream();

                _ = lz77.Decode((ulong)testinput.Length, window, compressedStream, uncompressedStream);

                uncompressedStream.Position = 0;

                for (int i=0;i<testinput.Length;i++)
                {
                    var nextUncompressed = uncompressedStream.ReadByte();
                    Assert.NotEqual(-1, nextUncompressed);
                    Assert.Equal(testinput[i], (byte)nextUncompressed);
                }
            }
        }
    }

    [Fact]
    public void TestBufferSizeNotChangeDecodedResults()
    {
        foreach (var testinput in new[] { _testinput_1, _testinput_2, _testinput_3 })
        {
            for (var bufferSize = 4; bufferSize <= 1024; bufferSize += 10)
            {
                var lz77 = new Lz77(null, bufferSize, bufferSize);

                using var inputStream = new MemoryStream(testinput);
                using var compressedStream = new MemoryStream();

                (ulong uncompressed, ulong compressed, _, _, _) = lz77.Encode(inputStream, compressedStream);

                Assert.Equal(uncompressed, (ulong)inputStream.Length);
                Assert.Equal(compressed + 8, (ulong)compressedStream.Length);

                compressedStream.Position = 0;

                using var uncompressedStream = new MemoryStream();

                _ = lz77.Decode(compressedStream, uncompressedStream);

                uncompressedStream.Position = 0;

                for (int i = 0; i < testinput.Length; i++)
                {
                    var nextUncompressed = uncompressedStream.ReadByte();
                    Assert.NotEqual(-1, nextUncompressed);
                    Assert.Equal(testinput[i], (byte)nextUncompressed);
                }
            }
        }
    }

    [Fact]
    public void TestEncodeDecodeRoundtrip_SilesiaWebster()
    {
        using var inputStream = File.OpenRead("./silesia/webster");
        var compressedFile = Path.GetTempFileName();
        using var compressedStream = File.Create(compressedFile);

        var lz77 = new Lz77();
        _ = lz77.Encode(inputStream, compressedStream);

        compressedStream.Flush();

        Assert.NotEqual(0, compressedStream.Length);

        compressedStream.Position = 0;

        var uncompressedFile = Path.GetTempFileName();
        using var uncompressedStream = File.Create(uncompressedFile);

        lz77 = new Lz77();

        _ = lz77.Decode(compressedStream, uncompressedStream);

        uncompressedStream.Flush();

        Assert.Equal(inputStream.Length, uncompressedStream.Length);

        // Vertaillaan alkuperäinen ja takaisinpurettu tiedosto

        inputStream.Position = 0;
        uncompressedStream.Position = 0;

        var buffer1 = new byte[1 * 1024 * 1024];
        var buffer2 = new byte[1 * 1024 * 1024];
        var read1 = 0;
        var read2 = 0;

        do
        {
            read1 = inputStream.Read(buffer1, 0, buffer1.Length);
            read2 = uncompressedStream.Read(buffer2, 0, buffer2.Length);

            Assert.Equal(read1, read2);

            for (int i = 0; i < read1; i++)
            {
                Assert.Equal(buffer1[i], buffer2[i]);
            }

        } while (read1 > 0);
    }

    [Fact]
    public void TestEncodeDecodeRoundtrip_SilesiaXRay()
    {
        using var inputStream = File.OpenRead("./silesia/x-ray");
        var compressedFile = Path.GetTempFileName();
        using var compressedStream = File.Create(compressedFile);

        var lz77 = new Lz77();
        _ = lz77.Encode(inputStream, compressedStream);

        compressedStream.Flush();

        Assert.NotEqual(0, compressedStream.Length);

        compressedStream.Position = 0;

        var uncompressedFile = Path.GetTempFileName();
        using var uncompressedStream = File.Create(uncompressedFile);

        lz77 = new Lz77();

        _ = lz77.Decode(compressedStream, uncompressedStream);

        uncompressedStream.Flush();

        Assert.Equal(inputStream.Length, uncompressedStream.Length);

        // Vertaillaan alkuperäinen ja takaisinpurettu tiedosto

        inputStream.Position = 0;
        uncompressedStream.Position = 0;

        var buffer1 = new byte[1 * 1024 * 1024];
        var buffer2 = new byte[1 * 1024 * 1024];
        var read1 = 0;
        var read2 = 0;

        do
        {
            read1 = inputStream.Read(buffer1, 0, buffer1.Length);
            read2 = uncompressedStream.Read(buffer2, 0, buffer2.Length);

            Assert.Equal(read1, read2);

            for (int i = 0; i < read1; i++)
            {
                Assert.Equal(buffer1[i], buffer2[i]);
            }

        } while (read1 > 0);
    }
}