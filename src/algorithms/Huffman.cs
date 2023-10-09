using System.Diagnostics;

namespace Tiracompress.Algorithms
{
    /// <summary>
    /// Tämä luokka toteuttaa Huffman -algoritmiin perustuvan tiedon koodauksen/pakkauksen sekä purkamisen
    /// </summary>
    public class Huffman
    {
        /// <summary>
        /// Solmu Huffman-puussa, joko lehti-/symbolisolmu tai välisolmu
        /// </summary>
        public class Node
        {
            /// <summary>
            /// Tämän solmun sisältämä symboli tai null mikäli välisolmu
            /// </summary>
            public ushort? Symbol { get; set; }

            /// <summary>
            /// Symboolin esiintymistiheys tai lapsisolmujen summa mikäli välisolmu
            /// </summary>
            public ulong Frequency { get; set; }

            /// <summary>
            /// Vasen lapsisolmu mikäli välisolmu
            /// </summary>
            public Node? Left { get; set; }

            /// <summary>
            /// Oikean lapsisolmu mikäli välisolmu
            /// </summary>
            public Node? Right { get; set; }

            /// <summary>
            /// Luo uusi symbolisolmu
            /// </summary>
            /// <param name="symbolFrequency">Symboli ja esiintymistiheys jota tämä solmu edustaa</param>
            public Node((ushort, ulong) symbolFrequency)
            {
                Symbol = symbolFrequency.Item1;
                Frequency = symbolFrequency.Item2;
            }

            /// <summary>
            /// Luo uusi välisolmu
            /// </summary>
            /// <param name="left">Vasen lapsisolmu</param>
            /// <param name="right">Oikea lapsisolmu</param>
            public Node(Node? left, Node? right)
            {
                Left = left;
                Right = right;
                Frequency = (left?.Frequency ?? 0) + (right?.Frequency ?? 0);
            }
        }

        /// <summary>
        /// Rakentaa rekursiivisesti koodaustaulukon Huffman-symbolipuusta.
        /// Staattinen metodi jotta voidaan kutsua myös Deflate-algoritmista.
        /// </summary>
        /// <param name="node">Solmu joka käsitellään</param>
        /// <param name="table">Taulukko johon lisätään löydetyt symbolit</param>
        /// <param name="code">Tämänhetkinen koodijono</param>
        /// <param name="depth">Tämänhetkinen koodijonon pituus bitteinä</param>
        private static void BuildTableFromTree(
            Node node,
            IDictionary<ushort, (uint, int)> table,
            uint code = 0,
            int depth = 0)
        {
            if (node.Symbol.HasValue)
            {
                // Tämä on symbolisolmu
                table.Add(node.Symbol.Value, (code, depth));
                return;
            }

            // Lisää koodijonon pituus ja tee tilaa uudelle bitille
            depth++;
            code <<= 1;

            if (node.Left != null)
            {
                // Vasen polku, uusi bitti tulee olla 0
                BuildTableFromTree(
                    node.Left,
                    table,
                    code,
                    depth);
            }

            if (node.Right != null)
            {
                // Oikea polku, uusi bitti tulee olla 1
                code |= 1;

                BuildTableFromTree(
                    node.Right,
                    table,
                    code,
                    depth);
            }
        }

        /// <summary>
        /// Rakentaa pakkaukseen käytettävän koodaustaulukon Huffman-symbolipuusta.
        /// Staattinen metodi jotta voidaan kutsua myös Deflate-algoritmista.
        /// </summary>
        /// <param name="huffmanTreeRoot">Huffman-symbolipuun juurisolmu</param>
        /// <returns>Koodaustaulukko jossa avaimena symbolit</returns>
        public static IDictionary<ushort, (uint, int)> BuildCodeTable(Node huffmanTreeRoot)
        {
            var table = new Dictionary<ushort, (uint, int)>();

            BuildTableFromTree(huffmanTreeRoot, table);

            return table;
        }

