using System.Diagnostics;
using Microsoft.VisualBasic;

namespace Tiracompress.Algorithms
{
    /// <summary>
    /// Tämä luokka toteuttaa LZ77 -algoritmiin perustuvan tiedon koodauksen/pakkauksen sekä purkamisen
    /// </summary>
    public class Lz77
    {
        /// <summary>
        /// Pakkausikkunan oletus-koko (4 kilotavua)
        /// </summary>
        private const int DefaultBufferSize = 4 * 1024;

        /// <summary>
        /// Minimipituus vastaavuutta mikä kelpuutetaan pakkausikkunasta
        /// </summary>
        private const int MinMatchLength = 3;

        /// <summary>
        /// Minimipituus vastaavuutta mikä kelpuutetaan pakkausikkunasta ilman että
        /// yritetään jatkaa vielä pidemmän vastaavuuden etsimistä
        /// </summary>
        private const int MinAcceptableLength = 5;

        /// <summary>
        /// Maksimimäärä vastaavuuksien etsimisiä pakkausikkunasta kun on löytynyt jo
        /// minimipituuden täyttäviä vastaavuuksia.
        /// </summary>
        private const int MaxMatchRetries = 5;

        /// <summary>
        /// Maksimipituus mikä kelpuutetaan pakkausikkunasta
        /// </summary>
        private const int MaxMatchLength = 255;

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
        private readonly int _bitsForDistance;
        private readonly int _bitsForLength;
        private readonly int _inputBufferSize;
        private readonly int _outputBufferSize;

