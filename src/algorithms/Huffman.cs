namespace Tiracompress.Algorithms
{
    /// <summary>
    /// This class implements encoding and decoding using Huffman algorithm
    /// </summary>
    public class Huffman
    {
        /// <summary>
        /// Node in a Huffman tree, either leaf or intermediate
        /// </summary>
        public class Node
        {
            /// <summary>
            /// Symbol represented this node or null if not leaf node
            /// </summary>
            public byte? Symbol { get; set; }

            /// <summary>
            /// Symbol frequency or sum of child if not leaf node
            /// </summary>
            public ulong Frequency { get; set; }

            /// <summary>
            /// Left child of this node if not leaf node
            /// </summary>
            public Node? Left { get; set; }

            /// <summary>
            /// Right child of this node if not leaf node
            /// </summary>
            public Node? Right { get; set; }

            /// <summary>
            /// Create leaf node
            /// </summary>
            /// <param name="symbolFrequency">Symbol frequency represented by this node</param>
            public Node((byte, ulong) symbolFrequency)
            {
                Symbol = symbolFrequency.Item1;
                Frequency = symbolFrequency.Item2;
            }

            /// <summary>
            /// Create intermediate node
            /// </summary>
            /// <param name="left">Left child or null</param>
            /// <param name="right">Right child or null</param>
            public Node(Node? left, Node? right)
            {
                Left = left;
                Right = right;
                Frequency = (left?.Frequency ?? 0) + (right?.Frequency ?? 0);
            }
        }

        /// <summary>
        /// Recursively build encoding table from Huffman symbol tree
        /// </summary>
        /// <param name="node">Node to recurse into</param>
        /// <param name="table">Table to add found symbols</param>
        /// <param name="code">Current code</param>
        /// <param name="depth">Current code's length in bits</param>
        private void BuildTableFromTree(
            Node node,
            IDictionary<byte, (ushort, int)> table,
            int code = 0,
            int depth = 0)
        {
            if (node.Symbol.HasValue)
            {
                // This is leaf node
                table.Add(node.Symbol.Value, ((ushort)code, depth));
                return;
            }

            // Increase code length
            depth++;
            code <<= 1;

            if (node.Left != null)
            {
                // Going to left path, first bit must be 0
                BuildTableFromTree(
                    node.Left,
                    table,
                    code,
                    depth);
            }

            if (node.Right != null)
            {
                // Going to left path, first bit must be 1
                code |= 1;

                BuildTableFromTree(
                    node.Right,
                    table,
                    code,
                    depth);
            }
        }

        /// <summary>
        /// Build a code table for encoding data from Huffman symbol tree
        /// </summary>
        /// <param name="huffmanTreeRoot">Root of the Huffman symbol tree</param>
        /// <returns>Lookup keyed by symbols</returns>
        public IDictionary<byte, (ushort, int)> BuildCodeTable(Node huffmanTreeRoot)
        {
            var table = new Dictionary<byte, (ushort, int)>();

            BuildTableFromTree(huffmanTreeRoot, table);

            return table;
        }

        /// <summary>
        /// Build Huffman symbol tree from symbol frequencies
        /// </summary>
        /// <param name="symbolFrequencies">Symbol freqiencies list</param>
        /// <returns>Root of the Huffman symbol tree</returns>
        public Node BuildHuffmanTree(IList<(byte, ulong)> symbolFrequencies)
        {
            var heap = new LinkedList<Node>();

            foreach (var symbolFrequency in symbolFrequencies)
            {
                _ = heap.AddLast(new Node(symbolFrequency));
                //Console.WriteLine($"New node: {symbolFrequency}");
            }

            Node? root = null;

            while (heap.Count > 0)
            {
                var first = heap.First;
                var second = first!.Next;

                var intermediate = new Node(first.Value, second?.Value);

                //Console.WriteLine($"New intermediate node: {intermediate.Frequency}");

                heap.RemoveFirst();

                if (second != null)
                    heap.RemoveFirst();

                if (heap.Count == 0)
                {
                    // All nodes processed, return as root
                    root = intermediate;
                    break;
                }

                // Find insertion point for new intermediate node in heap
                var heapItem = heap.First;

                while (heapItem != null)
                {
                    if (intermediate.Frequency < heapItem.Value.Frequency)
                    {
                        _ = heap.AddBefore(heapItem, intermediate);
                        break;
                    }

                    heapItem = heapItem.Next;
                }

                if (heapItem == null)
                {
                    _ = heap.AddLast(intermediate);
                }
            }

            return root!;
        }

        /// <summary>
        /// Builds symbol frequencies from the input data.
        /// Read stream in 1M blocks to input buffer to make processing faster.
        /// </summary>
        /// <param name="inputStream">Stream to read input from</param>
        /// <returns>Symbols (bytes) and their frequencies in input</returns>
        public IList<(byte, ulong)> BuildSymbolFrequencies(Stream inputStream)
        {
            var buffer = new byte[1024 * 1024 * 1024];
            var dictionary = new Dictionary<byte, ulong>(byte.MaxValue);

            for (int b = byte.MinValue; b <= byte.MaxValue; b++)
            {
                dictionary.Add((byte)b, 0);
            }

            int read;
            do
            {
                read = inputStream.Read(buffer);

                if (read > 0)
                {
                    for (int i = 0; i < read; i++)
                    {
                        dictionary[buffer[i]] += 1;
                    }
                }

            } while (read > 0);

            // Remove 0 frequency bytes and sort in increasing order
            return dictionary
                .Where(pair => pair.Value > 0)
                .Select(pair => (pair.Key, pair.Value))
                .OrderBy(pair => pair.Value)
                .ToList();
        }
    }
}