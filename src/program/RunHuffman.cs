using Tiracompress.Algorithms;

namespace Tiracompress
{
    public partial class Program
    {
        private static int RunHuffmanAlgorithmForEncoding(
            string inputFile,
            string outputFile)
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
                Console.Write($"0x{pair.Item1:X2} = {pair.Item2} ");
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

                Console.Write($"0x{symbol:X2} = {code} ");
            }

            Console.WriteLine();

            using var output = File.Create(outputFile);

            // Asetetaan syötetiedosto takaisin alkuun koodaamista varten
            input.Position = 0;

            Console.WriteLine();
            Console.Write("Compressing data... ");
            (ulong uncompressed, ulong compressed, TimeSpan timing) = huffman.Encode(
                                                        root,
                                                        codeTable,
                                                        input,
                                                        output);

            var huffmanTableOverhead = (ulong)output.Length - compressed;
            var reduction = (compressed + huffmanTableOverhead) / (double)uncompressed;

            if (reduction > 1)
            {
                reduction = -(reduction - 1);
            }

            if ((compressed + huffmanTableOverhead) > uncompressed)
            {
                Console.WriteLine("done, but output grew bigger than input (maybe input too small or frequency distribution too even?)");
            }
            else
            {
                Console.WriteLine("done");
            }

            Console.WriteLine($"Encoding took: {timing:c}");
            Console.WriteLine($"Input data size: {uncompressed} bytes, output data size: {compressed} bytes, Huffman table overhead: {huffmanTableOverhead} bytes = {reduction:P2} reduction");

            return 0;
        }

        private static int RunHuffmanAlgorithmForDecoding(
            string inputFile,
            string outputFile)
        {
            using var input = File.OpenRead(inputFile);
            using var output = File.Create(outputFile);

            var huffman = new Huffman();

            Console.WriteLine();
            Console.Write("Uncompressing data... ");

            var timing = huffman.Decode(input, output);

            if (timing == null)
            {
                Console.WriteLine("error!");
                return 1;
            }

            Console.WriteLine("done");

            Console.WriteLine($"Decoding took: {timing:c}");

            return 0;
        }

        private static int RunHuffmanAlgorithm(
            string inputFile,
            string outputFile,
            string mode)
        {
            return mode switch
            {
                "compress" => RunHuffmanAlgorithmForEncoding(inputFile, outputFile),
                "uncompress" => RunHuffmanAlgorithmForDecoding(inputFile, outputFile),
                _ => throw new ArgumentOutOfRangeException(nameof(mode)),
            };
        }
    }
}