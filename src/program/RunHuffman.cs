using Tiracompress.Algorithms;

namespace Tiracompress
{
    public partial class Program
    {
        private static int RunHuffmanAlgorithm(
            string inputFile,
            string outputFile,
            string mode)
        {
            using var input = File.OpenRead(inputFile);

            var huffman = new Huffman();

            var freq = huffman.BuildSymbolFrequencies(input);

            if (freq.Count == 0)
            {
                Console.Write("Input has no data, cannot compress anything!");
                return 0;
            }

            Console.Write("Input data symbol frequencies: ");

            foreach (var pair in freq)
            {
                Console.Write($"{pair.Item1} = {pair.Item2} ");
            }

            Console.WriteLine();

            var root = huffman.BuildHuffmanTree(freq);

            // Use tree to build code table
            var codeTable = huffman.BuildCodeTable(root);

            Console.WriteLine();
            Console.Write("Huffman code table for encoding: ");

            foreach (var symbol in codeTable.Keys)
            {
                int codeLength = codeTable[symbol].Item2;
                string code = string.Empty;

                for (var bit = codeLength - 1; bit >= 0; bit--)
                {
                    var bitValue = (ushort)Math.Pow(2, bit);
                    code += (codeTable[symbol].Item1 & bitValue) == bitValue
                        ? "1" : "0";
                }

                Console.Write($"{symbol} = {code} ");
            }

            Console.WriteLine();

            return 1;
        }
    }
}