        /// <summary>
        /// Luo uuden LZ77 algoritmin, optionaalisesti oletuksesta poikkeavalla ikkunan koolla ja/tai puskurien koolla.
        /// </summary>
        /// <param name="windowSize">Ikkunan koko tavuina (käytetään oletuskokoa mikäli ei asetettu, maksimi 64 kilotavua)</param>
        /// <param name="inputBufferSize">Käytettävä sisääntulevan tietovirran puskurin koko (oletus: 128 kilotavua)</param>
        /// <param name="outputBufferSize">Käytettävä ulosmenevän tietovirran puskurin koko (oletus: 128 kilotavua)</param>
        public Lz77(
            int? windowSize = null,
            int? inputBufferSize = null,
            int? outputBufferSize = null)
        {
            _windowMaxSize = Math.Max(0, Math.Min(windowSize ?? DefaultBufferSize, DefaultBufferSize));

            // Lasketaan montako bittiä tarvitaan viittauksen (maksimi)etäisyyden esittämiseksi bittipakatussa muodossa,
            // pohjautuen ikkunan kokoon
            var left = _windowMaxSize;

            while (left > 0)
            {
                left /= 2;
                _bitsForDistance++;
            }

            // Lasketaan montako bittiä tarvitaan viittauksen (maksimi)pituuden esittämiseksi bittipakatussa muodossa
            left = MaxMatchLength;

            while (left > 0)
            {
                left /= 2;
                _bitsForLength++;
            }

            // Puskurien koko vaikuttaa levy-I/O:n määrään (l. I/O:n tehokkuuteen) koska kaikki luku ja kirjoitus
            // tietovirtoihin tehdään puskurien kautta
            _inputBufferSize = inputBufferSize ?? 128 * 1024;
            _outputBufferSize = outputBufferSize ?? 128 * 1024;
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
        /// <param name="windowSize">Ikkunapuskurin (maksimi)koko</param>
        /// <param name="input">Tavu joka lisätään</param>
        /// <param name="windowFrontPointer">Ikkunan looginen alkukohta</param>
        /// <param name="windowBackPointer">Ikkunan looginen loppukohta</param>
        public static void AddInputToWindow(
            byte[] window,
            int windowSize,
            byte input,
            ref long windowFrontPointer,
            ref long windowBackPointer)
        {
            window[windowBackPointer % windowSize] = input;

            // Siirrä loogisia ikkunan aloitus ja lopetusrajoja
            if ((int)(windowBackPointer - windowFrontPointer) < windowSize)
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

        private bool ScanWindowForward(
            long start,
            byte[] inputBlock,
            int inputPointer,
            int dataInBlock,
            byte[] window,
            byte[] temporaryWindow,
            long windowFrontPointer,
            long windowBackPointer,
            out ushort matchLength,
            out byte? nextUnmatchedByte)
        {
            nextUnmatchedByte = null;
            matchLength = 1;
            var windowToScan = window;
            var windowsSwapped = false;

            var originalInputStart = inputPointer - 1;

            for (var j = start; j < windowBackPointer; j++)
            {
                if (inputPointer >= dataInBlock)
                {
                    // Sisäänluettava syöte loppui
                    if (matchLength > 1)
                    {
                        // Vähennä yksi jotta viimeinen tavu palautetaan omanaan
                        matchLength--;
                    }

                    break;
                }

                nextUnmatchedByte = inputBlock[inputPointer];

                if (windowsSwapped)
                {
                    // Lisätään syötettä ikkunaan
                    AddInputToWindow(
                        windowToScan,
                        _windowMaxSize,
                        nextUnmatchedByte.Value,
                        ref windowFrontPointer,
                        ref windowBackPointer);
                }

                if (windowToScan[j % _windowMaxSize] != nextUnmatchedByte.Value)
                {
                    // Seuraava sisääntuleva tavu ei vastaa ikkunan seuraavaa tavua
                    break;
                }

                if (j == windowBackPointer - 1 && !windowsSwapped)
                {
                    // Alunperäisen ikkunan takaraja saavutettu, vaihda tilapäiseen ikkunaan ja
                    // lisää kaikki tähänastiset luetut tavut sinne jotta voimme jatkaa skannaamista.
                    // Tarkoituksena on säilyttää alkuperäinen ikkuna koskemattomana jotta voimme skannata
                    // useammasta kohdasta vastaavuuksia etsiessä.

                    Buffer.BlockCopy(window, 0, temporaryWindow, 0, _windowMaxSize);
                    windowToScan = temporaryWindow;

                    windowsSwapped = true;

                    for (var i = originalInputStart; i <= inputPointer; i++)
                    {
                        AddInputToWindow(
                            windowToScan,
                            _windowMaxSize,
                            inputBlock[i],
                            ref windowFrontPointer,
                            ref windowBackPointer);
                    }
                }

                if (matchLength == MaxMatchLength)
                {
                    break;
                }

                inputPointer++;
                matchLength++;
            }

            return matchLength >= MinAcceptableLength;
        }

        /// <summary>
        /// Etsii jo nähdyn sisääntulevan tietovirran ikkunasta ensimmäisen vastaavuuden seuraaviin
        /// sisääntuleviin tavuihin skannaamalla ikkunaa takaperin.
        /// 
        /// Algoritmin yksinkertaistamiseksi vastaavuuksien etsiminen ei lue sisääntulevaa tietovirtaa vaan
        /// käyttää vain aiemmin luettua blokkia syötteenä, jotta vastaavuuksien etsinnässä ei tarvitse
        /// palata takaisin sisääntulevassa virrassa.
        /// 
        /// </summary>
        /// <param name="inputBlock">Käsiteltävä sisääntuleva blokki</param>
        /// <param name="inputPointer">Sisääntulevan blokin seuraavan tavun osoitin</param>
        /// <param name="dataInBlock">Sisääntulevan blokin sisältämä tietomäärä tavuina</param>
        /// <param name="window">Ikkuna josta vastaavuksia skannataan</param>
        /// <param name="temporaryWindow">Tilapäinen ikkuna josta vastaavuksia skannataan</param>
        /// <param name="windowFrontPointer">Ikkunan looginen alkukohta</param>
        /// <param name="windowBackPointer">Ikkunan looginen loppukohta</param>
        /// <returns></returns>
        public (ushort, byte, byte?) ScanWindowForMatch(
            byte[] inputBlock,
            ref int inputPointer,
            int dataInBlock,
            ref byte[] window,
            ref byte[] temporaryWindow,
            ref long windowFrontPointer,
            ref long windowBackPointer)
        {
            if (inputPointer >= dataInBlock)
            {
                return (0, 0, null);
            }

            var firstByte = inputBlock[inputPointer];
            inputPointer++;

            ushort distanceActual = 0;
            ushort distance = 0;
            ushort length = 0;

            var tries = 0;
            var nextByteAfterBestMatch = default(byte?);

            var windowsSwapped = false;

            // Skannataan taaksepäin ikkunaa
            for (var i = windowBackPointer - 1; i >= windowFrontPointer; i--)
            {
                distanceActual++;

                if (window[i % _windowMaxSize] != firstByte)
                {
                    // Tavujonon alkukohtaa ei vielä löytynyt, jatka taaksepäin.
                    continue;
                }

                // Skannataan eteenpäin kuinka monta lisätavua löytyy vastaavuutta
                // Huom: koska seuraavat sisäänluetut tavut lisätään sitä mukaan osaksi ikkunaa, voi pituusviittaus
                // viitata myös uusiin tavuihin - tämä on LZ77:ssa täysin laillista. 

                if (ScanWindowForward(
                        i + 1,
                        inputBlock,
                        inputPointer,
                        dataInBlock,
                        window,
                        temporaryWindow,
                        windowFrontPointer,
                        windowBackPointer,
                        out var matchLength,
                        out var nextUnmatchedByte))
                {
                    // Riittävä vastaavuus löytyi
                    distance = distanceActual;
                    length = matchLength;
                    nextByteAfterBestMatch = nextUnmatchedByte;
                    break;
                }

                if (matchLength >= MinMatchLength)
                {
                    tries++;

                    if (matchLength > length)
                    {
                        // Merkitään tämä toistaiseksi parhaimmaksi ja otetaan kohta talteen
                        distance = distanceActual;
                        length = matchLength;
                        nextByteAfterBestMatch = nextUnmatchedByte;
                    }
                }

                if (tries >= MaxMatchRetries)
                {
                    // Käytetään sitä parasta tulosta mikä tähän asti on saatu
                    break;
                }
            }

            if (distance > 0)
            {
                // Syötetään ikkunaan kaikki löydetyt tavut + viimeinen
                // ei vastaava tavu, joka palautuu omanaan.
                for (var i = 0; i < length + 1; i++)
                {
                    AddInputToWindow(
                        window,
                        _windowMaxSize,
                        inputBlock[inputPointer - 1],
                        ref windowFrontPointer,
                        ref windowBackPointer);
                    inputPointer += 1;
                }

                inputPointer -= 1;

                // Palauta takaisinpäinviittaus + seuraava tavu
                return (distance, (byte)length, nextByteAfterBestMatch);
            }

            // Lisää ainoa luettu tavu ikkunaan
            AddInputToWindow(
                window,
                _windowMaxSize,
                firstByte,
                ref windowFrontPointer,
                ref windowBackPointer);

            // Palauta tavu omanaan
            return (0, 0, firstByte);
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
            if (outputBytePointer > _outputBufferSize - 3)
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
            byte nextChar)
        {
            if (outputBytePointer > _outputBufferSize - 3)
            {
                compressedBytes += FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
                outputBytePointer = 0;
            }

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
            outputBuffer[outputBytePointer] = nextChar;
            outputBytePointer++;
        }

        /// <summary>
        /// Pakkaa sisääntulevan tietovirran ulosmenevään tietovirtaan käyttäen määriteltyä
        /// LZ77 ikkunaa.
        /// 
        /// Käsittelee ja pakkaa tietovirtaa sisäänluettavien puskurien rajojen sisällä.
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

            var inputBuffer = new byte[_inputBufferSize];
            var outputBuffer = new byte[_outputBufferSize];

            var inputPointer = -1;
            var outputBytePointer = 0;
            var outputBitPointer = 0;
            var dataInBuffer = 0;

            long windowFrontPointer = 0;
            long windowBackPointer = 0;

            var temporaryWindow = new byte[_windowMaxSize];

            ulong literals = 0;
            ulong references = 0;

            byte? nextByte;

            do
            {
                // Luetaan ja prosessoidaan puskuri kerrallaan
                try
                {
                    dataInBuffer = inputStream.Read(inputBuffer, 0, inputBuffer.Length);
                }
                catch
                {
                    // Lukeminen ei onnistunut!
                    return (0, 0, 0);
                }

                if (dataInBuffer <= 0)
                {
                    // Sisääntuleva tietovirta on loppu
                    break;
                }

                // Etsitään ikkunasta vastaavuutta käsiteltäville tavuille LZ77 algoritmilla
                // kunnes kaikki sisääntulevan puskurin data on luettu.

                inputPointer = 0;

                while (inputPointer < dataInBuffer)
                {
                    (ushort distance, byte length, nextByte) = ScanWindowForMatch(
                        inputBuffer,
                        ref inputPointer,
                        dataInBuffer,
                        ref window,
                        ref temporaryWindow,
                        ref windowFrontPointer,
                        ref windowBackPointer);

                    if (!nextByte.HasValue)
                    {
                        // Virhetilanne
                        return (0, 0, 0);
                    }

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
                        // Vastaavuus löytyi ikkunasta
                        OutputReferencedData(
                            distance,
                            length,
                            outputBuffer,
                            ref outputBytePointer,
                            outputStream,
                            ref compressedBytes,
                            nextByte.Value);

                        references++;
                    }
                }
            } while (dataInBuffer > 0);

            /*
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

                

                if (outputBytePointer >= outputBuffer.Length)
                {
                    compressedBytes += FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
                    outputBytePointer = 0;
                }

            } while (nextByte != null);
            */

            // Kirjoita viimeinen keskeneräinen blokki
            if (outputBytePointer > 0 || outputBitPointer > 0)
            {
                compressedBytes += FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
            }

            outputStream.Flush();

            return (compressedBytes, literals, references);
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
            var window = new byte[_windowMaxSize];

            (ulong compressedSize, ulong literals, ulong references) = Encode(window, inputStream, outputStream);

            var elapsed = timing.Elapsed;

            return (inputSize, compressedSize, literals, references, elapsed);
        }

