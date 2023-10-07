using System.Diagnostics;

namespace Tiracompress.Algorithms
{
    /// <summary>
    /// Tämä luokka toteuttaa Deflate -algoritmiin perustuvan tiedon koodauksen/pakkauksen sekä purkamisen
    /// </summary>
    public class Deflate
    {
        /// <summary>
        /// Minimipituus LZ-pakatun datan takaisinpäinviittauksessa
        /// </summary>
        public const int MinBackreferenceLength = 3;

        /// <summary>
        /// Maksimipituus LZ-pakatun datan takaisinpäinviittauksessa
        /// </summary>
        public const int MaxBackreferenceLength = 258;

        /// <summary>
        /// LZ koodatun symbolin perusluokka
        /// </summary>
        public abstract class Symbol
        { }

        /// <summary>
        /// LZ koodattu literaali-symbooli
        /// </summary>
        public class Literal : Symbol
        {
            /// <summary>
            /// Literaalitavu
            /// </summary>
            public byte Data { get; }

            /// <summary>
            /// Uusi literaali-symbooli
            /// </summary>
            /// <param name="data"></param>
            public Literal(byte data)
            {
                Data = data;
            }
        }

        /// <summary>
        /// LZ koodattu takaisinpäinviittaus-symbooli
        /// </summary>
        public class Backreference : Symbol
        {
            /// <summary>
            /// Etäisyys taaksepäin tavuina
            /// </summary>
            public ushort Distance { get; }
            /// <summary>
            /// Viittauksen pituus tavuina
            /// </summary>
            public ushort Length { get; }
            /// <summary>
            /// Seuraava literaalitavu viittauksen jälkeen
            /// </summary>
            public byte Next { get; }

            /// <summary>
            /// Uusi takaisinpäinviittaus
            /// </summary>
            /// <param name="distance"></param>
            /// <param name="length"></param>
            /// <param name="next"></param>
            public Backreference(
                ushort distance,
                ushort length,
                byte next)
            {
                Distance = distance;
                Length = length;
                Next = next;
            }
        }

        private readonly int _windowSize;
        private readonly int _blockSize;

        /// <summary>
        /// Luo uuden Deflate algoritmin, 32-kilotavun ikkunalla ja 64-kilotavun blokkikoolla.
        /// </summary>
        public Deflate()
        {
            _windowSize = 32 * 1024;

            // Deflatessa puskurit ovat 1-1 sisäänluettavan sekä uloskirjoitettavan (maksimi)
            // blokin koon suhteen.
            _blockSize = 64 * 1024;
        }

