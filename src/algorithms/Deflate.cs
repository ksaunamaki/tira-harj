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
        /// Blokin lopetussymboli
        /// </summary>
        public class EndOfBlock : Symbol
        { }

        /// <summary>
        /// LZ koodattu literaali-symboli
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
        /// LZ koodattu takaisinpäinviittaus-symboli
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
            /// Montako extrabittiä symbolin jälkeen tulee tuottaa pituutta varten
            /// </summary>
            public ushort ExtraBitsRequired { get; set; }

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
        private readonly int _inputBlockSize;
        private readonly int _outputBlockSize;

        /// <summary>
        /// Luo uuden Deflate algoritmin, 32-kilotavun ikkunalla ja 64-kilotavun blokkikoolla.
        /// </summary>
        public Deflate()
        {
            _windowSize = 32 * 1024;

            // Deflatessa puskurit ovat 1-1 sisäänluettavan sekä uloskirjoitettavan (maksimi)
            // blokin koon suhteen, seuraavilla huomioilla:

            // - pakkaamaton (BTYPE=0) uloskirjoitettava blokki voi sisältää maksimissaan 65535 tavua (2^16 - 1) dataa.
            // - täten sisäänluettava blokki on syytä olla myös sama 65535 tavua jotta se voidaan kirjoittaa kokonaisena
            //   pakkaamattomaan ulosmenevään blokkiin
            // - pakkaamaton blokki sisältää myös neljä tavua otsakkeita, joten efektiivinen uloskirjoitettavan puskurin koko
            // on 2 + 4 + 65535 = 65541, koska jokaisen uloskirjoitettavan puskurin alussa on blokin otsakebitit, ja
            // pakkaamattomassa blokissa otsakebittien jälkeen on aina tyhjät bitit (0-7 kpl) siten että muut otsakkeet
            // lähtevät tavurajalta (toisin kuin pakatussa blokissa). Joten riippuen mihin kohtaa ensimmäistä tavua blokkiotsakebitit
            // sjoittuvat edellisen blokin seurauksena (josta viimeinen ei-täysi tavu kopioidaan), täytetään loput bitit nollilla joko
            // ensimmäisessä tai toisessa tavussa.

            _inputBlockSize = (64 * 1024) - 1;
            _outputBlockSize = _inputBlockSize + 6;
        }

        private static readonly IDictionary<ushort, (ushort, int)> BackreferenceSymbolValues = new Dictionary<ushort, (ushort, int)>();

        private static void AddSymbolValueForLengthRange(
            ushort lengthStart,
            ushort lengthEnd,
            ushort symbolValue,
            ushort extraBits)
        {
            for (ushort l = lengthStart; l <= lengthEnd; l++)
            {
                BackreferenceSymbolValues.Add(l, (symbolValue, extraBits));
            }
        }

        static Deflate()
        {
            /*
            Luodaan lookup-taulukko takaisinpäinviittausten symbolien pituuksille sekä niiden vaatimille extrabiteille,
            jotta koodauksen aikana arvo löytyy suoraan viittauksen pituutta katsomalla.

            Per https://datatracker.ietf.org/doc/html/rfc1951:

            Code Bits Length(s) Code Bits Lengths   Code Bits Length(s)
            ---- ---- ------     ---- ---- -------   ---- ---- -------
             257   0     3       267   1   15,16     277   4   67-82
             258   0     4       268   1   17,18     278   4   83-98
             259   0     5       269   2   19-22     279   4   99-114
             260   0     6       270   2   23-26     280   4  115-130
             261   0     7       271   2   27-30     281   5  131-162
             262   0     8       272   2   31-34     282   5  163-194
             263   0     9       273   3   35-42     283   5  195-226
             264   0    10       274   3   43-50     284   5  227-257
             265   1  11,12      275   3   51-58     285   0    258
             266   1  13,14      276   3   59-66            
            */

            AddSymbolValueForLengthRange(3, 3, 257, 0);
            AddSymbolValueForLengthRange(4, 4, 258, 0);
            AddSymbolValueForLengthRange(5, 5, 259, 0);
            AddSymbolValueForLengthRange(6, 6, 260, 0);
            AddSymbolValueForLengthRange(7, 7, 261, 0);
            AddSymbolValueForLengthRange(8, 8, 262, 0);
            AddSymbolValueForLengthRange(9, 9, 263, 0);
            AddSymbolValueForLengthRange(10, 10, 264, 0);
            AddSymbolValueForLengthRange(11, 12, 265, 1);
            AddSymbolValueForLengthRange(13, 14, 266, 1);
            AddSymbolValueForLengthRange(15, 16, 267, 1);
            AddSymbolValueForLengthRange(17, 18, 268, 1);
            AddSymbolValueForLengthRange(19, 22, 269, 2);
            AddSymbolValueForLengthRange(23, 26, 270, 2);
            AddSymbolValueForLengthRange(27, 30, 271, 2);
            AddSymbolValueForLengthRange(31, 34, 272, 2);
            AddSymbolValueForLengthRange(35, 42, 273, 3);
            AddSymbolValueForLengthRange(43, 50, 274, 3);
            AddSymbolValueForLengthRange(51, 58, 275, 3);
            AddSymbolValueForLengthRange(59, 66, 276, 3);
            AddSymbolValueForLengthRange(67, 82, 277, 4);
            AddSymbolValueForLengthRange(83, 98, 278, 4);
            AddSymbolValueForLengthRange(99, 114, 279, 4);
            AddSymbolValueForLengthRange(115, 130, 280, 4);
            AddSymbolValueForLengthRange(131, 162, 281, 5);
            AddSymbolValueForLengthRange(163, 194, 282, 5);
            AddSymbolValueForLengthRange(195, 226, 283, 5);
            AddSymbolValueForLengthRange(227, 257, 284, 5);
            AddSymbolValueForLengthRange(258, 258, 285, 5);
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

            // Mikäli ikkuna on > 256 tavua, skannataan oletuksena vain neljännes ikkunan koosta taaksepäin, muuten algoritmi on HIDAS
            // lineaarisella haulla, lisäksi mikäli tulee liian lyhyitä vastaavuuksia, tiputetaan joka
            // kerralla etäisyys vielä puoleen.
            var windowCurrentSize = windowBackPointer - windowFrontPointer;
            ushort max_distance = windowCurrentSize > 256
                ? (ushort)(_windowSize / 4)
                : (ushort)_windowSize;

            var pendingToAddToWindow = new List<byte>();

            // Skannataan taaksepäin ikkunaa
            for (var i = windowBackPointer - 1; i >= windowFrontPointer; i--)
            {
                distance_actual++;

                if (window[i % _windowSize] != nextByte || distance_actual <= MinBackreferenceLength)
                {
                    // Tavujonon alkukohtaa ei vielä löytynyt, jatka taaksepäin TAI etäisyys ei ylitä
                    // minimiviittauspituutta. Jälkimmäinen ehto lisätty jotta vältytään lisäämästä
                    // uusia tavuja ikkunaan (ja siirtämään sen rajoja) ennen kuin osoittautuu että pituus ylittää
                    // minimipituusvaatimuksen mukaisen length=3 (+yksi edge-case tilanteen hoitamiseksi, katso myöhemmin selitys).

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

                        if (length <= MinBackreferenceLength)
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

                                if (windowCurrentSize > 256)
                                {
                                    // Tiputetaan skannauspituutta puoleen
                                    max_distance = (ushort)(max_distance >> 1); // (ushort)((max_distance & 0xFFFE) / 2);
                                }

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

                        if (length < 3)
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
                        }

                        break;
                    }

                    length++;

                    if (length > MinBackreferenceLength && pendingToAddToWindow.Count > 0)
                    {
                        // Tyhjennetään jono ikkunaan, tämä viittaus kelpaa minimipituuden perusteella

                        // Ikkunaan tyhjennetään minimipituus+1 kohdalla, jotta edge-case jossa sisääntuleva blokki
                        // loppuu, ja pituus on sillä hetkellä tasan kolme, ei tuota viittausta jossa pituus tippu kahteen
                        // koska viittauksessa pitää aina olla mukana vielä seuraava tavu = sisääntulevan blokin viimeinen
                        // tavu.
                        foreach (byte b in pendingToAddToWindow)
                        {
                            Lz77.AddInputToWindow(
                                window,
                                _windowSize,
                                b,
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
                if (pendingToAddToWindow.Count > 0)
                {
                    // Pituus tasan kolme, joten ikkunajonoa ei luupissa ehditty tyhjentää
                    foreach (byte b in pendingToAddToWindow)
                    {
                        Lz77.AddInputToWindow(
                            window,
                            _windowSize,
                            b,
                            ref windowFrontPointer,
                            ref windowBackPointer);
                    }
                }

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
        /// Kirjoittaa N koodibittiä uloskirjoitettavaan blokkiin
        /// </summary>
        /// <param name="code">Koodi joka kirjoitetaan</param>
        /// <param name="bits">Bittien lukumäärä koodissa</param>
        /// <param name="outputBlock">Uloskirjoitusblokki</param>
        /// <param name="outputBytePointer">Uloskirjoitusblokin tavuosoitin mitä tavua kirjoitetaan</param>
        /// <param name="outputBitPointer">Uloskirjoitusblokin bittiosoitin mitä bittiä tavusta kirjoitetaan</param>
        /// <returns>True mikäli kirjoitus onnistui, false mikäli blokki loppui kesken</returns>
        public bool OutputBits(
            byte code,
            int bits,
            byte[] outputBlock,
            ref int outputBytePointer,
            ref int outputBitPointer)
        {
            //Console.Write($"{outputBitPointer} ");

            // Montako bittiä vielä mahtuu nykyiseen uloskirjoitettavaan tavuun?
            var remaining = 8 - outputBitPointer;

            if (remaining < bits)
            {
                // Koodibitit jakautuu kahden tavun välille
                var removeLowerBits = bits - remaining;

                // Ensimmäinen yhdistäminen vasemmanpuoleisimmille biteille
                byte b = code;
                b >>= removeLowerBits;
                outputBlock[outputBytePointer] |= b;

                outputBytePointer++;

                // Tarkista loppuiko blokki
                if (outputBytePointer >= outputBlock.Length)
                {
                    return false;
                }

                // Nollaa seuraava kirjoitettava tavu puskurissa 
                outputBlock[outputBytePointer] = 0;

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

            outputBlock[outputBytePointer] |= code;
            outputBitPointer += bits;

            if (outputBitPointer == 8)
            {
                outputBitPointer = 0;
                outputBytePointer++;
            }

            if (outputBitPointer == 0 &&
                outputBytePointer < outputBlock.Length)
            {
                // Nollaa seuraava kirjoitettava tavu puskurissa
                outputBlock[outputBytePointer] = 0;
            }

            return true;
        }


        /// <summary>
        /// Koodataan sisääntuleva blokki symboleiksi
        /// </summary>
        /// <param name="inputBlock">Sisääntuleva blokki</param>
        /// <param name="dataInInputBlock">Tavujen määrä blokissa</param>
        /// <param name="window">Pakkausikkuna</param>
        /// <param name="literals">Koodattujen literaalien määrä</param>
        /// <param name="references">Kooodattujen takaisinpäinviittausten määrä</param>
        /// <param name="outputSymbols">Koodatut symbolit</param>
        /// <param name="symbolFrequencies">Symbolien esiintymistiheydet</param>
        /// <returns>True mikäli koodaus onnistui, false virhetilanteessa</returns>
        public bool EncodeInputBlock(
            byte[] inputBlock,
            int dataInInputBlock,
            byte[] window,
            ref ulong literals,
            ref ulong references,
            out IList<Symbol> outputSymbols,
            out IDictionary<ushort, ulong> symbolFrequencies)
        {
            var inputBlockPointer = 0;
            long windowFrontPointer = 0;
            long windowBackPointer = 0;

            outputSymbols = new List<Symbol>(_inputBlockSize);
            symbolFrequencies = Enumerable.Range(0, 287).ToDictionary(x => (ushort)x, x => 0UL);

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
                    return false;
                }

                outputSymbols.Add(symbol);

                if (symbol is Literal literal)
                {
                    // Literaalitavut saavat tavun arvon as-is
                    literals++;
                    symbolFrequencies[literal.Data]++;
                }
                else if (symbol is Backreference br)
                {
                    // Takaisinpäinviittaukset saavat taulukossa määritellyn kiinteän arvon
                    // perustuen viittauksen pituuteen
                    references++;
                    symbolFrequencies[BackreferenceSymbolValues[br.Length].Item1]++;
                }
            }

            outputSymbols.Add(new EndOfBlock());
            symbolFrequencies[256]++;

            return true;
        }

        private void CreateUncompressedOutputBlock(
            byte[] inputBlock,
            int dataInInputBlock,
            byte[] outputBlock,
            int outputBitPointer,
            out int dataInOutputBlock,
            out int bitsInLastByte)
        {
            var outputBytePointer = 0;

            // Nollataan ensimmäisen tavun käyttämättömät bitit (jotta aiemmasta koodauksesta ei jää bittiroskaa)
            switch (outputBitPointer)
            {
                case 0:
                    outputBlock[0] = 0;
                    break;
                case 1:
                    outputBlock[0] &= 0x80;
                    break;
                case 2:
                    outputBlock[0] &= 0xC0;
                    break;
                case 3:
                    outputBlock[0] &= 0xE0;
                    break;
                case 4:
                    outputBlock[0] &= 0xF0;
                    break;
                case 5:
                    outputBlock[0] &= 0xF8;
                    break;
                case 6:
                    outputBlock[0] &= 0xFC;
                    break;
                case 7:
                    outputBlock[0] &= 0xFE;
                    break;
                default:
                    break;
            }

            // Kirjoitetaan BFINAL otsake
            _ = OutputBits(0, 1, outputBlock, ref outputBytePointer, ref outputBitPointer);
            // Kirjoitetaan BTYPE otsake (= 00 - no compression)
            _ = OutputBits(0, 2, outputBlock, ref outputBytePointer, ref outputBitPointer);

            if (outputBytePointer == 0 ||
                outputBitPointer > 0)
            {
                // Nollataan loput bitit otsakkeen jälkeen jotta pakkaamaton blokki
                // alkaa tavurajasta
                _ = OutputBits(0, 8 - outputBitPointer, outputBlock, ref outputBytePointer, ref outputBitPointer);
            }

            // Kirjoitetaan LEN ja NLEN otsakkeet, multi-byte arvot tallennetaan least-significant byte first -järjestyksessä
            // eli toisinsanoen little-endian muodossa
            ushort len = (ushort)dataInInputBlock;
            var lenBytes = BitConverter.GetBytes(len);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(lenBytes);

            Buffer.BlockCopy(lenBytes, 0, outputBlock, outputBytePointer, 2);
            outputBytePointer += 2;

            // NLEN on yhden komplementti LEN arvosta
            ushort nlen = (ushort)~dataInInputBlock;
            lenBytes = BitConverter.GetBytes(nlen);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(lenBytes);

            Buffer.BlockCopy(lenBytes, 0, outputBlock, outputBytePointer, 2);
            outputBytePointer += 2;

            // Kopioidaan kaikki sisääntulevat tavut ulosmenevään blokkiin.
            // Mahdollista nopeana massakopiointina, koska operoimme tavurajojen mukaan.
            Buffer.BlockCopy(inputBlock, 0, outputBlock, outputBytePointer, dataInInputBlock);

            dataInOutputBlock = outputBytePointer + dataInInputBlock;
            bitsInLastByte = 8;
        }

        private bool OutputOver8BitCode(
            (uint, int) code,
            byte[] outputBlock,
            int codeLength,
            ref int outputBytePointer,
            ref int outputBitPointer)
        {
            byte encodeByte;

            // Yli kahdeksan bitin koodi, pilkotaan tavupalasiin ylimmistä biteistä alkaen
            var bitsToOutput = codeLength - (codeLength / 8 * 8);

            for (var shiftRounds = codeLength / 8; shiftRounds > 0; shiftRounds--)
            {
                var temporaryCode = code.Item1;
                temporaryCode >>= shiftRounds * 8;

                encodeByte = (byte)(temporaryCode & 0xFF);

                if (!OutputBits(encodeByte, bitsToOutput, outputBlock, ref outputBytePointer, ref outputBitPointer))
                    return false;

                codeLength -= bitsToOutput;
                bitsToOutput = 8;
            }

            // Lisää alin kooditavu
            if (!OutputBits((byte)(code.Item1 & 0xFF), codeLength, outputBlock, ref outputBytePointer, ref outputBitPointer))
                return false;

            return true;
        }

        private bool OutputSymbol(
            ushort symbol,
            byte[] outputBlock,
            IDictionary<ushort, (uint, int)> codeTable,
            ref int outputBytePointer,
            ref int outputBitPointer)
        {
            // Etsi koodaus seuraavalle tavulle
            if (!codeTable.TryGetValue(symbol, out var code))
            {
                throw new InvalidOperationException("Encountered symbol in input that is not found in code table!");
            }

            var codeLength = code.Item2;

            if (codeLength > 8)
            {
                return OutputOver8BitCode(code, outputBlock, codeLength, ref outputBytePointer, ref outputBitPointer);
            }

            // Lisää alin tai ainoa kooditavu
            if (!OutputBits((byte)code.Item1, codeLength, outputBlock, ref outputBytePointer, ref outputBitPointer))
                return false;

            return true;
        }

        private bool OutputBackreference(
            Backreference br,
            byte[] outputBlock,
            IDictionary<ushort, (uint, int)> codeTable,
            ref int outputBytePointer,
            ref int outputBitPointer)
        {
            // TODO
            return false;
        }

        /// <summary>
        /// Muodostetaan uloskirjoitettava blokki perustuen koodattuihin symboleihin. Mikäli koodattu blokki
        /// kasvaa isommaksi kuin alkuperäinen sisääntuleva blokki, palautetaan koodamaton blokki.
        /// </summary>
        /// <param name="huffmanRoot">Huffman-symbolipuun juurisolmu</param>
        /// <param name="codeTable">Huffman koodaustaulukko</param>
        /// <param name="outputSymbols">Koodattavat symbolit</param>
        /// <param name="inputBlock">Sisääntuleva blokki</param>
        /// <param name="dataInInputBlock">Sisällön määrä sisääntulevassa blokissa tavuina</param>
        /// <param name="outputBlock">Uloskirjoitettava blokki</param>
        /// <param name="outputBitPointer">Uloskirjoitettavan blokin ensimmäisen tavun alun bittirajaosoitin</param>
        /// <param name="dataInOutputBlock">Uloskirjoitettavan blokin sisältämä tietomäärä kokonaisina tavuina</param>
        /// <param name="bitsInLastByte">Uloskirjoitettavan blokin viimeisen tietotavun bittien määrä</param>
        /// <param name="headerStartBitOffset">Uloskirjoitettavan blokin ensimmäisen tavun otsakkeen bittiosoitin</param>
        /// <returns>true mikäli blokki on kompressoitu, false mikäli kompressoimaton</returns>
        public bool CreateOutputBlock(
            Huffman.Node huffmanRoot,
            IDictionary<ushort, (uint, int)> codeTable,
            IList<Symbol> outputSymbols,
            byte[] inputBlock,
            int dataInInputBlock,
            byte[] outputBlock,
            int outputBitPointer,
            out int dataInOutputBlock,
            out int bitsInLastByte,
            out int headerStartBitOffset)
        {
            // Montako kokonaista tavua + vajaa lopputavu uloskirjoitettavassa blokissa on
            dataInOutputBlock = 0;
            // Montako bittiä viimeisestä tavusta on käytetty
            bitsInLastByte = 0;

            var outputBytePointer = 0;

            headerStartBitOffset = outputBitPointer;

            // Kirjoitetaan BFINAL otsake
            _ = OutputBits(0, 1, outputBlock, ref outputBytePointer, ref outputBitPointer);
            // Kirjoitetaan BTYPE otsake (= 10 - compressed with dynamic Huffman codes)
            _ = OutputBits(2, 2, outputBlock, ref outputBytePointer, ref outputBitPointer);

            // TODO: muodostetaan Distance code Huffman arvot

            // TODO: muodostetaan Code Length sekvenssien Huffman arvot


            // TODO: koodataan (koodatut) Literal/Length codet tähän väliin

            // TODO: koodataan Distance codet tähän väliin

            // TODO: koodataan Code Length codet tähän väliin


            // Koodataan symbolit

            var reEncodeToUncompressed = false;

            foreach (var symbol in outputSymbols)
            {
                if (symbol is Literal literal)
                {
                    if (!OutputSymbol(
                        literal.Data,
                        outputBlock,
                        codeTable,
                        ref outputBytePointer,
                        ref outputBitPointer))
                    {
                        // Koodattu esitys meni pidemmäksi kuin pakkaamaton
                        reEncodeToUncompressed = true;
                        break;
                    }

                    continue;
                }

                if (symbol is Backreference br)
                {
                    if (!OutputBackreference(
                        br,
                        outputBlock,
                        codeTable,
                        ref outputBytePointer,
                        ref outputBitPointer))
                    {
                        // Koodattu esitys meni pidemmäksi kuin pakkaamaton
                        reEncodeToUncompressed = true;
                        break;
                    }

                    continue;
                }

                if (symbol is EndOfBlock eob)
                {
                    if (!OutputSymbol(
                        256,
                        outputBlock,
                        codeTable,
                        ref outputBytePointer,
                        ref outputBitPointer))
                    {
                        // Koodattu esitys meni pidemmäksi kuin pakkaamaton
                        reEncodeToUncompressed = true;
                        break;
                    }
                }
            }

            if (reEncodeToUncompressed)
            {
                CreateUncompressedOutputBlock(
                    inputBlock,
                    dataInInputBlock,
                    outputBlock,
                    headerStartBitOffset,
                    out dataInOutputBlock,
                    out bitsInLastByte);

                return false;
            }

            bitsInLastByte = outputBitPointer;

            return true;
        }

        /// <summary>
        /// Pakkaa sisääntulevan tietovirran ulosmenevään tietovirtaan.
        /// 
        /// </summary>
        /// <param name="window">Pakkausikkuna</param>
        /// <param name="inputStream">Sisääntuleva tietovirta josta luetaan pakkaamaton syöte</param>
        /// <param name="outputStream">Ulosmenevä tietovirta johon data pakataan</param>
        /// <returns>Tuple (pakattu datan koko, literaalien määrä, viittausten määrä, kompressoitujen blokkien määrä, kompressoimattomien blokkien määrä)</returns>
        public (ulong, ulong, ulong, ulong, ulong) Encode(
            byte[] window,
            Stream inputStream,
            Stream outputStream)
        {
            ulong compressedBytes = 0;

            var inputBlock = new byte[_inputBlockSize];
            var outputSymbols = default(IList<Symbol>);
            var symbolFrequencies = default(IDictionary<ushort, ulong>);

            ulong literals = 0;
            ulong references = 0;
            ulong compressedBlocks = 0;
            ulong uncompressedBlocks = 0;

            int dataInInputBlock;

            var outputBlock = new byte[_outputBlockSize];
            var writeOutputBlock = false;
            var dataInOutputBlock = 0;
            var bitsInOutputBlockLastByte = 0;
            var headerStartBitOffset = 0;

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
                    return (0, 0, 0, 0, 0);
                }

                // Onko edelliseltä kierrokselta ulosmenevää blokkia kirjoitusvalmiina?
                if (writeOutputBlock)
                {
                    var isLastBlock = dataInInputBlock <= 0;

                    if (bitsInOutputBlockLastByte < 8 && !isLastBlock)
                    {
                        // Blokin viimeinen tavu ei osu tavurajaan, joten se pitää siirtää
                        // osaksi seuraavaa blokkia jossa sitä jatketaan uuden blokin otsakkeella
                        outputStream.Write(outputBlock, 1, dataInOutputBlock - 1);
                        compressedBytes += (ulong)dataInOutputBlock - 1;

                        outputBlock[0] = outputBlock[dataInOutputBlock - 1];
                    }
                    else
                    {
                        if (isLastBlock)
                        {
                            // Käännetään BFINAL bitti viimeisen blokin merkiksi
                            outputBlock[0] = (byte)(outputBlock[0] | (int)Math.Pow(2, 8 - headerStartBitOffset));
                        }

                        outputStream.Write(outputBlock, 0, dataInOutputBlock);
                        compressedBytes += (ulong)dataInOutputBlock;
                    }

                    // Asetetaan bittiosoitin tarvittaessa takaisin nollaan
                    if (bitsInOutputBlockLastByte == 8)
                        bitsInOutputBlockLastByte = 0;
                }

                if (dataInInputBlock <= 0)
                {
                    // Sisääntuleva tietovirta on loppu
                    break;
                }

                // Vaihe 1 - Etsitään ikkunasta vastaavuutta käsiteltäville tavuille LZ77 algoritmilla
                // kunnes kaikki sisääntulevan blokin data on luettu. 
                // Muodostetaan samalla Deflate-algoritmissa määritettyjen
                // sallittujen symboolien (0-285 + 286,287) esiintymistiheydet Huffman-puuta varten.

                if (!EncodeInputBlock(
                    inputBlock,
                    dataInInputBlock,
                    window,
                    ref literals,
                    ref references,
                    out outputSymbols,
                    out symbolFrequencies))
                {
                    return (0, 0, 0, 0, 0);
                }

                // Vaihe 2 - luodaan Huffman puu nähdyistä symboleista
                var root = Huffman.BuildHuffmanTree(symbolFrequencies
                    .Where(pair => pair.Value > 0)
                    .Select(pair => (pair.Key, pair.Value))
                    .OrderBy(pair => pair.Value));

                var codeTable = Huffman.BuildCodeTable(root);

                // Vaihe 3 - koodataan ulosmenevä blokki
                if (CreateOutputBlock(
                    root,
                    codeTable,
                    outputSymbols,
                    inputBlock,
                    dataInInputBlock,
                    outputBlock,
                    bitsInOutputBlockLastByte,
                    out dataInOutputBlock,
                    out bitsInOutputBlockLastByte,
                    out headerStartBitOffset))
                    compressedBlocks++;
                else
                    uncompressedBlocks++;

                writeOutputBlock = true;

            } while (dataInInputBlock > 0);

            return (compressedBytes, literals, references, compressedBlocks, uncompressedBlocks);
        }

        /// <summary>
        /// Pakkaa sisääntulevan tietovirran ulosmenevään tietovirtaan Deflate algoritmilla.
        /// 
        /// Uloskirjoitettava tietovirta koostuu sarjasta loogisia blokkeja, jotka voivat olla joko LZ77 koodattu
        /// dynaamisella Huffman -puulla, LZ77-koodattu Deflate-algoritmissa määritellyllä kiinteällä Huffman -puulla, 
        /// tai täysin pakkaamattomia. Tämä toteutus generoi vain joko dynaamisia Huffman koodattuja tai pakkaamattomia
        /// blokkeja (mikäli Huffman koodaus tuottaa alkuperäistä isomman pakkauksen).
        /// 
        /// Deflate algoritmissa jokainen uloskirjoitettava blokki on tasan sen pituinen kuin siihen kirjoitetut
        /// bitit, eivätkä ne välttämättä pääty tai ala tavurajojen mukaan pl. viimeinen blokki jossa luonnollisesti
        /// päättyy tietovirran viimeiseen tavuun (mutta loogisesti mahdollisesti kesken tavua.)
        /// </summary>
        /// <param name="inputStream">Sisääntuleva tietovirta josta luetaan pakkaamaton syöte</param>
        /// <param name="outputStream">Ulosmenevä tietovirta johon data pakataan</param>
        /// <returns>Tuple (pakkaamaton datan koko, pakattu datan koko, literaalien määrä, viittausten määrä, 
        /// kompressoitujen blokkien määrä, kompressoimattomien blokkien määrä, koodausaika)</returns>
        public (ulong, ulong, ulong, ulong, ulong, ulong, TimeSpan) Encode(
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

            (ulong compressedSize,
             ulong literals,
             ulong references,
             ulong compressedBlocks,
             ulong uncompressedBlocks) = Encode(window, inputStream, outputStream);

            var elapsed = timing.Elapsed;

            return (inputSize, compressedSize, literals, references, compressedBlocks, uncompressedBlocks, elapsed);
        }
    }
}