        /// <summary>
        /// Purkaa sisääntulevan tietovirran ulosmenevään tietovirtaan.
        /// 
        /// Käsittelee tietovirtoja luokan määritellyssä puskurikoossa.
        /// </summary>
        /// <param name="window">Pakkausikkuna</param>
        /// <param name="inputStream">Sisääntuleva tietovirta</param>
        /// <param name="outputStream">Ulosmenevä tietovirta</param>
        public bool Decode(
            byte[] window,
            Stream inputStream,
            Stream outputStream)
        {
            ulong uncompressedBytes = 0;

            var inputBuffer = new byte[_inputBufferSize];
            var outputBuffer = new byte[_outputBufferSize];

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

                            AddInputToWindow(
                                window,
                                _windowMaxSize,
                                symbol,
                                ref windowFrontPointer,
                                ref windowBackPointer);

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

                                AddInputToWindow(
                                    window,
                                    _windowMaxSize,
                                    symbol,
                                    ref windowFrontPointer,
                                    ref windowBackPointer);

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

                            AddInputToWindow(
                                window,
                                _windowMaxSize,
                                symbol,
                                ref windowFrontPointer,
                                ref windowBackPointer);

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

            // Uusi ikkuna dekoodaukseen
            var window = new byte[_windowMaxSize];

            // Aloita dekoodaus
            var timing = Stopwatch.StartNew();

            if (!Decode(window, inputStream, outputStream))
            {
                // Ei kyetty purkamaan dataosuutta, virheellinen tiedostosisältö?
                return null;
            }

            var elapsed = timing.Elapsed;

            return elapsed;
        }
    }

}