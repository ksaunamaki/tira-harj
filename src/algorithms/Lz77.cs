using System.Diagnostics;

namespace Tiracompress.Algorithms
{
    /// <summary>
    /// Tämä luokka toteuttaa LZ77 -algoritmiin perustuvan tiedon koodauksen/pakkauksen sekä purkamisen
    /// </summary>
    public class Lz77
    {
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

        private readonly int _windowMaxSize;

        /// <summary>
        /// Luo uuden LZ77 algoritmin halutulla ikkunan koolla
        /// </summary>
        /// <param name="windowSize">Ikkunan koko tavuina</param>
        public Lz77(int windowSize)
        {
            _windowMaxSize = windowSize;
        }

        /// <summary>
        /// Simuloi yksittäisen tavun lukemista sisääntulevasta tietovirrasta, käyttäen
        /// puskurointia välissä
        /// </summary>
        /// <param name="inputStream">Sisääntuleva tietovirta</param>
        /// <param name="inputBuffer">Sisääntulevan tietovirran puskuri</param>
        /// <param name="inputPointer">Sisääntulevan tietovirran puskurin seuraavan tavun osoitin</param>
        /// <param name="dataInBuffer">Sisääntulevan tietovirran puskurin sisältämä tietomäärä tavuina</param>
        private byte? GetByteFromInput(
            Stream inputStream,
            byte[] inputBuffer,
            ref int inputPointer,
            ref int dataInBuffer)
        {
            if (inputPointer >= inputBuffer.Length ||
                inputPointer == -1)
            {
                dataInBuffer = inputStream.Read(inputBuffer, 0, inputBuffer.Length);
                inputPointer = 0;
            }

            if (inputPointer >= dataInBuffer)
            {
                return null;
            }

            var b = inputBuffer[inputPointer];
            inputPointer++;

            return b;
        }

        /// <summary>
        /// Lisätään uusi sisääntuleva tavu ikkunaan
        /// </summary>
        /// <param name="window">Ikkunapuskuri johon lisätään</param>
        /// <param name="input">Tavu joka lisätään</param>
        /// <param name="windowFrontPointer">Ikkunan looginen alkukohta</param>
        /// <param name="windowBackPointer">Ikkunan looginen loppukohta</param>
        public void AddInputToWindow(
            byte[] window,
            byte input,
            ref long windowFrontPointer,
            ref long windowBackPointer)
        {
            window[windowBackPointer % _windowMaxSize] = input;

            // Siirrä loogisia ikkunan aloitus ja lopetusrajoja

            if ((int)(windowBackPointer - windowFrontPointer) < _windowMaxSize)
            {
                // Ikkuna ei ole vielä kasvanut maksimiin
                windowBackPointer++;
            }
            else
            {
                windowFrontPointer++;
                windowBackPointer++;
            }
        }

