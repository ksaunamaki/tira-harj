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

LZ77 algoritmin tapauksessa koodattu tieto alkaa suoraan ensimmäisestä tavusta, koska purkaminen ei vaadi erityistä metatietoa käytetystä pakkauksesta, poislukien käytetty ikkunan koko jota uloskirjoitettuun dataan ei ole tallennettu. Muuten pakattu data on itsekuvaavaa purkamisen logiikan osalta. Tämä johtuu siitä, että LZ77 luokan oletuskonstruktori käyttää [oletuksena] 32 kilotavun pakkausikkunaa ja sitä myös omana projektina oleva pakkaus-/purkuohjelma käyttää kummassakin tapauksessa.

Algoritmien toteutukset myös käyttävät sisäisesti sekä sisäänluettavan että uloskirjoitettavan tiedon osalta omaa puskurointia (64 kilotavua / puskuri). Tämä johtuu siitä että luku- ja kirjoitus-I/O:n kannalta yksittäisen tavun lukeminen ja/tai kirjoittaminen on potentiaalisesti erittäin hidasta, joka on logiikan kannalta se tapa miten pakkausalgoritmit sisäisesti käsittelevät tietoa (luetaan seuraava sisääntuleva tavu yksi kerrallaan, ja pakataan se koodattuun muotoon).

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

Varsinaista pitkälle menevää analyysia toteutettujen algoritmien aika- tai tilavaativuuksista ei ole erityisesti tehty. Sekä Huffman että LZ77 algoritmien toteutukset ovat hyvin perusmuotoisia, eikä erityistä optimointia tai monimutkaisempia lähestymistapoja esimerkiksi LZ77:n tapauksessa jonovastaavuuksien etsintään ole tehty. Optimoidummalla toteutuksella LZ77 algoritmin osalta olisi tilavaativuutta pakatun tiedon osalta varmasti saanut pienennettyä; vastaavasti Huffman algorimissa optimoidummalla Huffman-puun enkoodauksella pakatun tiedon oheen olisi saanut tilavaativuutta jonkin verran pienennettyä (mutta ei merkittävästi).

### Huffman

Huffman -algoritmin osalta aikavaativuus on O(n), koska algoritmi lukee sisääntulevan tietovirran lineaarisesti kahteen otteeseen läpi: ensimmäisen kerran muodostaakseen esiintyvien symbolien (tavujen) esiintymistiheydet, jonka perusteella itse pakkauspuu muodostetaan, ja toisen kerran koodataakseen symbolit käyttäen muodostettua puuta.

Tilavaativuuden osalta Huffman algoritmi pakkaa sisääntulevat symbolit (tavut) mahdollisimman optimaalisella bittiesityksellä perustuen symbolien esiintymistiheyksiin, jonka seurauksena yleisimmät symbolit saavat lyhyimmän bittikoodatun muodon ja harvinaisimmat pisimmän. Pakkauksen tehokkuus kuitenkin riippuu täysin lähdedatan eri symbolien määrästä ja niiden esiintymistiheyksien tasapainosta, tai tasapainottomuudesta. Lähdedata joka on painottunut tasaisesti jakautuneeseen esiintymistiheyteen ja sisältää mahdollisimman lähelle kaikkia potentiaalisia tavuarvoja (0-255) ei käytännössä pakkaudu lainkaan Huffman koodauksella, koska muodostettavan puun perusteella muodostuvat bittikoodaukset ovat kaikki tasaisesti lähellä yhtä pitkiä bittijonoja (8 bittiä). Tähän joukkoon lähdedataa kuuluu esimerkiksi jo valmiiksi pakattu binääridata ja useat binääritiedostoformaatit. Parhaiten puoledtaan pakkautuu lähdedata, joka on esimerkiksi luonnollista kieltä ja siten symboolit eli tavut painottuvat vahvasti eri akkosten esitysmuotoihin, ja luonnolliselle kielellä tyypillisesti eri kirjaimien välillä on huomattavat erot esiintymistiheyksissä.

