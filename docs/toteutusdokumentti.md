# Toteutusdokumentti

## Yleisrakenne

Ohjelmisto on yleistasolla toteutettu kolmena erillisenä .NET projektina:

### algorithms.csproj

Hakemistossa */src/algorithms/*.

Itse pakkaus- ja purkualgoritmit toteuttava projekti, jossa jokainen kokonaan toteutettu algoritmi (Huffman ja LZ77) ovat omina erillisinä .cs kooditiedostona/luokkana. 

Deflate -algoritmi, jota ei kurssin ajan puitteissa ehditty saattaa valmiiksi toimivaksi toteutukseksi, on myös omana luokkana mutta keskeneräisenä.

Vaikka normaalitilanteessa luokan enkapsulointiin kuuluisi käyttää public näkyvyyttä vain niiden metodien osalta joita normaalitilanteessa tarvitaan kutsua luokan ulkoa, testausprojektin johdosta käytännössä kaikki metodit algoritmiluokissa ovat ulos näkyviä. Käytännössä ulkopäin kutsuttaviksi on tarkoitettu vain **Encode(...)** ja **Decode(...)** metodit luokissa, jotka toteuttavat niille syötteinä annettujen sisäänluettavan ja uloskirjoitettavan virtojen (.NET *Stream* objekti tai sen perivä objekti) joko pakkauksen, tai purkamisen käyttäen kyseistä algoritmia.

Algoritmit lukevat sisäänluettavan tietovirran kokonaisuudessa alusta loppuun, pakkaavat/purkavat luetut tavut luokan mukaisella algoritmilla, sekä kirjoittavat pakatun tai puretun tiedon uloskirjoitettavaan virtaan.

Huffman algoritmin tapauksessa uloskirjoitettava tietovirta sisältää lyhyen otsaketiedon jonka perusteella voidaan verifioida purkaessa että alkuperäinen tavumäärä on purettu, koska Huffman koodattu data on bittiorientoitunutta eikä viimeinen koodattu tavu välttämättä sisällä tavurajan mukaan viimeistä koodibittiä. Lisäksi purkamista varten Huffman koodattu data sisältää ennen varsinaista pakattua tietoa pakkaukseen käytetyn Huffman -puun, joka on koodattu ns. post-order traversal -järjestyksessä ja joka puretaan takaisin puurakenteeksi pinopohjaisella menetelmällä (kts. [ECE264: Huffman Coding](https://engineering.purdue.edu/ece264/17au/hw/HW13?alt=huffman).)

Kuten Huffman algoritmissa, LZ77 algoritmin tapauksessa uloskirjoitettava tietovirta sisältää myös lyhyen otsaketiedon jonka perusteella voidaan verifioida purkaessa että alkuperäinen tavumäärä on purettu koska tiedoston viimeinen koodattu tavu voi sisältää vain osan biteistä tietoa. Muuten pakattu data on itsekuvaavaa purkamisen logiikan osalta. Tämä johtuu siitä, että LZ77 luokan konstruktori käyttää [oletuksena] 4 kilotavun pakkausikkunaa ja sitä myös omana projektina oleva pakkaus-/purkuohjelma käyttää kummassakin tapauksessa.

Algoritmien toteutukset myös käyttävät sisäisesti sekä sisäänluettavan että uloskirjoitettavan tiedon osalta omaa puskurointia (64 kilotavua / puskuri Huffman algoritmissa ja 128 kilotavua / puskuri LZ77 algoritmissa). Tämä johtuu siitä että luku- ja kirjoitus-I/O:n kannalta yksittäisen tavun lukeminen ja/tai kirjoittaminen on potentiaalisesti erittäin hidasta, joka on logiikan kannalta se tapa miten pakkausalgoritmit sisäisesti käsittelevät tietoa (luetaan seuraava sisääntuleva tavu yksi kerrallaan, ja pakataan se koodattuun muotoon).

.NET Stream -pohjaiset luokat *saattavat* sisäisesti toteuttaa myös tiedon puskurointia, mutta tietoinen päätös tehtiin algoritmeja toteuttaessa että tässä tehdyt toteutukset sisältävät oman puskuroinnin. Tämä väistämättä monimutkaisti itse toteutuksia jonkin verran, koska sisäisissä metodeissa kaikki varsinainen luku ja kirjoitus tapahtuu kohdistuen puskureihin (byte[] arrayt) ja algoritmit sisältävät siten ylimääräistä logiikkaa näiden puskureiden täyttämiseen ja uloskirjoittamiseen ylivuotojen tapahtuessa, mitä käy potentiaalisesti kesken enkoodatun tiedon tuottamista tai lisätavujen lukemista.

Keskeneräiseksi jääneessä Deflate-luokassa erillistä sisääntulevan ja uloskirjoitettavan tiedon puskuria ei ole, koska Deflate jo itsessään perustuu blokkipohjaiseen pakkaamiseen, jossa syöte pilkotaan palasiin ja jokainen sen mukainen blokki pakataan erillisesti uloskirjoitettavaksi. Tällöin sisäänluettava ja uloskirjoitettava yksittäinen blokki toimii samalla luku- ja kirjoituspuskurina.

### tiracompress.csproj

Hakemistossa */src/program/*.

Konsolisovellus, jonka avulla voidaan terminaalista käsin kutsua toteutettuja pakkausluokkia ja pakata tai purkaa komentorivillä määritettyjä tiedostoja tiedostojärjestelmässä ([kts. käyttöohjeet](./kayttoohje.md).)

Konsolisovelluksen toteutus on varsin suoraviivainen eikä sisällä erityisen paljon koodia, lähinnä komentoriviparametrien purkamislogiikka, sekä pakkaus- ja purkuosioiden kutsuminen algorithms.proj projektista, joihin konsolisovellusprojekti linkittää.

### tests.csproj

Hakemistossa */src/tests/*

Yksikkötestiprojekti, joka linkittää algorithms.csproj -projektiin, ja sisältää yhden testi-luokan / algoritmi. Testiprojekti ei linkitä konsolisovellukseen eikä testaa sitä mitenkään, koska toteutuksen osalta algoritmien oikeellisuus ja testaus oli oleellinen.

Testiluokissa koitetaan testata toteutettuja algoritmeja kokonaisuutena (roundtrip testaus) sekä yksittäisiä metodeja mahdollisimman hyvän testikattavuuden saavuttamiseksi, mutta koska pakkausalgoritmit ovat väistämättä luonteensa vuoksi hyvin pitkälti tilakoneen tapaisia, yksittäisten metodien testaaminen järkevällä tavalla on jossain määrin haasteellista. Näinollen testien osalta on pääsääntöisesti pyritty varmistamaan esimerkiksi oikeellinen ulostulo annettujen syötteiden perusteella, mutta ei vaikkapa yritetty testata mahdollimman aika-/tilatehokasta toimintaa.

Lisätietoja itse testeista ja sen tuottamista datoista [testausdokumentissa](./testausdokumentti.md).

## Aika- ja tilavaativuusanalyysi

Varsinaista pitkälle menevää analyysia toteutettujen algoritmien aika- tai tilavaativuuksista ei ole erityisesti tehty. Sekä Huffman että LZ77 algoritmien toteutukset ovat hyvin perusmuotoisia, eikä erityistä optimointia tai monimutkaisempia tietorakentita/lähestymistapoja esimerkiksi LZ77:n tapauksessa jonovastaavuuksien etsintään ole tehty. LZSS mukaisella variantilla LZ77 algoritmin osalta olisi tilavaativuutta pakatun tiedon osalta (yhden bitin markintä jolla erotellaan literaali ja takaisinpäinviittaukset, nyt erottelu tapahtuu length -kentän perusteella) saanut vielä edelleen pienennettyä mutta projektissa toteutettiin "puhdas" LZ77 ilman myöhempiä parannuksia. Vastaavasti Huffman algorimissa optimoidummalla Huffman-puun enkoodauksella pakatun tiedon oheen olisi saanut tilavaativuutta jonkin verran pienennettyä (mutta ei merkittävästi).

### Huffman

Huffman -algoritmin osalta aikavaativuus on O(n), koska algoritmi lukee sisääntulevan tietovirran lineaarisesti kahteen otteeseen läpi: ensimmäisen kerran muodostaakseen esiintyvien symbolien (tavujen) esiintymistiheydet, jonka perusteella itse pakkauspuu muodostetaan, ja toisen kerran koodataakseen symbolit käyttäen muodostettua puuta.

Tilavaativuuden osalta Huffman algoritmi pakkaa sisääntulevat symbolit (tavut) mahdollisimman optimaalisella bittiesityksellä perustuen symbolien esiintymistiheyksiin, jonka seurauksena yleisimmät symbolit saavat lyhyimmän bittikoodatun muodon ja harvinaisimmat pisimmän. Pakkauksen tehokkuus kuitenkin riippuu täysin lähdedatan eri symbolien määrästä ja niiden esiintymistiheyksien tasapainosta, tai tasapainottomuudesta. Lähdedata joka on painottunut tasaisesti jakautuneeseen esiintymistiheyteen ja sisältää mahdollisimman lähelle kaikkia potentiaalisia tavuarvoja (0-255) ei käytännössä pakkaudu lainkaan Huffman koodauksella, koska muodostettavan puun perusteella muodostuvat bittikoodaukset ovat kaikki tasaisesti lähellä yhtä pitkiä bittijonoja (8 bittiä). Tähän joukkoon lähdedataa kuuluu esimerkiksi jo valmiiksi pakattu binääridata ja useat binääritiedostoformaatit. Parhaiten puoledtaan pakkautuu lähdedata, joka on esimerkiksi luonnollista kieltä ja siten symboolit eli tavut painottuvat vahvasti eri akkosten esitysmuotoihin, ja luonnolliselle kielellä tyypillisesti eri kirjaimien välillä on huomattavat erot esiintymistiheyksissä.

Vaikka itse koodattu sisältö pakkautuisikin tehokkaasti Huffman-algoritmilla, tilavaativuuden osalta pakatussa datassa ylimääräisen tilantarpeen aiheuttava Huffman-puun tallentaminen purkamista varten aiheuttaa käytännössä tilanteen, jossa erittäin lyhyt syöte kasvattaa pakatun tiedoston kokoa koska puun kuvaava data yliajaa pakatun tietosisällön aiheuttaman säästön tilassa. Käytännössä tämä kuitenkin tulee vastaan vain syötteillä jotka ovat muutamien kymmenien tavujen kokoisia.

### LZ77

LZ77 -algoritmin osalta aikavaativuus on jotain O(n) ja O(m*n) välillä, jossa m riippuu pakkausikkunan koosta ja kokonaisaikavaativuus siitä kuinka pitkältä pakkausikkunasta löytyy takaisinpäinviittauksen symboolivastaavuus sisäänluettavalle tavulle, sekä kuinka pitkä vastaavuus kerralla löydetään. Tässä projektissa käytetty symboolivastaavuuksien etsimisalgoritmi on suht naiivi, ja se skannaa lineaarisesti pakkausikkunaa taaksepäin löytääkseen vastaavuuksia. Vastaavuuksien osalta on minimikynnysarvo (3 samaa tavua) jonka pituiset tai pidemmät viittaukset kelpuutetaan, mutta siten että algoritmi yrittää skannata ikkunaa taaksepäin löytääkseen vielä mahdollisesti pidempiä viittauksia maksimissaan viisi kertaa. Mikäli viittaus kuitenkin täyttää pienimmän mahdollisen kelpuutetun pituuden (viisi samaa tavua), etsintä lopetetaan siihen kohtaan. Näillä kynnysarvoilla, sekä 4 kilotavun oletusikkunalla, on pyritty nopeuttamaan vastaavuuksia etsimistä mahdollisimman paljon. 

Kynnysarvot pituuksille ovat myös koodattu muuttujiksi lähdekoodiin, joten niitä muuttamalla on mahdollista vaikuttaa algoritmin nopeuteen ja osittain myös tilatehokkuuteen.

Tilavaativuuden osalta LZ77-algoritmin toteutus on perusmuotoisessa LZ77 (verrattuna LZSS varianttiin johon viitataan aiemmin) väistämättä jonkin verran tuhlaava, koska sekä literaalit että takaisinpäinviittaukset sisältävät aina yhden 8-bitin tavun pituustiedon joka literaaleilla on aina arvossa nolla, mutta käytännössä minimipituuskynnysarvolla sekä useammalla skannausyrityksellä viittaukset tekstimuotoisessa lähdetiedossa aiheuttavat sen että LZ77 pakkaa noin vähän yli puoleen alkuperäisestä koosta. LZSS variantilla tätä olisi saanut entisestään tehostettua, sekä luonnollisesti isommalla pakkausikkunalla mutta pakkausikkunan koolla on suora vaikutus koodausnopeuteen johtuen lineaariskannauksesta.

### Deflate

Deflate-algoritmin luokka on vain osittain loppuun asti toteutettu, josta syystä aikavaativuus ja tilavaativuusanalyyseja ei ole mahdollista tämän projektin toteutuksesta tehdä.

## LLM:n käyttö

Projektissa ei ole käytetty valmistelun tai toteutuksen osalta laajoja kielimalleja, kuten ChatGPT.