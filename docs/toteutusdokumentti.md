# Toteutusdokumentti

## Yleisrakenne

Ohjelmisto on yleistasolla toteutettu kolmena erillisenä .NET projektina:

### algorithms.csproj

Hakemistossa */src/algorithms/*.

Itse pakkaus- ja purkualgoritmit toteuttava projekti, jossa jokainen toteutettu algoritmi (Huffman, LZ77 tällä hetkellä) on omana erillisenä .cs kooditiedostona/luokkana. 

Vaikka normaalitilanteessa luokan enkapsulointiin kuuluisi käyttää public näkyvyyttä vain niiden metodien osalta joita normaalitilanteessa tarvitaan kutsua luokan ulkoa, testausprojektin johdosta käytännössä kaikki metodit algoritmiluokissa ovat ulos näkyviä. Käytännössä ulkopäin kutsuttaviksi on tarkoitettu vain **Encode(...)** ja **Decode(...)** metodit luokissa, jotka toteuttavat niille syötteinä annettujen sisäänluettavan ja uloskirjoitettavan virtojen (.NET *Stream* objekti tai sen perivä objekti) joko pakkauksen, tai purkamisen käyttäen kyseistä algoritmia.

Algoritmit lukevat sisäänluettavan tietovirran kokonaisuudessa alusta loppuun, pakkaavat/purkavat luetut tavut luokan mukaisella algoritmilla, sekä kirjoittavat pakatun tai puretun tiedon uloskirjoitettavaan virtaan.

Huffman algoritmin tapauksessa uloskirjoitettava tietovirta sisältää lyhyen otsaketiedon jonka perusteella voidaan verifioida purkaessa että alkuperäinen tavumäärä on purettu, koska Huffman koodattu data on bittiorientoitunutta eikä viimeinen koodattu tavu välttämättä sisällä tavurajan mukaan viimeistä koodibittiä. Lisäksi purkamista varten Huffman koodattu data sisältää ennen varsinaista pakattua tietoa pakkaukseen käytetyn Huffman -puun, joka on koodattu ns. post-order traversal -järjestyksessä ja joka puretaan takaisin puurakenteeksi pinopohjaisella menetelmällä (kts. [ECE264: Huffman Coding](https://engineering.purdue.edu/ece264/17au/hw/HW13?alt=huffman).)

LZ77 algoritmin tapauksessa koodattu tieto alkaa suoraan ensimmäisestä tavusta, koska purkaminen ei vaadi erityistä metatietoa käytetystä pakkauksesta, poislukien käytetty ikkunan koko jota uloskirjoitettuun dataan ei ole tallennettu. Muuten pakattu data on itsekuvaavaa purkamisen logiikan osalta. Tämä johtuu siitä, että LZ77 luokan oletuskonstruktori käyttää [oletuksena] 32 kilotavun pakkausikkunaa ja sitä myös omana projektina oleva pakkaus-/purkuohjelma käyttää kummassakin tapauksessa.

Algoritmien toteutukset myös käyttävät sisäisesti sekä sisäänluettavan että uloskirjoitettavan tiedon osalta omaa puskurointia (64 kilotavua / puskuri). Tämä johtuu siitä että luku- ja kirjoitus-I/O:n kannalta yksittäisen tavun lukeminen ja/tai kirjoittaminen on potentiaalisesti erittäin hidasta, joka on logiikan kannalta se tapa miten pakkausalgoritmit sisäisesti käsittelevät tietoa (luetaan seuraava sisääntuleva tavu yksi kerrallaan, ja pakataan se koodattuun muotoon). .NET Stream -pohjaiset luokat *saattavat* sisäisesti toteuttaa myös tiedon puskurointia, mutta tietoinen päätös tehtiin algoritmeja toteuttaessa että tässä tehdyt toteutukset sisältävät oman puskuroinnin. Tämä väistämättä monimutkaisti itse toteutuksia jonkin verran, koska sisäisissä metodeissa kaikki varsinainen luku ja kirjoitus tapahtuu kohdistuen puskureihin (byte[] arrayt) ja algoritmit sisältävät siten ylimääräistä logiikkaa näiden puskureiden täyttämiseen ja uloskirjoittamiseen ylivuotojen tapahtuessa, mitä käy potentiaalisesti kesken enkoodatun tiedon tuottamista tai lisätavujen lukemista.

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

Varsinaista pitkälle menevää analyysia toteutettujen algoritmien aika- tai tilavaativuuksista ei ole toistaiseksi tehty.

### Huffman

Huffman -algoritmin osalta aikavaativuus on O(n), koska algoritmi lukee sisääntulevan tietovirran lineaarisesti kahteen otteeseen läpi: ensimmäisen kerran muodostaakseen esiintyvien symbolien (tavujen) esiintymistiheydet, jonka perusteella itse pakkauspuu muodostetaan, ja toisen kerran koodataakseen symbolit käyttäen muodostettua puuta.

### LZ77

LZ77 -algoritmin osalta aikavaativuus on jotain O(n) ja O(m*n) välillä, jossa m riippuu pakkausikkunan koosta ja kokonaisaikavaativuus siitä kuinka pitkältä pakkausikkunasta löytyy symboolivastaavuus sisäänluettavalle tavulle sekä kuinka pitkä vastaavuus kerralla löydetään.