        /// <summary>
        /// Etsii jo nähdyn sisääntulevan tietovirran ikkunasta ensimmäisen vastaavuuden seuraaviin
        /// sisääntuleviin tavuihin skannaamalla ikkunaa takaperin.
        /// 
        /// Etsintä EI ulotu ikkunassa mahdollisiin aiempiin vastaavuuksiin vaan vain ensimmäinen
        /// vastaavuus palautetaan (vaikka aiemmin olisi pidempiä vastaavuuksia!)
        /// </summary>
        /// <param name="inputStream">Sisääntuleva tietovirta</param>
        /// <param name="inputBuffer">Sisääntulevan tietovirran puskuri</param>
        /// <param name="inputPointer">Sisääntulevan tietovirran puskurin seuraavan tavun osoitin</param>
        /// <param name="dataInBuffer">Sisääntulevan tietovirran puskurin sisältämä tietomäärä tavuina</param>
        /// <param name="window">Ikkunapuskuri josta vastaavuksia skannataan</param>
        /// <param name="windowFrontPointer">Ikkunan looginen alkukohta</param>
        /// <param name="windowBackPointer">Ikkunan looginen loppukohta</param>
        /// <returns></returns>
        private (ushort, byte, byte?) ScanWindowForMatch(
            Stream inputStream,
            byte[] inputBuffer,
            ref int inputPointer,
            ref int dataInBuffer,
            byte[] window,
            ref long windowFrontPointer,
            ref long windowBackPointer)
        {
            var firstByte = GetByteFromInput(inputStream, inputBuffer, ref inputPointer, ref dataInBuffer);

            if (!firstByte.HasValue)
                return (0, 0, null);

            ushort distance_actual = 0;
            ushort distance = 0;
            byte length = 0;
            byte? nextByte = null;

            // Skannataan taaksepäin ikkunaa
            for (var i = windowBackPointer - 1; i >= windowFrontPointer; i--)
            {
                //Console.WriteLine($"ScanWindowForMatch (0x{firstByte:X2}): i: {i} ({i % _windowMaxSize})");

                distance_actual++;

                if (window[i % _windowMaxSize] != firstByte)
                {
                    // Tavujonon alkukohtaa ei vielä löytynyt, jatka taaksepäin
                    continue;
                }

                // Ensimmäinen vastaavuus löytyi, aloitetaan lisäämään uutta sisääntulevaa tavuvirtaa ikkunaan
                AddInputToWindow(window, firstByte.Value, ref windowFrontPointer, ref windowBackPointer);

                // Asetetaan etäisyysviittaus alkukohtaan
                distance = distance_actual;
                length = 1;

                // Skannataan eteenpäin kuinka monta lisätavua löytyy vastaavuutta
                // Huom: koska seuraavat sisäänluetut tavut lisätään sitä mukaan osaksi ikkunaa, voi pituusviittaus
                // viitata myös uusiin tavuihin - tämä on LZ77:ssa täysin laillista. 
                long j;

                for (j = i + 1; j < windowBackPointer; j++)
                {
                    var nextByte_tmp = GetByteFromInput(inputStream, inputBuffer, ref inputPointer, ref dataInBuffer);

                    if (nextByte_tmp != null)
                    {
                        AddInputToWindow(window, nextByte_tmp.Value, ref windowFrontPointer, ref windowBackPointer);

                        if (window[j % _windowMaxSize] != nextByte_tmp.Value)
                        {
                            // Seuraava tavu ei enää löytynyt ikkunasta
                            nextByte = nextByte_tmp;
                            break;
                        }

                        if (length == byte.MaxValue)
                        {
                            // Maksimipituus täyttyi
                            nextByte = nextByte_tmp;
                            break;
                        }
                    }
                    else
                    {
                        // Syöte loppui, vähennä pituudesta yksi jotta viimeinen luettu tavu jää omaksi
                        // viitteeseen
                        length--;

                        if (length == 0)
                            distance = 0;

                        break;
                    }

                    length++;
                }

                break;
            }

            if (distance == 0)
            {
                AddInputToWindow(window, firstByte.Value, ref windowFrontPointer, ref windowBackPointer);
            }

            return (distance, length, nextByte ?? firstByte);
        }

        /// <summary>
        /// Kirjoita ulosmenevään puskuriin yksittäinen sisääntuleva tavu sellaisenaan
        /// </summary>
        /// <param name="outputByte">Tavu joka kirjoitetaan</param>
        /// <param name="outputBuffer">Ulosmenevän pakkauksen puskuri</param>
        /// <param name="outputBytePointer">Ulosmenevän pakkauksen puskurin tavuosoitin</param>
        /// <param name="outputStream">Ulosmenevä tietovirta</param>
        /// <param name="compressedBytes">Tähän saakka pakattujen tavujen määrä</param>
        public void OutputLiteralData(
            byte outputByte,
            byte[] outputBuffer,
            ref int outputBytePointer,
            Stream outputStream,
            ref ulong compressedBytes)
        {
            outputBuffer[outputBytePointer] = 0;
            outputBytePointer++;

            if (outputBytePointer >= outputBuffer.Length)
            {
                compressedBytes += FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
                outputBytePointer = 0;
            }

            outputBuffer[outputBytePointer] = 0;
            outputBytePointer++;

            if (outputBytePointer >= outputBuffer.Length)
            {
                compressedBytes += FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
                outputBytePointer = 0;
            }

            outputBuffer[outputBytePointer] = outputByte;
            outputBytePointer++;
        }

