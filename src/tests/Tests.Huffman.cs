using Tiracompress.Algorithms;

namespace Tiracompress.Tests;

public class HuffmanTests
{
    /*  Sample input from https://www.programiz.com/dsa/huffman-coding example

        Expected results:

        Byte    Frequency   Code
        0xA     5           11
        0xB     1           100
        0xC     6           0
        0xD     3           101       

    */ 
    private byte[] _testinput = new byte[] 
    {
        0xB,0xC,0xA,0xA,0xD,0xD,0xD,0xC,0xC,0xA,0XC,0xA,0XC,0xA,0XC
    };

    [Fact]
    public void TestSymbolFrequencies()
    {
        var huffman = new Huffman();

        using var memoryStream = new MemoryStream(_testinput);

        var frequencies = huffman.BuildSymbolFrequencies(memoryStream);

        Assert.Equal(4, frequencies.Count);

        foreach (var (symbol, frequency) in frequencies)
        {
            switch (symbol)
            {
                case 0xA:
                    Assert.Equal(5, (int)frequency);
                    break;
                case 0xB:
                    Assert.Equal(1, (int)frequency);
                    break;
                case 0xC:
                    Assert.Equal(6, (int)frequency);
                    break;
                case 0xD:
                    Assert.Equal(3, (int)frequency);
                    break;
                default:
                    Assert.Fail("Unknown symbol in frequencies table");
                    break;
            }
        }
    }

    [Fact]
    public void TestSymbolTree()
    {
        var huffman = new Huffman();

        using var memoryStream = new MemoryStream(_testinput);

        var frequencies = huffman.BuildSymbolFrequencies(memoryStream);
        var rootNode = huffman.BuildHuffmanTree(frequencies);

        // Test tree's correctness
        Assert.Null(rootNode.Symbol);

        Assert.NotNull(rootNode.Left.Symbol);
        Assert.Equal((int)rootNode.Left.Symbol, 0xC);
        Assert.Equal((int)rootNode.Left.Frequency, 6);

        Assert.Null(rootNode.Right.Symbol);

        Assert.Null(rootNode.Right.Left.Symbol);

        Assert.NotNull(rootNode.Right.Left.Left.Symbol);
        Assert.Equal((int)rootNode.Right.Left.Left.Symbol, 0xB);
        Assert.Equal((int)rootNode.Right.Left.Left.Frequency, 1);

        Assert.NotNull(rootNode.Right.Left.Right.Symbol);
        Assert.Equal((int)rootNode.Right.Left.Right.Symbol, 0xD);
        Assert.Equal((int)rootNode.Right.Left.Right.Frequency, 3);

        Assert.NotNull(rootNode.Right.Right.Symbol);
        Assert.Equal((int)rootNode.Right.Right.Symbol, 0xA);
        Assert.Equal((int)rootNode.Right.Right.Frequency, 5);
    }

    private bool IsBitSet(ushort code, int bit)
    {
        var bitValue = (ushort)Math.Pow(2, bit - 1);

        return (code & bitValue) == bitValue;
    }

    [Fact]
    public void TestCodeTable()
    {
        var huffman = new Huffman();

        using var memoryStream = new MemoryStream(_testinput);

        var frequencies = huffman.BuildSymbolFrequencies(memoryStream);
        var rootNode = huffman.BuildHuffmanTree(frequencies);
        var table = huffman.BuildCodeTable(rootNode);

        Assert.Equal(4, table.Count);

        Assert.Contains<byte, (ushort, int)>(0xA, table);

        var codeForA = table[0xA];

        Console.WriteLine(codeForA);

        Assert.True(IsBitSet(codeForA.Item1, 1));
        Assert.True(IsBitSet(codeForA.Item1, 2));

        Assert.Contains<byte, (ushort, int)>(0xB, table);

        var codeForB = table[0xB];

        Assert.False(IsBitSet(codeForB.Item1, 1));
        Assert.False(IsBitSet(codeForB.Item1, 2));
        Assert.True(IsBitSet(codeForB.Item1, 3));

        Assert.Contains<byte, (ushort, int)>(0xC, table);

        var codeForC = table[0xC];

        Assert.False(IsBitSet(codeForC.Item1, 1));

        Assert.Contains<byte, (ushort, int)>(0xD, table);

        var codeForD = table[0xD];

        Assert.True(IsBitSet(codeForD.Item1, 1));
        Assert.False(IsBitSet(codeForD.Item1, 2));
        Assert.True(IsBitSet(codeForD.Item1, 3));
    }
}