Vaikka itse koodattu sisältö pakkautuisikin tehokkaasti Huffman-algoritmilla, tilavaativuuden osalta pakatussa datassa ylimääräisen tilantarpeen aiheuttava Huffman-puun tallentaminen purkamista varten aiheuttaa käytännössä tilanteen, jossa erittäin lyhyt syöte kasvattaa pakatun tiedoston kokoa koska puun kuvaava data yliajaa pakatun tietosisällön aiheuttaman säästön tilassa. Käytännössä tämä kuitenkin tulee vastaan vain syötteillä jotka ovat muutamien kymmenien tavujen kokoisia.

### LZ77

LZ77 -algoritmin osalta aikavaativuus on jotain O(n) ja O(m*n) välillä, jossa m riippuu pakkausikkunan koosta ja kokonaisaikavaativuus siitä kuinka pitkältä pakkausikkunasta löytyy takaisinpäinviittauksen symboolivastaavuus sisäänluettavalle tavulle, sekä kuinka pitkä vastaavuus kerralla löydetään. Kuten edellä todettu, tässä projektissa käytetty symboolivastaavuuksien etsimisalgoritmi on naiivi, ja skannaa pakkausikkunaa taaksepäin löytääkseen ensimmäisen vastaantulevan vastaavuuskohdan riippumatta siitä olisiko ikkunassa aiemmin löydettävissä oleva pidempi vastaavuus. Tässä suhteessa keskeneräiseen Deflate-algoritmiin luokkaan luotu modifioitu versio on hieman tehokkaampi toteutus, koska se noudattaa Deflate-algoritmin spesifikaatiossa määriteltyä kolmen symbolin minimiä vastaavuuden suhteen.

Koska symboolivastaavuuksista pakkausikkunasta kelpuutetaan myös yhden symbolin pituiset vastaavuudet, käytännössä ei voi olla syötettä missä algoritmi koskaan tutkisi pakkausikkunaa pidemmälle kuin on erilaisia mahdollisia symboleita syötteessä, eli 256 eri tavua. Pidemmän minimivastaavuusvaatimuksen kanssa aikavaativuus helposti kasvaisi nykyisellä totetutuksella suureksi, koska silloin olisi mahdollista sopivalla syötteellä että algotitmi lähes jokaisella kierroksella skannaisi pakkausikkunaa lopusta aina alkuun asti. Deflate-luokassa, jossa minimi on toteutettu, tätä on mitigoitu käyttämällä strategiaa jossa takaisinpäinskannaus ei koskaan käsittele koko pakkausikkunaa mikäli vastaavuutta ei löydy, vaan se puolittaa haku-avaruutta joka kerralla kun liian lyhyt vastaavuus löytyy ja pitää jatkaa ikkunan taaksepäin tutkimista.

Tilavaativuuden osalta LZ77-algoritmin toteutus tässä projektissa on myös varsin epätehokas, johtuen osittain edellämainitusta minimivastaavuuden puutteesta takaisinpäinviittausten luomisessa. Tämä käytännössä helposti aiheuttaa tilanteen missä algoritmi tuottaa ison määrän takaisinpäinviittauksia vain yhdelle tavulle, joka koodattuna ei ole sen tehokkaampi kuin suoran literaalin koodaaminen.

Modifioimalla koodatun tiedon esitystapaa tilavaativuutta saisi tehostettua sekä takaisinpäinviittausten että literaalienkin koodauksen osalta, mutta nyt implementoitu toteutus käyttää puhtaasti tavurajojen mukaan menevää koodausta jossa etäisyys on 16-bittinen (kaksi tavua) luku. Tätä 16-bittistä lukua käytetään sekä literaaleille että takaisinpäinviittauksille ja sitä myötä erityisesti literaalitkoodaus, mutta myös takaisinpäinviittauksen koodaus alle 4 symbolin pituisten viittausten osalta tuhlaa tilaa pakatussa esitystavassa.

### Deflate

Deflate-algoritmin luokka on vain osittain loppuun asti toteutettu, josta syystä aikavaativuus ja tilavaativuusanalyyseja ei ole mahdollista tämän projektin toteutuksesta tehdä.

## LLM:n käyttö

Projektissa ei ole käytetty valmistelun tai toteutuksen osalta laajoja kielimalleja, kuten ChatGPT.