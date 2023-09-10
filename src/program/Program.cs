
namespace Tiracompress
{
    // Disable XML comment style check for obvious public class and methods for main program 
#pragma warning disable 1591
    public partial class Program
    {
        private static string? GetParameterValue(
            IDictionary<string, string> lookup,
            string parameterName,
            params string[] allowedValues)
        {
            if (!lookup.TryGetValue(parameterName, out var parameter) ||
                !parameter.Contains(":"))
            {
                return null;
            }

            var paramValue = parameter.Substring(parameter.IndexOf(':') + 1);

            if (allowedValues?.Length > 0 &&
                !allowedValues.Contains(paramValue))
            {
                return null;
            }

            return paramValue;
        }

        private static (string? inputFile, string? outputFile, string? mode, string? algorithm) ParseParameters(string[] args)
        {
            var lookup = args.ToDictionary(arg =>
            {
                var parts = arg.Split(':');
                return parts[0].TrimStart('-');
            });

            var inputFile = GetParameterValue(lookup, "input");
            var outputFile = GetParameterValue(lookup, "output");
            var mode = GetParameterValue(lookup, "mode", "compress", "uncompress");
            var algorithm = GetParameterValue(lookup, "algorithm", "huffman", "lz77", "deflate");

            return (inputFile, outputFile, mode, algorithm);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("--input:<path_to_file> --output:<path_to_file> --mode:[compress|uncompress] --algorithm:[huffman|lz77|deflate]");
        }

        public static int Main(string[] args)
        {
            if (args == null ||
                args.Length == 0)
            {
                Console.WriteLine("No parameters given.");
                PrintUsage();
                return 0;
            }

            var (inputFile, outputFile, mode, algorithm) = ParseParameters(args);

            if (inputFile == null ||
                outputFile == null ||
                mode == null ||
                algorithm == null)
            {
                Console.WriteLine("Not all of the expected parameters given.");
                PrintUsage();
                return 0;
            }

            // Check that input file is available
            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"Input file '{inputFile}' not found!");
                return 1;
            }

            switch (algorithm)
            {
                case "huffman":
                    return RunHuffmanAlgorithm(inputFile, outputFile, mode);
                case "lz77":
                    return RunLz77Algorithm(inputFile, outputFile, mode);
                case "deflate":
                    return RunDeflateAlgorithm(inputFile, outputFile, mode);
                default:
                    break;
            }

            return 0;
        }
    }
#pragma warning restore 1591
}