        /// <summary>
        /// Etsii jo nähdyn sisääntulevan tietovirran ikkunasta ensimmäisen vastaavuuden seuraaviin
        /// sisääntuleviin tavuihin skannaamalla ikkunaa takaperin.
        /// 
        /// Etsintä EI ulotu ikkunassa mahdollisiin aiempiin vastaavuuksiin vaan vain ensimmäinen
        /// vastaavuus palautetaan (vaikka aiemmin olisi pidempiä vastaavuuksia!)
        /// </summary>
        /// <param name="inputBlock">Käsiteltävä sisääntuleva blokki</param>
        /// <param name="inputPointer">Sisääntulevan blokin seuraavan tavun osoitin</param>
        /// <param name="dataInBlock">Sisääntulevan blokin sisältämä tietomäärä tavuina</param>
        /// <param name="window">Ikkunapuskuri josta vastaavuksia skannataan</param>
        /// <param name="windowFrontPointer">Ikkunan looginen alkukohta</param>
        /// <param name="windowBackPointer">Ikkunan looginen loppukohta</param>
        /// <returns>Literaali symboli, takaisinpäinviittaus tai null mikäli sisääntuleva blokki on loppu </returns>
        public Symbol? LzScanWindowForMatch(
            byte[] inputBlock,
            ref int inputPointer,
            int dataInBlock,
            byte[] window,
            ref long windowFrontPointer,
            ref long windowBackPointer)
        {
            if (inputPointer >= dataInBlock)
            {
                return null;
            }

            var nextByte = inputBlock[inputPointer];
            inputPointer++;

            ushort distance_actual = 0;
            ushort distance = 0;
            ushort length = 0;

            // Skannataan oletuksena vain neljännes ikkunan koosta taaksepäin, muuten algoritmi on HIDAS
            // lineaarisella haulla, lisäksi mikäli tulee liian lyhyitä vastaavuuksia, tiputetaan joka
            // kerralla vielä puoleen.
            ushort max_distance = (ushort)(_windowSize / 4);

            var pendingToAddToWindow = new List<byte>();

            // Skannataan taaksepäin ikkunaa
            for (var i = windowBackPointer - 1; i >= windowFrontPointer; i--)
            {
                distance_actual++;

                if (window[i % _windowSize] != nextByte || distance_actual < MinBackreferenceLength)
                {
                    // Tavujonon alkukohtaa ei vielä löytynyt, jatka taaksepäin TAI etäisyys ei ole vähintään
                    // minimiviittauspituuden verran. Jälkimmäinen ehto lisätty jotta vältytään lisäämästä
                    // uusia tavuja ikkunaan (ja siirtämään sen rajoja) ennen kuin osoittautuu että pituus täyttää
                    // minimipituusvaatimukset.

                    if (distance_actual > max_distance)
                    {
                        break;
                    }

                    continue;
                }

                // Ensimmäinen vastaavuus löytyi, aloitetaan lisäämään uutta sisääntulevaa tavuvirtaa 
                // jonoon
                pendingToAddToWindow.Add(nextByte);

                // Asetetaan etäisyysviittaus alkukohtaan
                distance = distance_actual;
                length = 1;

                // Skannataan eteenpäin kuinka monta lisätavua löytyy vastaavuutta
                // Huom: koska seuraavat sisäänluetut tavut lisätään sitä mukaan osaksi ikkunaa, voi pituusviittaus
                // viitata myös uusiin tavuihin - tämä on LZ77:ssa täysin laillista. 
                long j;
                var restoreNextByte = nextByte;
                bool restoreBackscan = false;

                for (j = i + 1; j < windowBackPointer; j++)
                {
                    if (inputPointer < dataInBlock)
                    {
                        nextByte = inputBlock[inputPointer];
                        inputPointer++;

                        if (length < MinBackreferenceLength)
                        {
                            pendingToAddToWindow.Add(nextByte);
                        }
                        else
                        {
                            Lz77.AddInputToWindow(
                                window,
                                _windowSize,
                                nextByte,
                                ref windowFrontPointer,
                                ref windowBackPointer);
                        }

                        if (window[j % _windowSize] != nextByte)
                        {
                            // Seuraava tavu ei enää löytynyt ikkunasta
                            if (length < MinBackreferenceLength)
                            {
                                // Palautetaan haku takaisin edellisen löytökohdan kohdalle
                                restoreBackscan = true;
                                nextByte = restoreNextByte;
                                inputPointer -= length;
                                pendingToAddToWindow.Clear();

                                length = 0;
                                distance = 0;

                                // Tiputetaan skannauspituutta puoleen
                                max_distance = (ushort)((max_distance & 0xFFFE) / 2);

                                break;
                            }

                            break;
                        }

                        if (length == MaxBackreferenceLength)
                        {
                            // Maksimipituus täyttyi
                            break;
                        }
                    }
                    else
                    {
                        // Syöte loppui, vähennä pituudesta yksi jotta viimeinen luettu tavu jää omaksi
                        // viitteekseen
                        length--;

                        if (length == 0)
                            distance = 0;

                        break;
                    }

                    length++;

                    if (length >= MinBackreferenceLength && pendingToAddToWindow.Count > 0)
                    {
                        // Tyhjennetään jono ikkunaan, tämä viittaus kelpaa minimipituuden perusteella
                        foreach (byte b in pendingToAddToWindow)
                        {
                            Lz77.AddInputToWindow(
                                window,
                                _windowSize,
                                pendingToAddToWindow[0],
                                ref windowFrontPointer,
                                ref windowBackPointer);
                        }
                        pendingToAddToWindow.Clear();
                    }
                }

                if (restoreBackscan)
                {
                    // Palataan takaisin skannaamaan taaksepäin
                    continue;
                }

                break;
            }

            if (distance > 0)
            {
                // Palauta takaisinpäinviittaus
                return new Backreference(distance, length, nextByte);
            }

            // Lisää ainoa luettu tavu ikkunaan
            Lz77.AddInputToWindow(
                window,
                _windowSize,
                nextByte,
                ref windowFrontPointer,
                ref windowBackPointer);


            // Palauta literaali symbooli
            return new Literal(nextByte);
        }