        /// <summary>
        /// Kirjoita ulosmenevään puskuriin takaisinpäin viittaus ikkunasta löytyneeseen tavujonoon 
        /// sekä sitä seuraava sisääntuleva tavu sellaisenaan
        /// </summary>
        /// <param name="distance">Etäisyys taaksepäin ikkunassa vastaavuuden alkuun</param>
        /// <param name="length">Tavujen määrä ikkunassa jotka vastaavat sisääntulevaa tavujonoa</param>
        /// <param name="outputBuffer">Ulosmenevän pakkauksen puskuri</param>
        /// <param name="outputBytePointer">Ulosmenevän pakkauksen puskurin tavuosoitin</param>
        /// <param name="outputStream">Ulosmenevä tietovirta</param>
        /// <param name="compressedBytes">Tähän saakka pakattujen tavujen määrä</param>
        /// <param name="nextChar">Tavu joka kirjoitetaan viittauksen jälkeen</param>
        public void OutputReferencedData(
            ushort distance,
            byte length,
            byte[] outputBuffer,
            ref int outputBytePointer,
            Stream outputStream,
            ref ulong compressedBytes,
            byte? nextChar)
        {
            // Kirjoita etäisyyden alempi tavu (LE järjestys)
            outputBuffer[outputBytePointer] = (byte)(distance & 0xFF);
            outputBytePointer++;

            if (outputBytePointer >= outputBuffer.Length)
            {
                compressedBytes += FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
                outputBytePointer = 0;
            }

            // Kirjoita etäisyyden ylempi tavu (LE järjestys)
            outputBuffer[outputBytePointer] = (byte)(distance >> 8);
            outputBytePointer++;

            if (outputBytePointer >= outputBuffer.Length)
            {
                compressedBytes += FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
                outputBytePointer = 0;
            }

            // Kirjoita pituus
            outputBuffer[outputBytePointer] = length;
            outputBytePointer++;

            if (outputBytePointer >= outputBuffer.Length)
            {
                compressedBytes += FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
                outputBytePointer = 0;
            }

            // Kirjoita seuraava tavu
            if (nextChar.HasValue)
            {
                outputBuffer[outputBytePointer] = nextChar.Value;
                outputBytePointer++;
            }
        }

        /// <summary>
        /// Pakkaa sisääntulevan tietovirran ulosmenevään tietovirtaan käyttäen määriteltyä
        /// LZ77 ikkunaa koodaustaulukkoa.
        /// </summary>
        /// <param name="window">Pakkausikkuna</param>
        /// <param name="inputStream">Sisääntuleva tietovirta josta luetaan pakkaamaton syöte</param>
        /// <param name="outputStream">Ulosmenevä tietovirta johon data pakataan</param>
        /// <returns>Pakatun datan koko tavuina</returns>
        private ulong Encode(
            byte[] window,
            Stream inputStream,
            Stream outputStream)
        {
            ulong compressedBytes = 0;

            var inputBuffer = new byte[64 * 1024];
            var outputBuffer = new byte[64 * 1024];

            var inputPointer = -1;
            var outputBytePointer = 0;
            var dataInBuffer = 0;

            long windowFrontPointer = 0;
            long windowBackPointer = 0;

            long literals = 0;
            long references = 0;

            byte? nextByte;

            do
            {
                // Etsitään ikkunasta vastaavuutta seuraaville tavuille
                (ushort distance, byte length, nextByte) = ScanWindowForMatch(
                    inputStream,
                    inputBuffer,
                    ref inputPointer,
                    ref dataInBuffer,
                    window,
                    ref windowFrontPointer,
                    ref windowBackPointer);

                if (!nextByte.HasValue)
                    continue; // Sisääntulevan datan loppu

                if (distance == 0)
                {
                    // Vastaavuutta ei löytynyt ikkunasta
                    OutputLiteralData(
                        nextByte.Value,
                        outputBuffer,
                        ref outputBytePointer,
                        outputStream,
                        ref compressedBytes);

                    literals++;
                }
                else
                {
                    // Yksi tai useampi peräkkäinen vastaavuus löytyi ikkunasta
                    OutputReferencedData(
                        distance,
                        length,
                        outputBuffer,
                        ref outputBytePointer,
                        outputStream,
                        ref compressedBytes,
                        nextByte);

                    references++;
                }

                if (outputBytePointer >= outputBuffer.Length)
                {
                    compressedBytes += FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
                    outputBytePointer = 0;
                }

            } while (nextByte != null);

            // Kirjoita viimeinen keskeneräinen blokki
            if (outputBytePointer > 0)
            {
                compressedBytes += FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
            }

            outputStream.Flush();

            Console.WriteLine();
            Console.WriteLine($"Literal bytes encoded: {literals}");
            Console.WriteLine($"References encoded: {references}");

            return compressedBytes;
        }