        /// <summary>
        /// Rakentaa Huffman-symbolipuun symboleista ja niiden esiintymistiheyksistä.
        /// Staattinen metodi jotta voidaan kutsua myös Deflate-algoritmista.
        /// </summary>
        /// <param name="symbolFrequencies">Symbolien esiintymistiheydet</param>
        /// <returns>Huffman-symbolipuun juurisolmu</returns>
        public static Node BuildHuffmanTree(IEnumerable<(ushort, ulong)> symbolFrequencies)
        {
            var heap = new LinkedList<Node>();

            foreach (var symbolFrequency in symbolFrequencies)
            {
                _ = heap.AddLast(new Node(symbolFrequency));
            }

            Node? root = null;

            while (heap.Count > 0)
            {
                var first = heap.First;
                var second = first!.Next;

                var intermediate = new Node(first.Value, second?.Value);

                heap.RemoveFirst();

                if (second != null)
                    heap.RemoveFirst();

                if (heap.Count == 0)
                {
                    // Kaikki elementit prosessoitu, aseta juurisolmuksi
                    root = intermediate;
                    break;
                }

                // Etsi kohta johon uusi välisolmu lisätään
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
        /// Rakentaa symbolien esiintymistiheydet sisääntulevan tietovirran perusteella.
        /// Lukee tietovirtaa 1M paloissa käsittelyn nopeuttamiseksi.
        /// </summary>
        /// <param name="inputStream">Sisääntuleva tietovirta</param>
        /// <returns>Listan symboleista (tavut) ja niiden esiintymistiheyksistä sisääntulevassa tietovirrassa</returns>
        public IList<(ushort, ulong)> BuildSymbolFrequencies(Stream inputStream)
        {
            var buffer = new byte[1024 * 1024];
            var dictionary = new Dictionary<ushort, ulong>(ushort.MaxValue);

            for (int b = byte.MinValue; b <= byte.MaxValue; b++)
            {
                dictionary.Add((ushort)b, 0);
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

            // Poista ei-esiintyvät symbolit/tavut ja järjestä nousevaan järjestykseen
            return dictionary
                .Where(pair => pair.Value > 0)
                .Select(pair => (pair.Key, pair.Value))
                .OrderBy(pair => pair.Value)
                .ToList();
        }

        /// <summary>
        /// Serialisoi Huffman-symbolipuun ulosmenevään tietovirtaan rekursiivisesti käyttäen
        /// post-order traversal järjestystä.
        /// 
        /// Kaikki puun alkiot vieraillaan ja:
        /// - jokaista symbolisolmua vasten kirjoitetaan tavu 1 + symboli omana tavuna
        /// - jokaista välisolmua vasten kirjoitetaan tavu 0
        /// </summary>
        /// <param name="node">Solmu joka käsitellään</param>
        /// <param name="outputStream">Ulosmenevä tietovirta</param>
        public void SerializeHuffmanTree(
            Node node,
            Stream outputStream)
        {
            if (node.Left != null)
            {
                SerializeHuffmanTree(
                    node.Left,
                    outputStream);
            }

            if (node.Right != null)
            {
                SerializeHuffmanTree(
                    node.Right,
                    outputStream);
            }

            if (node.Symbol.HasValue)
            {
                // This is leaf node
                outputStream.WriteByte(0x01);
                outputStream.WriteByte((byte)node.Symbol.Value);
                return;
            }

            outputStream.WriteByte(0x00);
        }

        /// <summary>
        /// Kirjoita valmis uloskirjoituspuskuri ulosmenevään tietovirtaan
        /// </summary>
        /// <param name="outputBuffer">Uloskirjoituspuskuri</param>
        /// <param name="writeBytes">Kuinka monta tavua puskurin alusta kirjoitetaan ulos</param>
        /// <param name="outputStream">Ulosmenevä tietovirta</param>
        /// <returns>Kirjoitetut tavut</returns>
        private ulong FlushOutputBlock(
            byte[] outputBuffer,
            int writeBytes,
            Stream outputStream)
        {
            outputStream.Write(outputBuffer, 0, writeBytes);

            return (ulong)writeBytes;
        }

        /// <summary>
        /// Kopioi koodibitit yhdestä koodattavasta symbolista uloskirjoituspuskuriin
        /// </summary>
        /// <param name="code">Koodi joka kirjoitetaan</param>
        /// <param name="bits">Bittien lukumäärä koodissa</param>
        /// <param name="outputBuffer">Uloskirjoituspuskuri</param>
        /// <param name="outputBytePointer">Uloskirjoituspuskurin tavuosoitin mitä tavua kirjoitetaan</param>
        /// <param name="outputBitPointer">Uloskirjoituspuskurin bittiosoitin mitä bittiä tavusta kirjoitetaan</param>
        /// <param name="outputStream">Ulosmenevä tietovirta</param>
        /// <returns>Tavujen määrä joita kirjoitettiin mikäli puskuri tuli välissä täyteen</returns>
        public ulong MoveBitsToOutput(
            byte code,
            int bits,
            ref byte[] outputBuffer,
            ref int outputBytePointer,
            ref int outputBitPointer,
            Stream outputStream)
        {
            ulong flushed = 0;

            // Montako bittiä vielä mahtuu nykyiseen uloskirjoitettavaan tavuun?
            var remaining = 8 - outputBitPointer;

            // Console.WriteLine($"outputBitPointer: {outputBitPointer}, outputBytePointer: {outputBytePointer}, remaining: {remaining}, code: {code}, bits: {bits}");

            if (remaining < bits)
            {
                // Koodibitit jakautuu kahden tavun välille, hitaampi tapaus koska voi olla että
                // uloskirjoituspuskuri pitää välissä tyhjätä

                var removeLowerBits = bits - remaining;

                // Ensimmäinen yhdistäminen vasemmanpuoleisimmille biteille
                byte b = code;
                b >>= removeLowerBits;
                outputBuffer[outputBytePointer] |= b;

                outputBytePointer++;

                // Tarkista loppuiko blokki
                if (outputBytePointer >= outputBuffer.Length)
                {
                    flushed += FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
                    outputBytePointer = 0;
                }

                // Nollaa seuraava kirjoitettava tavu puskurissa 
                outputBuffer[outputBytePointer] = 0;

                // Toinen yhdistäminen lopuille biteille normaalissa polussa
                bits -= remaining;

                // Poista koodista yläbitit jotka jo kirjoitettiin
                code <<= 8 - removeLowerBits;
                code >>= 8 - removeLowerBits;

                outputBitPointer = 0;
                remaining = 8;
            }

            /* Laske kuinka paljon bittejä pitää siirtää vasemmalle jotta yhdistettävissä kohteeseen:

                 Uloskirjoitettava tavu:  1011XXXX
                 Koodi:                   YYYYYY10
                                                <<
                                          YYYY1000
                                                OR
                                          1011XXXX
                                                 =
                 Uloskirjoitettava tavu:  101110XX
            */

            var shiftLeft = remaining - bits;

            if (shiftLeft > 0)
                code <<= shiftLeft;

            outputBuffer[outputBytePointer] |= code;
            outputBitPointer += bits;

            if (outputBitPointer == 8)
            {
                outputBitPointer = 0;
                outputBytePointer++;
            }

            if (outputBitPointer == 0 &&
                outputBytePointer < outputBuffer.Length)
            {
                // Nollaa seuraava kirjoitettava tavu puskurissa
                outputBuffer[outputBytePointer] = 0;
            }

            if (outputBytePointer >= outputBuffer.Length)
            {
                flushed += FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
                outputBytePointer = 0;
                outputBitPointer = 0;

                // Nollaa seuraava kirjoitettava tavu puskurissa 
                outputBuffer[outputBytePointer] = 0;
            }

            return flushed;
        }

        /// <summary>
        /// Pakkaa sisääntulevan tietovirran ulosmenevään tietovirtaan käyttäen määriteltyä
        /// Huffman koodaustaulukkoa.
        /// 
        /// Käsittelee tietovirtoja 64kB blokkikoossa.
        /// </summary>
        /// <param name="codeTable">Koodaustaulukko jota käytetään pakkaamiseen</param>
        /// <param name="inputStream">Sisääntuleva tietovirta josta luetaan pakkaamaton syöte</param>
        /// <param name="outputStream">Ulosmenevä tietovirta johon data pakataan</param>
        /// <returns>Pakatun datan koko tavuina</returns>
        private ulong Encode(
            IDictionary<ushort, (uint, int)> codeTable,
            Stream inputStream,
            Stream outputStream)
        {
            ulong compressedBytes = 0;

            var inputBuffer = new byte[64 * 1024];
            var outputBuffer = new byte[64 * 1024];

            var outputBytePointer = 0;
            var outputBitPointer = 0;

            uint temporaryCode;
            byte encodeByte;
            int codeLength;

            while (inputStream.Position < inputStream.Length)
            {
                var read = inputStream.Read(inputBuffer, 0, inputBuffer.Length);
                var inputPointer = 0;

                while (inputPointer < read)
                {
                    // Etsi koodaus seuraavalle tavulle
                    if (!codeTable.TryGetValue(inputBuffer[inputPointer], out var code))
                    {
                        throw new InvalidOperationException("Encountered symbol in input that is not found in code table!");
                    }

                    codeLength = code.Item2;

                    if (codeLength > 8)
                    {
                        // Yli kahdeksan bitin koodi, pilkotaan tavupalasiin ylimmistä biteistä alkaen
                        var bitsToOutput = codeLength - (codeLength / 8 * 8);

                        for (var shiftRounds = codeLength / 8; shiftRounds > 0; shiftRounds--)
                        {
                            temporaryCode = code.Item1;
                            temporaryCode >>= shiftRounds * 8;

                            encodeByte = (byte)(temporaryCode & 0xFF);

                            compressedBytes += MoveBitsToOutput(
                                encodeByte,
                                bitsToOutput,
                                ref outputBuffer,
                                ref outputBytePointer,
                                ref outputBitPointer,
                                outputStream);

                            codeLength -= bitsToOutput;
                            bitsToOutput = 8;
                        }

                        // Jätetään alin tavu jäljelle
                        encodeByte = (byte)(code.Item1 & 0xFF);
                    }
                    else
                    {
                        encodeByte = (byte)code.Item1;
                    }

                    // Lisää alin tai ainoa kooditavu
                    compressedBytes += MoveBitsToOutput(
                            encodeByte,
                            codeLength,
                            ref outputBuffer,
                            ref outputBytePointer,
                            ref outputBitPointer,
                            outputStream);

                    inputPointer++;
                }
            }

            // Kirjoita viimeinen keskeneräinen blokki
            if (outputBytePointer > 0 || outputBitPointer > 0)
            {
                if (outputBitPointer > 0)
                    outputBytePointer++;

                compressedBytes += FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
            }

            outputStream.Flush();

            return compressedBytes;
        }

        /// <summary>
        /// Pakkaa sisääntulevan tietovirran ulosmenevään tietovirtaan käyttäen määriteltyä
        /// Huffman koodaustaulukkoa. Tallentaa pakkaamattoman datan koon ja 
        /// koodaukseen käytetyn Huffman-puun ulostulotiedoston alkuun.
        /// 
        /// Tiedostorakenne:
        /// -- Pakkaamattoman datan koko tavuina (64-bittinen etumerkitön kokonaisluku) --
        /// -- HUFFMAN-PUU --
        /// -- Huffman-puun serialisoinnin lopetusmerkki (1-tavu, 0x03)
        /// -- COMPRESSED DATA --
        /// </summary>
        /// <param name="huffmanTreeRoot">Huffman-symbolipuun juurialkio</param>
        /// <param name="codeTable">Koodaustaulukko jota käytetään pakkaamiseen</param>
        /// <param name="inputStream">Sisääntuleva tietovirta josta luetaan pakkaamaton syöte</param>
        /// <param name="outputStream">Ulosmenevä tietovirta johon data pakataan</param>
        /// <returns>Tuple (pakkaamaton datan koko, pakattu datan koko, koodausaika)</returns>
        public (ulong, ulong, TimeSpan) Encode(
            Node huffmanTreeRoot,
            IDictionary<ushort, (uint, int)> codeTable,
            Stream inputStream,
            Stream outputStream)
        {
            // Aseta sisääntulon ja ulostulon oletetut aloituskohdat ja koot
            inputStream.Position = 0;
            outputStream.SetLength(0);

            ulong inputSize = (ulong)inputStream.Length;

            outputStream.Write(BitConverter.GetBytes(inputSize), 0, 8);

            // Koodaa Huffman-puu tiedostoon
            SerializeHuffmanTree(huffmanTreeRoot, outputStream);

            // Lisää lopetusmerkki serialisoidulle Huffman-puulle
            outputStream.WriteByte(0x03);

            // Aloita koodaus
            var timing = Stopwatch.StartNew();

            ulong compressedSize = Encode(codeTable, inputStream, outputStream);

            var elapsed = timing.Elapsed;

            return (inputSize, compressedSize, elapsed);
        }

        /// <summary>
        /// Deserialisoi Huffman-symbolipuun sisääntulevasta tietovirrasta
        /// </summary>
        /// <param name="inputStream">Sisääntuleva tietovirta</param>
        public Node? DeserializeHuffmanTree(Stream inputStream)
        {
            var decodeStack = new Stack<Node>();

            while (true || inputStream.Position < inputStream.Length)
            {
                var marker = inputStream.ReadByte();

                switch (marker)
                {
                    case 1:
                        // Symbolisolmu
                        var symbol = (byte)inputStream.ReadByte();
                        decodeStack.Push(new Node((symbol, 0)));
                        continue;
                    case 0:
                        // Välisolmu
                        if (decodeStack.Count > 1)
                        {
                            var rightChild = decodeStack.Pop();
                            var leftChild = decodeStack.Pop();

                            var node = new Node(leftChild, rightChild);

                            decodeStack.Push(node);
                            continue;
                        }

                        // Juurisolmu jäljellä
                        if (decodeStack.Count == 1)
                            return decodeStack.Pop();

                        // Epäkelpo rakenne?
                        return null;
                    case 3:
                        // Lopetusmerkki
                        if (decodeStack.Count == 1)
                            return decodeStack.Pop();

                        // Epäkelpo rakenne?
                        return null;
                    default:
                        // Ei-odotettu elementin merkki, tiedosto on epäkelpo?
                        return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Purkaa sisääntulevan tietovirran ulosmenevään tietovirtaan käyttäen määriteltyä
        /// Huffman-puuta.
        /// 
        /// Käsittelee tietovirtoja 64kB blokkikoossa.
        /// </summary>
        /// <param name="huffmanTreeRoot">Huffman-symbolipuun juurisolmu</param>
        /// <param name="expectedDataSize">Odotettu pakkaamattoman datan koko</param>
        /// <param name="inputStream">Sisääntuleva tietovirta</param>
        /// <param name="outputStream">Ulosmenevä tietovirta</param>
        private bool Decode(
            Node huffmanTreeRoot,
            ulong expectedDataSize,
            Stream inputStream,
            Stream outputStream)
        {
            ulong uncompressedBytes = 0;

            var inputBuffer = new byte[64 * 1024];
            var outputBuffer = new byte[64 * 1024];

            var outputBytePointer = 0;

            var bitExponents = new byte[]
            {
                (byte)Math.Pow(2, 7),
                (byte)Math.Pow(2, 6),
                (byte)Math.Pow(2, 5),
                (byte)Math.Pow(2, 4),
                (byte)Math.Pow(2, 3),
                (byte)Math.Pow(2, 2),
                (byte)Math.Pow(2, 1),
                (byte)Math.Pow(2, 0)
            };

            Node node = huffmanTreeRoot;

            while (inputStream.Position < inputStream.Length)
            {
                var read = inputStream.Read(inputBuffer, 0, inputBuffer.Length);
                var inputPointer = 0;
                var inputBitPointer = 0;

                while (inputPointer < read)
                {
                    byte encodedByte = inputBuffer[inputPointer];

                    // Kuljetaan bittijonon perusteella puuta alas kunnes symboli löytyy
                    while (inputBitPointer < 8)
                    {
                        var bit = bitExponents[inputBitPointer];
                        if ((encodedByte & bit) == bit)
                        {
                            // Bitti 1 - oikea lapsi
                            if (node.Right == null)
                                return false;

                            node = node.Right;
                        }
                        else
                        {
                            // Bitti 0 - vasen lapsi
                            if (node.Left == null)
                                return false;

                            node = node.Left;
                        }

                        inputBitPointer++;

                        if (node.Symbol.HasValue)
                        {
                            outputBuffer[outputBytePointer] = (byte)node.Symbol.Value;
                            outputBytePointer++;
                            break;
                        }
                    }

                    if (inputBitPointer == 8)
                    {
                        inputPointer++;
                        inputBitPointer = 0;
                    }

                    if (!node.Symbol.HasValue)
                    {
                        // Bittijono jatkuu seuraavassa sisääntulevassa tavussa
                        continue;
                    }

                    uncompressedBytes++;

                    // Tarkista loppuiko blokki
                    if (outputBytePointer >= outputBuffer.Length)
                    {
                        _ = FlushOutputBlock(outputBuffer, outputBuffer.Length, outputStream);
                        outputBytePointer = 0;
                    }

                    if (uncompressedBytes == expectedDataSize)
                    {
                        // Kaikki purettu
                        break;
                    }

                    // Palauta purkupuu takaisin alkuun
                    node = huffmanTreeRoot;
                }

                if (uncompressedBytes == expectedDataSize)
                {
                    // Kaikki purettu
                    break;
                }
            }

            // Kirjoita viimeinen keskeneräinen blokki
            if (outputBytePointer > 0)
            {
                _ = FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
            }

            outputStream.Flush();

            if (uncompressedBytes != expectedDataSize)
            {
                // Jotain meni pieleen
                return false;
            }

            return true;
        }

        /// <summary>
        /// Purkaa sisääntulevan tietovirran ulosmenevään tietovirtaan käyttäen tietovirran alussa olevaa
        /// Huffman-puuta.
        /// 
        /// Otsakkeessa olevaa pakkaamattoman datan kokoa käytetään jotta tiedetään missä kohti purku on valmis,
        /// koska koodattu bittijono voi loppua keskellä viimeistä tavua.
        /// </summary>
        /// <param name="inputStream">Sisääntuleva tietovirta josta luetaan puu+pakattu syöte</param>
        /// <param name="outputStream">Ulosmenevä tietovirta johon data puretaan</param>
        /// <returns>Dekoodausaika tai null mikäli tiedoston luku epäonnistui</returns>
        public TimeSpan? Decode(
            Stream inputStream,
            Stream outputStream)
        {
            // Aseta sisääntulon ja ulostulon oletetut aloituskohdat ja koot
            inputStream.Position = 0;
            outputStream.SetLength(0);

            byte[] uLongBuffer = new byte[8];

            if (inputStream.Read(uLongBuffer, 0, uLongBuffer.Length) != uLongBuffer.Length)
            {
                // Ei kyetty lukemaan edes oletettua kokoa, virheellinen tiedostosisältö?
                return null;
            }

            var expectedDataSize = BitConverter.ToUInt64(uLongBuffer);

            // Lue Huffman-puu tiedosta
            var huffmanTreeRoot = DeserializeHuffmanTree(inputStream);

            if (huffmanTreeRoot == null)
            {
                // Ei kyetty lukemaan Huffman-symbolipuuta, virheellinen tiedostosisältö?
                return null;
            }

            // Aloita dekoodaus
            var timing = Stopwatch.StartNew();

            if (!Decode(huffmanTreeRoot, expectedDataSize, inputStream, outputStream))
            {
                // Ei kyetty purkamaan dataosuutta, virheellinen tiedostosisältö?
                return null;
            }

            var elapsed = timing.Elapsed;

            return elapsed;
        }
    }
}