        /// <summary>
        /// Pakkaa sisääntulevan tietovirran ulosmenevään tietovirtaan.
        /// </summary>
        /// <param name="window">Pakkausikkuna</param>
        /// <param name="inputStream">Sisääntuleva tietovirta josta luetaan pakkaamaton syöte</param>
        /// <param name="outputStream">Ulosmenevä tietovirta johon data pakataan</param>
        /// <returns>Tuple (pakattu datan koko, literaalien määrä, viittausten määrä)</returns>
        public (ulong, ulong, ulong) Encode(
            byte[] window,
            Stream inputStream,
            Stream outputStream)
        {
            ulong compressedBytes = 0;

            var inputBlock = new byte[_blockSize];
            var inputBlockPointer = -1;
            var outputSymbols = new List<Symbol>(_blockSize);

            long windowFrontPointer = 0;
            long windowBackPointer = 0;

            ulong literals = 0;
            ulong references = 0;

            int dataInInputBlock;

            do
            {
                // Luetaan ja prosessoidaan blokki kerrallaan
                try
                {
                    dataInInputBlock = inputStream.Read(inputBlock, 0, inputBlock.Length);
                }
                catch
                {
                    // Lukeminen ei onnistunut!
                    return (0, 0, 0);
                }

                if (dataInInputBlock <= 0)
                {
                    // Sisääntuleva tietovirta on loppu
                    break;
                }

                // Nollataan arvot jokaiselle blokille
                inputBlockPointer = 0;
                windowFrontPointer = 0;
                windowBackPointer = 0;

                // Etsitään ikkunasta vastaavuutta seuraaville tavuille LZ algoritmilla
                // kunnes kaikki data on luettu
                while (inputBlockPointer < dataInInputBlock)
                {
                    var symbol = LzScanWindowForMatch(
                        inputBlock,
                        ref inputBlockPointer,
                        dataInInputBlock,
                        window,
                        ref windowFrontPointer,
                        ref windowBackPointer);

                    if (symbol == null)
                    {
                        // Virhetilanne blokkien käsittelyssä!
                        return (0, 0, 0);
                    }

                    outputSymbols.Add(symbol);
                }

                // TODO: lasketaan Huffman puu symboleilles
                foreach (var symbol in outputSymbols)
                {
                    if (symbol is Literal)
                        literals++;
                    else
                        references++;
                }

                // TODO: kirjoitetaan valmis blokki ulos

            } while (dataInInputBlock > 0);

            return (compressedBytes, literals, references);
        }

        /// <summary>
        /// Pakkaa sisääntulevan tietovirran ulosmenevään tietovirtaan Deflate algoritmilla.
        /// 
        /// </summary>
        /// <param name="inputStream">Sisääntuleva tietovirta josta luetaan pakkaamaton syöte</param>
        /// <param name="outputStream">Ulosmenevä tietovirta johon data pakataan</param>
        /// <returns>Tuple (pakkaamaton datan koko, pakattu datan koko, literaalien määrä, viittausten määrä, koodausaika)</returns>
        public (ulong, ulong, ulong, ulong, TimeSpan) Encode(
            Stream inputStream,
            Stream outputStream)
        {
            // Aseta sisääntulon ja ulostulon oletetut aloituskohdat ja koot
            inputStream.Position = 0;
            outputStream.SetLength(0);

            ulong inputSize = (ulong)inputStream.Length;

            // Aloita koodaus
            var timing = Stopwatch.StartNew();

            // Luo uusi ikkuna
            var window = new byte[_windowSize];

            (ulong compressedSize, ulong literals, ulong references) = Encode(window, inputStream, outputStream);

            var elapsed = timing.Elapsed;

            return (inputSize, compressedSize, literals, references, elapsed);
        }
    }
}