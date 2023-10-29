using System.Diagnostics;

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
        private const int DefaultWindowSize = 4 * 1024;

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
        private readonly ushort[] _bitExponents = new ushort[]
        {
            (ushort)Math.Pow(2, 0),
            (ushort)Math.Pow(2, 1),
            (ushort)Math.Pow(2, 2),
            (ushort)Math.Pow(2, 3),
            (ushort)Math.Pow(2, 4),
            (ushort)Math.Pow(2, 5),
            (ushort)Math.Pow(2, 6),
            (ushort)Math.Pow(2, 7),
            (ushort)Math.Pow(2, 8),
            (ushort)Math.Pow(2, 9),
            (ushort)Math.Pow(2, 10),
            (ushort)Math.Pow(2, 11),
            (ushort)Math.Pow(2, 12),
            (ushort)Math.Pow(2, 13),
            (ushort)Math.Pow(2, 14),
            (ushort)Math.Pow(2, 15),
        };

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
            _windowMaxSize = Math.Max(0, Math.Min(windowSize ?? DefaultWindowSize, DefaultWindowSize));

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

            // Puskuri ei voi olla pienempi kuin pisimmän koodauksen viemä tila kokonaisuna tavuina + 1 tavu
            var minimumBufferSize = ((_bitsForDistance + _bitsForLength + 8) / 8) + 2;

            _inputBufferSize = Math.Max(minimumBufferSize, inputBufferSize ?? 128 * 1024);
            _outputBufferSize = Math.Max(minimumBufferSize, outputBufferSize ?? 128 * 1024);
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
        /// käyttää vain aiemmin luettua sisääntulevan tietovirran puskuria syötteenä, jotta vastaavuuksien etsinnässä ei tarvitse
        /// palata takaisin sisääntulevassa virrassa.
        /// 
        /// </summary>
        /// <param name="inputBlock">Käsiteltävä sisääntulevan tietovirran puskuri</param>
        /// <param name="inputPointer">Sisääntulevan blokin seuraavan tavun osoitin</param>
        /// <param name="dataInBlock">Sisääntulevan blokin sisältämä tietomäärä tavuina</param>
        /// <param name="window">Pakkausikkuna josta vastaavuksia skannataan</param>
        /// <param name="temporaryWindow">Tilapäinen ikkunapuskuri</param>
        /// <param name="windowFrontPointer">Ikkunan looginen alkukohta</param>
        /// <param name="windowBackPointer">Ikkunan looginen loppukohta</param>
        /// <returns>Tuple (etäisyys, vastaavuuden pituus ja seuraava ei vastaava tavu)</returns>
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
        /// Kirjoittaa N koodibittiä uloskirjoitettavaan blokkiin.
        /// 
        /// Kirjoitettavat bitit ovat alimmat bitit Little-endian tulkintaisesti.
        /// </summary>
        /// <param name="value">Tavu joka kirjoitetaan</param>
        /// <param name="bits">Kirjoitettavien bittien lukumäärä tavussa</param>
        /// <param name="outputBlock">Uloskirjoitusblokki</param>
        /// <param name="outputBytePointer">Uloskirjoitusblokin tavuosoitin mitä tavua kirjoitetaan</param>
        /// <param name="outputBitPointer">Uloskirjoitusblokin bittiosoitin mitä bittiä tavusta kirjoitetaan</param>
        /// <param name="outputStream">Ulosmenevä tietovirta johon data pakataan</param>
        /// <param name="compressedBytes">Pakattujen tavujen määrä</param>
        public void Output8Bits(
            byte value,
            int bits,
            byte[] outputBlock,
            ref int outputBytePointer,
            ref int outputBitPointer,
            Stream outputStream,
            ref ulong compressedBytes)
        {
            // Montako bittiä vielä mahtuu nykyiseen uloskirjoitettavaan tavuun?
            var remaining = 8 - outputBitPointer;

            if (remaining < bits)
            {
                // Koodibitit jakautuu kahden tavun välille
                var removeLowerBits = bits - remaining;

                // Ensimmäinen yhdistäminen vasemmanpuoleisimmille biteille
                byte b = value;
                b >>= removeLowerBits;
                outputBlock[outputBytePointer] |= b;

                outputBytePointer++;

                // Tarkista loppuiko blokki
                if (outputBytePointer >= outputBlock.Length)
                {
                    compressedBytes += FlushOutputBlock(outputBlock, outputBytePointer, outputStream);
                    outputBytePointer = 0;
                }

                // Nollaa seuraava kirjoitettava tavu puskurissa 
                outputBlock[outputBytePointer] = 0;

                // Toinen yhdistäminen lopuille biteille normaalissa polussa
                bits -= remaining;

                // Poista koodista yläbitit jotka jo kirjoitettiin
                value <<= 8 - removeLowerBits;
                value >>= 8 - removeLowerBits;

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
                value <<= shiftLeft;

            outputBlock[outputBytePointer] |= value;
            outputBitPointer += bits;

            if (outputBitPointer == 8)
            {
                outputBitPointer = 0;
                outputBytePointer++;

                if (outputBytePointer >= outputBlock.Length)
                {
                    compressedBytes += FlushOutputBlock(outputBlock, outputBytePointer, outputStream);
                    outputBytePointer = 0;
                }
            }

            if (outputBitPointer == 0 &&
                outputBytePointer < outputBlock.Length)
            {
                // Nollaa seuraava kirjoitettava tavu puskurissa
                outputBlock[outputBytePointer] = 0;
            }
        }

        /// <summary>
        /// Kirjoittaa N koodibittiä uloskirjoitettavaan blokkiin
        /// 
        /// Kirjoitettavat bitit ovat alimmat bitit Little-endian tulkintaisesti.
        /// </summary>
        /// <param name="value">16-bittinen arvo joka kirjoitetaan</param>
        /// <param name="bits">Kirjoitettavien bittien lukumäärä tavussa</param>
        /// <param name="outputBlock">Uloskirjoitusblokki</param>
        /// <param name="outputBytePointer">Uloskirjoitusblokin tavuosoitin mitä tavua kirjoitetaan</param>
        /// <param name="outputBitPointer">Uloskirjoitusblokin bittiosoitin mitä bittiä tavusta kirjoitetaan</param>
        /// <param name="outputStream">Ulosmenevä tietovirta johon data pakataan</param>
        /// <param name="compressedBytes">Pakattujen tavujen määrä</param>
        public void OutputBits(
            ushort value,
            int bits,
            byte[] outputBlock,
            ref int outputBytePointer,
            ref int outputBitPointer,
            Stream outputStream,
            ref ulong compressedBytes)
        {
            byte encodeByte;

            // Yli kahdeksan bitin bittijono, pilkotaan tavupalasiin ylimmistä biteistä alkaen
            var bitsToOutput = bits - (bits / 8 * 8);

            for (var shiftRounds = bits / 8; shiftRounds > 0; shiftRounds--)
            {
                var temporaryCode = value;
                temporaryCode >>= shiftRounds * 8;

                encodeByte = (byte)(temporaryCode & 0xFF);

                Output8Bits(
                    encodeByte,
                    bitsToOutput,
                    outputBlock,
                    ref outputBytePointer,
                    ref outputBitPointer,
                    outputStream,
                    ref compressedBytes);

                bits -= bitsToOutput;
                bitsToOutput = 8;
            }

            // Lisää alin tavu
            Output8Bits(
                (byte)(value & 0xFF),
                bits,
                outputBlock,
                ref outputBytePointer,
                ref outputBitPointer,
                outputStream,
                ref compressedBytes);
        }

        /// <summary>
        /// Kirjoita ulosmenevään puskuriin yksittäinen sisääntuleva tavu sellaisenaan
        /// </summary>
        /// <param name="outputByte">Tavu joka kirjoitetaan</param>
        /// <param name="outputBuffer">Ulosmenevän pakkauksen puskuri</param>
        /// <param name="outputBytePointer">Ulosmenevän pakkauksen puskurin tavuosoitin</param>
        /// <param name="outputBitPointer">Ulosmenevän pakkauksen puskurin bittiosoitin</param>
        /// <param name="outputStream">Ulosmenevä tietovirta johon data pakataan</param>
        /// <param name="compressedBytes">Pakattujen tavujen määrä</param>
        public void OutputLiteralData(
            byte outputByte,
            byte[] outputBuffer,
            ref int outputBytePointer,
            ref int outputBitPointer,
            Stream outputStream,
            ref ulong compressedBytes)
        {
            // Kirjoita pituus
            Output8Bits(
                byte.MinValue,
                _bitsForLength,
                outputBuffer,
                ref outputBytePointer,
                ref outputBitPointer,
                outputStream,
                ref compressedBytes);

            // Kirjoita literaali tavu
            Output8Bits(
                outputByte,
                8,
                outputBuffer,
                ref outputBytePointer,
                ref outputBitPointer,
                outputStream,
                ref compressedBytes);
        }

        /// <summary>
        /// Kirjoita ulosmenevään puskuriin takaisinpäin viittaus ikkunasta löytyneeseen tavujonoon 
        /// sekä sitä seuraava sisääntuleva tavu sellaisenaan
        /// </summary>
        /// <param name="distance">Etäisyys taaksepäin ikkunassa vastaavuuden alkuun</param>
        /// <param name="length">Tavujen määrä ikkunassa jotka vastaavat sisääntulevaa tavujonoa</param>
        /// <param name="outputBuffer">Ulosmenevän pakkauksen puskuri</param>
        /// <param name="outputBytePointer">Ulosmenevän pakkauksen puskurin tavuosoitin</param>
        /// <param name="outputBitPointer">Ulosmenevän pakkauksen puskurin bittiosoitin</param>
        /// <param name="nextByte">Tavu joka kirjoitetaan viittauksen jälkeen</param>
        /// <param name="outputStream">Ulosmenevä tietovirta johon data pakataan</param>
        /// <param name="compressedBytes">Pakattujen tavujen määrä</param>
        public void OutputReferencedData(
            ushort distance,
            byte length,
            byte[] outputBuffer,
            ref int outputBytePointer,
            ref int outputBitPointer,
            byte nextByte,
            Stream outputStream,
            ref ulong compressedBytes)
        {
            // Kirjoita pituus
            Output8Bits(
                length,
                _bitsForLength,
                outputBuffer,
                ref outputBytePointer,
                ref outputBitPointer,
                outputStream,
                ref compressedBytes);

            // Kirjoita etäisyys
            OutputBits(
                distance,
                _bitsForDistance,
                outputBuffer,
                ref outputBytePointer,
                ref outputBitPointer,
                outputStream,
                ref compressedBytes);

            // Kirjoita seuraava tavu
            Output8Bits(
                nextByte,
                8,
                outputBuffer,
                ref outputBytePointer,
                ref outputBitPointer,
                outputStream,
                ref compressedBytes);
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
            var inputBuffer = new byte[_inputBufferSize];
            var outputBuffer = new byte[_outputBufferSize];

            int inputPointer;
            var outputBytePointer = 0;
            var outputBitPointer = 0;
            int dataInBuffer;

            long windowFrontPointer = 0;
            long windowBackPointer = 0;

            var temporaryWindow = new byte[_windowMaxSize];

            ulong literals = 0;
            ulong references = 0;

            byte? nextByte;

            // Kirjoitetaan alkuun pakkaamaton tiedostokoko
            ulong inputSize = (ulong)inputStream.Length;
            outputStream.Write(BitConverter.GetBytes(inputSize), 0, 8);
            ulong compressedBytes = 0;

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
                            ref outputBitPointer,
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
                            ref outputBitPointer,
                            nextByte.Value,
                            outputStream,
                            ref compressedBytes);

                        references++;
                    }
                }
            } while (dataInBuffer > 0);

            // Kirjoita viimeinen keskeneräinen blokki
            if (outputBytePointer > 0 || outputBitPointer > 0)
            {
                if (outputBitPointer > 0)
                    outputBytePointer++;

                compressedBytes += FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
            }

            outputStream.Flush();

            return (compressedBytes, literals, references);
        }

        /// <summary>
        /// Pakkaa sisääntulevan tietovirran ulosmenevään tietovirtaan LZ77 algoritmilla käyttäen luokan
        /// ikkunankokoa.
        /// 
        /// Tiedostorakenne:
        /// -- Pakkaamattoman datan koko tavuina (64-bittinen etumerkitön kokonaisluku) --
        /// -- LZ77 PAKATTU DATA --
        /// 
        /// LZ77 koodaus: - jokainen literaali on koodattuna (l,[byte]), jossa l on vastaavuuden pituuden maksimibittipituus (_bitsForLength, 8 bittiä),
        ///                 ja saa literaalien kohdalla arvo nolla, ja jonka perässä kahdeksalla bitillä literaalitavu.
        ///               - jokainen takaisinpäinviittaus on koodattuna (l, d, [byte]), jossa l on vastaavuuden vastaavuuden pituuden maksimibittipituus,
        ///                 d on löydetyn vastaavuuden etäisyyden maksimibittipituus (_bitsForDistance, 13 bittiä oletusikkunakoolla), ja
        ///                 jonka perässä kahdeksalla bitillä viittauksen jälkeinen seuraava literaalitavu.
        ///                 
        ///                 l ja d ovat esitystavaltaan etumerkittömiä little-endian järjestyksessä olevia kokonaislukuja.
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
        /// Purkaa yksittäisen sisäänluetun puskurin
        /// </summary>
        /// <returns>Tavuja nykyisen sisäänluetun puskurin lopusta jotka pitää säilyttää seuraavassa puskurissa alussa</returns>
        private int DecodeBuffer(
            bool isLastBuffer,
            byte[] window,
            ref long windowFrontPointer,
            ref long windowBackPointer,
            ulong expectedDataSize,
            byte[] inputBuffer,
            int inputBufferSize,
            ref int inputBitPointer,
            byte[] outputBuffer,
            Stream outputStream,
            ref int outputBytePointer,
            ref ulong uncompressedBytes)
        {
            int inputBytePointer = 0;

            // Jos uuden kierroksen alussa ei ole riittävästi tavuja jäljellä pisimpään mahdolliseen esitykseen,
            // lopeta käsittely ja siirrä jäljelläolevat tavut seuraavaan käsiteltävään puskuriin
            int cutoffPoint = !isLastBuffer
                ? inputBufferSize - ((_bitsForDistance + _bitsForLength + 8) / 8) - 1
                : inputBufferSize;

            int lengthHighBit = _bitsForLength - 1;
            byte length;
            int distanceHighBit = _bitsForDistance - 1;
            ushort distance;
            int exp;
            byte literalByte;

            while (inputBytePointer < inputBufferSize)
            {
                if (uncompressedBytes == expectedDataSize)
                    return 0;

                if (inputBytePointer >= cutoffPoint)
                {
                    return inputBufferSize - inputBytePointer;
                }

                if (outputBytePointer >= outputBuffer.Length)
                {
                    _ = FlushOutputBlock(outputBuffer, outputBytePointer, outputStream);
                    outputBytePointer = 0;
                }

                // Luetaan pituus
                length = 0;
                for (int i = lengthHighBit; i >= 0; i--)
                {
                    if (inputBitPointer >= 8)
                    {
                        inputBitPointer = 0;
                        inputBytePointer++;
                    }

                    exp = _bitExponents[lengthHighBit - inputBitPointer];
                    if ((inputBuffer[inputBytePointer] & exp) == exp)
                        length |= (byte)_bitExponents[i];

                    inputBitPointer++;
                }

                if (length == 0)
                {
                    // Literaalitavu, luetaan seuraavat kahdeksan bittiä
                    literalByte = 0;
                    for (int i = 7; i >= 0; i--)
                    {
                        if (inputBitPointer >= 8)
                        {
                            inputBitPointer = 0;
                            inputBytePointer++;
                        }

                        exp = _bitExponents[7 - inputBitPointer];
                        if ((inputBuffer[inputBytePointer] & exp) == exp)
                            literalByte |= (byte)_bitExponents[i];

                        inputBitPointer++;
                    }

                    outputBuffer[outputBytePointer] = literalByte;
                    outputBytePointer++;

                    AddInputToWindow(
                        window,
                        _windowMaxSize,
                        literalByte,
                        ref windowFrontPointer,
                        ref windowBackPointer);

                    uncompressedBytes++;

                    continue;
                }

                // Takaisinpäinviittaus, luetaan pituus
                distance = 0;

                for (int i = distanceHighBit; i >= 0; i--)
                {
                    if (inputBitPointer >= 8)
                    {
                        inputBitPointer = 0;
                        inputBytePointer++;
                    }

                    exp = _bitExponents[7 - inputBitPointer];
                    if ((inputBuffer[inputBytePointer] & exp) == exp)
                        distance |= _bitExponents[i];

                    inputBitPointer++;
                }

                // Kopioidaan ikkunasta
                var start = windowBackPointer - distance;

                for (int p = 0; p < length; p++)
                {
                    literalByte = window[(start + p) % _windowMaxSize];

                    outputBuffer[outputBytePointer] = literalByte;
                    outputBytePointer++;

                    AddInputToWindow(
                        window,
                        _windowMaxSize,
                        literalByte,
                        ref windowFrontPointer,
                        ref windowBackPointer);

                    uncompressedBytes++;

                    // Tarkista loppuiko blokki
                    if (outputBytePointer >= outputBuffer.Length)
                    {
                        _ = FlushOutputBlock(outputBuffer, outputBuffer.Length, outputStream);
                        outputBytePointer = 0;
                    }
                }

                // Lopuksi literaalitavu, luetaan seuraavat kahdeksan bittiä
                literalByte = 0;
                for (int i = 7; i >= 0; i--)
                {
                    if (inputBitPointer >= 8)
                    {
                        inputBitPointer = 0;
                        inputBytePointer++;
                    }

                    exp = _bitExponents[7 - inputBitPointer];
                    if ((inputBuffer[inputBytePointer] & exp) == exp)
                        literalByte |= (byte)_bitExponents[i];

                    inputBitPointer++;
                }

                outputBuffer[outputBytePointer] = literalByte;
                outputBytePointer++;

                AddInputToWindow(
                    window,
                    _windowMaxSize,
                    literalByte,
                    ref windowFrontPointer,
                    ref windowBackPointer);

                uncompressedBytes++;
            }

            return 0;
        }


        /// <summary>
        /// Purkaa sisääntulevan tietovirran ulosmenevään tietovirtaan.
        /// 
        /// Käsittelee tietovirtoja luokan määritellyssä puskurikoossa.
        /// </summary>
        /// <param name="expectedDataSize">Odotettu pakkaamattoman datan koko</param>
        /// <param name="window">Pakkausikkuna</param>
        /// <param name="inputStream">Sisääntuleva tietovirta</param>
        /// <param name="outputStream">Ulosmenevä tietovirta</param>
        public bool Decode(
            ulong expectedDataSize,
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

            bool modifiedInputBuffer = false;
            var inputBufferStart = 0;
            var readFromInput = inputBuffer.Length;

            var inputBitPointer = 0;

            while (inputStream.Position < inputStream.Length || modifiedInputBuffer)
            {
                var read = inputStream.Read(inputBuffer, inputBufferStart, readFromInput);

                var remainder = DecodeBuffer(
                    read == 0,
                    window,
                    ref windowFrontPointer,
                    ref windowBackPointer,
                    expectedDataSize,
                    inputBuffer,
                    inputBufferStart + read,
                    ref inputBitPointer,
                    outputBuffer,
                    outputStream,
                    ref outputBytePointer,
                    ref uncompressedBytes);

                if (remainder > 0)
                {
                    // Koodaus katkeaa mahdollisesti kesken puskurin lopun, 
                    // siirretään loput tavut alkuun ja luetaan loppu puskuri täyteen
                    Buffer.BlockCopy(inputBuffer, inputBufferStart + read - remainder, inputBuffer, 0, remainder);
                    inputBufferStart = remainder;
                    readFromInput = inputBuffer.Length - remainder;
                    modifiedInputBuffer = true;
                }
                else
                {
                    readFromInput = inputBuffer.Length;
                    modifiedInputBuffer = false;
                    inputBufferStart = 0;
                }

                if (uncompressedBytes == expectedDataSize)
                {
                    // Kaikki purettu
                    break;
                }
            }

            // Kirjoita viimeinen keskeneräinen puskuri
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

            byte[] uLongBuffer = new byte[8];

            if (inputStream.Read(uLongBuffer, 0, uLongBuffer.Length) != uLongBuffer.Length)
            {
                // Ei kyetty lukemaan edes oletettua kokoa, virheellinen tiedostosisältö?
                return null;
            }

            var expectedDataSize = BitConverter.ToUInt64(uLongBuffer);

            // Uusi ikkuna dekoodaukseen
            var window = new byte[_windowMaxSize];

            // Aloita dekoodaus
            var timing = Stopwatch.StartNew();

            if (!Decode(expectedDataSize, window, inputStream, outputStream))
            {
                // Ei kyetty purkamaan dataosuutta, virheellinen tiedostosisältö?
                return null;
            }

            var elapsed = timing.Elapsed;

            return elapsed;
        }
    }

}