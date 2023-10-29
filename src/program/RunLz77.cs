using Tiracompress.Algorithms;

namespace Tiracompress
{
    public partial class Program
    {
        private static int RunLz77AlgorithmForEncoding(
            string inputFile,
            string outputFile)
        {
            using var input = File.OpenRead(inputFile);
            using var output = File.Create(outputFile);

            // Käytetään oletus pakkausikkunaa
            var lz77 = new Lz77();

            Console.WriteLine();
            Console.Write("Compressing data... ");
            (ulong uncompressed, ulong compressed, ulong literals, ulong references, TimeSpan timing) = lz77.Encode(
                                                        input,
                                                        output);

            var reduction = compressed / (double)uncompressed;

            if (reduction > 1)
            {
                reduction = -(reduction - 1);
            }

            if (compressed > uncompressed)
            {
                Console.WriteLine("done, but output grew bigger than input!");
            }
            else
            {
                Console.WriteLine("done");
            }

            Console.WriteLine($"Encoding took: {timing:c}");
            Console.WriteLine($"Input data size: {uncompressed} bytes, literals encoded = {literals}, references encoded = {references}, output data size: {compressed} bytes = {reduction:P2} reduction");

            return 0;
        }

        private static int RunLz77AlgorithmForDecoding(
            string inputFile,
            string outputFile)
        {
            using var input = File.OpenRead(inputFile);
            using var output = File.Create(outputFile);

            // Käytetään oletus pakkausikkunaa
            var lz77 = new Lz77();

            Console.WriteLine();
            Console.Write("Uncompressing data... ");

            var timing = lz77.Decode(input, output);

            if (timing == null)
            {
                Console.WriteLine("error!");
                return 1;
            }

            Console.WriteLine("done");

            Console.WriteLine($"Decoding took: {timing:c}");

            return 0;
        }

        private static int RunLz77Algorithm(
            string inputFile,
            string outputFile,
            string mode)
        {
            return mode switch
            {
                "compress" => RunLz77AlgorithmForEncoding(inputFile, outputFile),
                "uncompress" => RunLz77AlgorithmForDecoding(inputFile, outputFile),
                _ => throw new ArgumentOutOfRangeException(nameof(mode)),
            };
        }
    }
}