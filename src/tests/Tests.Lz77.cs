using Tiracompress.Algorithms;

namespace Tiracompress.Tests;

public class Lz77Tests
{
    private byte[] _testinput_1 = new byte[] 
    {
        0xB,0xC,0xA,0xA,0xD,0xD,0xD,0xC,0xC,0xA,0XC,0xA,0XC,0xA,0XC
    };

    [Fact]
    public void TestEncodeDecodeRoundtrip_Input1()
    {
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
}