        /// <summary>
        /// Pakkaa sisääntulevan tietovirran ulosmenevään tietovirtaan LZ77 algoritmilla käyttäen luokan
        /// ikkunankokoa.
        /// 
        /// Koodaus: jokainen literaali on koodattuna (0,[byte]) kolmena tavuna, jossa 0 on 16-bittinen arvo
        ///          jokainen takaisinpäinviittaus on koodattuna (d,l,[byte]) neljänä tavuna, jossa
        ///            d on etäisyys liukuvassa ikkunassa taaksepäin 16-bittisenä etumerkittömänä little-endian koodattuna lukuna,
        ///            l on toistettavien tavujen määrä liukuvassa ikkunasta alkaen kohdasta d 8-bittisenä lukuna, ja
        ///            [byte] on viittauksen jälkeinen seuraava literaali tavu
        ///            
        /// </summary>
        /// <param name="inputStream">Sisääntuleva tietovirta josta luetaan pakkaamaton syöte</param>
        /// <param name="outputStream">Ulosmenevä tietovirta johon data pakataan</param>
        /// <returns>Tuple (pakkaamaton datan koko, pakattu datan koko, koodausaika)</returns>
        public (ulong, ulong, TimeSpan) Encode(
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
            var window = new byte[_windowMaxSize];

            ulong compressedSize = Encode(window, inputStream, outputStream);

            var elapsed = timing.Elapsed;

            return (inputSize, compressedSize, elapsed);
        }

