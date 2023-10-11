using Tiracompress.Algorithms;

namespace Tiracompress
{
    public partial class Program
    {
        private static int RunDeflateAlgorithmForEncoding(
            string inputFile,
            string outputFile)
        {
            using var input = File.OpenRead(inputFile);
            using var output = File.Create(outputFile);

            var deflate = new Deflate();

            Console.WriteLine();
            Console.Write("Compressing data... ");

            (ulong uncompressed,
             ulong compressed,
             ulong literals,
             ulong references,
             ulong compressedBlocks,
             ulong uncompressedBlocks,
             TimeSpan timing) = deflate.Encode(input, output);

            if (uncompressed == 0)
            {
                // Pakkaus epäonnistui
                Console.WriteLine("failed to compress!");
                return 1;
            }

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
            Console.WriteLine($"Input data size: {uncompressed} bytes, literals encoded = {literals}, references encoded = {references}, compressed blocks created = {compressedBlocks}, uncompressed blocks created = {uncompressedBlocks}, output data size: {compressed} bytes = {reduction:P2} reduction");

            return 0;
        }

        private static int RunDeflateAlgorithmForDecoding(
            string inputFile,
            string outputFile)
        {
            throw new NotImplementedException();
        }

        private static int RunDeflateAlgorithm(
            string inputFile,
            string outputFile,
            string mode)
        {
            return mode switch
            {
                "compress" => RunDeflateAlgorithmForEncoding(inputFile, outputFile),
                "uncompress" => RunDeflateAlgorithmForDecoding(inputFile, outputFile),
                _ => throw new ArgumentOutOfRangeException(nameof(mode)),
            };
        }
    }
}