        /// <summary>
        /// Purkaa sisääntulevan tietovirran ulosmenevään tietovirtaan.
        /// 
        /// Käsittelee tietovirtoja 64kB blokkikoossa.
        /// </summary>
        /// <param name="inputStream">Sisääntuleva tietovirta</param>
        /// <param name="outputStream">Ulosmenevä tietovirta</param>
        private bool DecodeInternal(
            Stream inputStream,
            Stream outputStream)
        {
            ulong uncompressedBytes = 0;

            var inputBuffer = new byte[64 * 1024];
            var outputBuffer = new byte[64 * 1024];

            var window = new byte[_windowMaxSize];
            long windowFrontPointer = 0;
            long windowBackPointer = 0;

            var outputBytePointer = 0;

            ushort distance;

            bool modifiedInputBuffer;
            var inputBufferStart = 0;
            var readFromInput = inputBuffer.Length;

            while (inputStream.Position < inputStream.Length)
            {
                var read = inputStream.Read(inputBuffer, inputBufferStart, readFromInput) + inputBufferStart;
                var inputPointer = 0;
                modifiedInputBuffer = false;

                while (inputPointer < read)
                {
                    if ((read - inputPointer) < 3)
                    {
                        // Koodaus katkeaa kesken puskurin loppuun, siirretään loput tavut alkuun ja luetaan loppu puskuri täyteen
                        var remaining = read - inputPointer;

                        Buffer.BlockCopy(inputBuffer, inputPointer, inputBuffer, 0, remaining);
                        inputBufferStart = remaining;
                        readFromInput = inputBuffer.Length - remaining;
                        modifiedInputBuffer = true;

                        break;
                    }

                    distance = BitConverter.ToUInt16(inputBuffer, inputPointer);
                    inputPointer += 2;

                    byte symbol;

                    switch (distance)
                    {
                        case 0:
                            // Literaali
                            symbol = inputBuffer[inputPointer];
                            inputPointer++;

                            outputBuffer[outputBytePointer] = symbol;
                            outputBytePointer++;
                            uncompressedBytes++;

                            AddInputToWindow(window, symbol, ref windowFrontPointer, ref windowBackPointer);

                            break;
                        default:
                            // Viittauksen pituus + seuraava byte
                            if ((read - inputPointer) < 2)
                            {
                                // Koodaus katkeaa kesken puskurin loppuun, palautetaan tilanne, siirretään loput tavut alkuun ja luetaan loppu puskuri täyteen
                                var remaining = read - inputPointer + 2;

                                Buffer.BlockCopy(inputBuffer, inputPointer - 2, inputBuffer, 0, remaining);
                                inputBufferStart = remaining;
                                readFromInput = inputBuffer.Length - remaining;
                                modifiedInputBuffer = true;

                                break;
                            }

                            var length = inputBuffer[inputPointer];
                            inputPointer++;

                            var start = windowBackPointer - distance;

                            for (int p = 0; p < length; p++)
                            {
                                symbol = window[(start + p) % _windowMaxSize];

                                outputBuffer[outputBytePointer] = symbol;
                                outputBytePointer++;
                                uncompressedBytes++;

                                AddInputToWindow(window, symbol, ref windowFrontPointer, ref windowBackPointer);

                                // Tarkista loppuiko blokki
                                if (outputBytePointer >= outputBuffer.Length)
                                {
                                    _ = FlushOutputBlock(outputBuffer, outputBuffer.Length, outputStream);
                                    outputBytePointer = 0;
                                }
                            }

                            symbol = inputBuffer[inputPointer];
                            inputPointer++;

                            outputBuffer[outputBytePointer] = symbol;
                            outputBytePointer++;
                            uncompressedBytes++;

                            AddInputToWindow(window, symbol, ref windowFrontPointer, ref windowBackPointer);

                            break;
                    }

                    if (modifiedInputBuffer)
                        break;

                    // Tarkista loppuiko blokki
                    if (outputBytePointer >= outputBuffer.Length)
                    {
                        _ = FlushOutputBlock(outputBuffer, outputBuffer.Length, outputStream);
                        outputBytePointer = 0;
                    }
                }

                if (!modifiedInputBuffer)
                {
                    // Palauta täysi puskurin tila luettavaksi
                    inputBufferStart = 0;
                    readFromInput = inputBuffer.Length;
                }
            }

            // Kirjoita viimeinen keskeneräinen blokki
            if (outputBytePointer > 0)
            {
                _ = FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
            }

            outputStream.Flush();

            return true;
        }

        /// <summary>
        /// Purkaa sisääntulevan tietovirran ulosmenevään tietovirtaan.
        /// </summary>
        /// <param name="inputStream">Sisääntuleva tietovirta josta luetaan pakattu syöte</param>
        /// <param name="outputStream">Ulosmenevä tietovirta johon data puretaan</param>
        /// <returns>Dekoodausaika tai null mikäli tiedoston luku epäonnistui</returns>
        public TimeSpan? Decode(
            Stream inputStream,
            Stream outputStream)
        {
            // Aseta sisääntulon ja ulostulon oletetut aloituskohdat ja koot
            inputStream.Position = 0;
            outputStream.SetLength(0);

            // Aloita dekoodaus
            var timing = Stopwatch.StartNew();

            if (!DecodeInternal(inputStream, outputStream))
            {
                // Ei kyetty purkamaan dataosuutta, virheellinen tiedostosisältö?
                return null;
            }

            var elapsed = timing.Elapsed;

            return elapsed;
        }
    }

}