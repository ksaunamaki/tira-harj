# Testausdokumentti

## Yksikkötestit

### Mitä on testattu

Automaattiset yksikkötestit on määritelty ajettavaksi *(/src/algorithms/* hakemistossa oleville, varsinaisia pakkaus- ja purkualgoritmeja toteuttaville, luokille jotka ovat yhdessä .cs kooditiedostossa per toteutettu algoritmi osana **algorithms.csproj** projektitiedostoa. Yksikkötesteillä on pyritty saavuttamaan mahdollisimman suuri kattavuus koodirivien osalta, mutta johtuen pakkausalgoritmeille tyypillisestä sisäisen tilan ylläpidosta johtuvasta ajoittaisesta monimutkaisuudesta, osa testeistä väistämättä joudutaan ajamaan osin "ylhäältä alas" pohjaisesti; käytännössä siis niin että yksikkötestillä vertaillaan että algoritmin pakkausosiolle syötetty pakkaamaton data tulee pakkausalgoritmin purkuosiolta takaisin tavulleen samanmuotoisena (ns. roundtrip-testaus).

Niiltä osin, kun algoritmin yksittäisiä - normaalisti sisäisiä - metodeja jotka toteuttavat tietyn funktion, kuten esim. yksittäisen koodatun entiteetin lisääminen ulosmenevään tietovirtaan, voidaan testata suoraan kutsumalla ko. metodia, on näin tehty.

Yksikkötestausta EI ole määritelty itse algoritmiluokkia ajavalle konsolisovellukselle, koska sen funktio on toimia vain käyttöliittymänä algoritmien hyödyntämiseen, eikä sisällä varsinaisesti mitään itsenäistä algoritmeihin liittyvää logiikkaa. 

### Testidata

Testidatana on käytetty testiluokissa olevia kiinteitä testitavujonoja, joissa osan osalta (esim. Huffman algoritmi) on etukäteen ollut manuaalisesti laskettavissa koodattu esitystapa jota vertaillaan algoritmin tuottamaan tulokseen. 

Lisäksi testidatana osana automaattitestejä on kaksi isoa tiedostoa [Silesia corpus](https://github.com/MiloszKrajewski/SilesiaCorpus) kokoelmasta, Webster sanakirja sekä röntgenkuvatiedosto. Näitä vasten ajetaan sekä Huffman että LZ77 algoritmin testejä round-trip menetelmällä, koodaten ensin koko tiedoston sisältö tilapäiseen tiedostoon jonka jälkeen taas purkaen sen toiseen tilapäiseen tiedostoon, lopussa vertaillen että alkuperäinen tiedosto ja toinen tilapäinen tiedosto vastaavat toisiaan tavutasolla.

### Testauskirjasto

Testaukseen on käytetty .NET projekteissa yleisesti käytettyä [xUnit -kirjastoa](https://xunit.net/), jota hyväksi käyttävät testiluokat on omana projektinaan */src/tests/* alihakemistossa. Testiprojekti puolestaan sisältää viittauksen itse **algorithms.csproj** projektiin jotta testiluokat näkevät ko. algoritmiluokat.

### Miten testit ajetaan

Automaattiset yksikkötestit saa ajettua [käyttöohjeissa](./kayttoohje.md) mainitulla dotnet komennolla:  

```
dotnet test tests/tests.csproj
```

## Koodin kattavuus

### Miten on luotu

Koodin testikattavuus yksikkötestien perusteella on laskettu xUnit .NET projekteissa vakiona käytettyä [coverlet](https://github.com/coverlet-coverage/coverlet) kirjastoa käyttäen. Koska coverlet muodostaa kattavuusraportin koneellisesti luettavassa tiedostomuodossa, repositoryyn on määritely käyttöön myös erillinen dotnet tool:n avulla asennettava raporttityökalu, joka osaa muuntaa coverlet tiedoston ihmisille helpommin luettavaa HTML muotoon (muiden formaattien ohella).

Kattavuusraportit saa ajettua käyttäen yksikkötestiä vastaavaa komentoa käyttäen:

```
dotnet test tests/tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput="coverage.xml"
```

Sekä HTML raportin generoiminen edellä olevan perusteella:

```
dotnet tool run reportgenerator -reports:tests/coverage.xml -targetdir:tests/coveragereport -reporttypes:Html 
```

Jälkimmäinen komento luo */src/tests/coveragereport/* hakemistoon HTML raportin jonka saa auki avaamalla index.html tiedoston selaimeen. Muokkaamalla komentoriviä raportin saa myös muodostettua johonkin toiseen hakemistoon.

### Tämänhetkinen kattavuus

Tämän dokumentin päivittämishetkellä testikattavuus on seuraavanlainen (otettu coverlet raportista):

<div class="table-responsive"><table class="overview table-fixed stripped"><colgroup><col class="column-min-200"><col class="column90"><col class="column105"><col class="column100"><col class="column70"><col class="column98"><col class="column112"><col class="column90"><col class="column70"><col class="column98"><col class="column112"></colgroup><thead><tr class="header"><th></th><th colspan="6" class="center">Line coverage</th><th colspan="4" class="center">Branch coverage</th></tr><tr><th><i class="icon-down-dir_active"></i>Name</th><th class="right"><i class="icon-down-dir"></i>Covered</th><th class="right"><i class="icon-down-dir"></i>Uncovered</th><th class="right"><i class="icon-down-dir"></i>Coverable</th><th class="right"><i class="icon-down-dir"></i>Total</th><th colspan="2" class="center"><i class="icon-down-dir"></i>Percentage</th><th class="right"><i class="icon-down-dir"></i>Covered</th><th class="right"><i class="icon-down-dir"></i>Total</th><th colspan="2" class="center"><i class="icon-down-dir"></i>Percentage</th></tr></thead><tbody><tr codeelement-row=""><th><i class="icon-minus"></i> algorithms</th><th class="right">1303</th><th class="right">113</th><th class="right">1416</th><th class="right">2942</th><th class="right" title="1303/1416">92%</th><th class="right"><coverage-bar><table class="coverage"><td class="covered92 green"></td><td class="covered8 red"></td></table></coverage-bar></th><th class="right">325</th><th class="right">384</th><th class="right" title="325/384">84.6%</th><th class="right"><coverage-bar><table class="coverage"><td class="covered85 green"></td><td class="covered15 red"></td></table></coverage-bar></th></tr><tr class-row=""><td><a href="algorithms_Deflate.html">Tiracompress.Algorithms.Deflate</td><td class="right"> 409 </td><td class="right"> 78 </td><td class="right"> 487 </td><td class="right"> 1054 </td><td class="right" title="409/487"> 83.9% </td><td class="right"><coverage-bar><table class="coverage"><td class="covered84 green"></td><td class="covered16 red"></td></table></coverage-bar></td><td class="right"> 100 </td><td class="right"> 135 </td><td class="right" title="100/135"> 74% </td><td class="right"><coverage-bar><table class="coverage"><td class="covered74 green"></td><td class="covered26 red"></td></table></coverage-bar></td></tr><tr class-row=""><td><a href="algorithms_Huffman.html">Tiracompress.Algorithms.Huffman</td><td class="right"> 360 </td><td class="right"> 19 </td><td class="right"> 379 </td><td class="right"> 806 </td><td class="right" title="360/379"> 94.9% </td><td class="right"><coverage-bar><table class="coverage"><td class="covered95 green"></td><td class="covered5 red"></td></table></coverage-bar></td><td class="right"> 99 </td><td class="right"> 117 </td><td class="right" title="99/117"> 84.6% </td><td class="right"><coverage-bar><table class="coverage"><td class="covered85 green"></td><td class="covered15 red"></td></table></coverage-bar></td></tr><tr class-row=""><td><a href="algorithms_Lz77.html">Tiracompress.Algorithms.Lz77</td><td class="right"> 534 </td><td class="right"> 16 </td><td class="right"> 550 </td><td class="right"> 1082 </td><td class="right" title="534/550"> 97% </td><td class="right"><coverage-bar><table class="coverage"><td class="covered97 green"></td><td class="covered3 red"></td></table></coverage-bar></td><td class="right"> 126 </td><td class="right"> 132 </td><td class="right" title="126/132"> 95.4% </td><td class="right"><coverage-bar><table class="coverage"><td class="covered95 green"></td><td class="covered5 red"></td></table></coverage-bar></td></tr></tbody></table></div>

Rivi- ja haaraumakattavuuden totaaleja tiputtavaa hieman automaattitesteissä mukana olevan Deflate-luokan testit, johtuen siitä että Deflate toteutus ei ole valmiiksi tehty.

## Suorituskykytestaus

Suorituskykyä on manuaalisesti testattu toteutettujen algoritmien osalta käyttäen edellämainittua Silesia corpus testimateriaalia, joissa pakattavana on yksittäisiä tiedostoja.

Operaatioon (pakkaus ja purku) kulunut aika testaamisessa on mitattu ohjelman sisäisesti mittaaman ja raportoiman algoritmin suoritusaikana. Lopuksi takaisinpurettun tiedoston oikeellisuutta on vertailtu alkuperäiseen syötetiedostoon käyttäen macOS:n komentorivityökalua cmp, joka vertaa tiedostoja tavutasolla toisiinsa.

Kaikki suorituskykytestaaminen on suoritettu MacBook Air, vuoden 2022 mallilla (M2 prosessori ja SSD levy). 

### Huffman

| Testitiedosto | Pakkausaika | Pakattu koko | Purkamisaika |
| ----- | ------------- | ------ | ------ |
| dickens (Tekstitiedosto, 10192446 tavua) | 0.321 sec | 5825921 tavua (~57% alkuperäisestä) | 0.648 sec |
| mr (Kuvatiedosto, DICOM muodossa, 9970564 tavua) | 0.321 sec | 4622875 tavua (~46% alkuperäisestä) | 0.493 sec |
| nci (Tekstitiedosto, 33553445 tavua) | 0.806 sec | 10223905 tavua (~30% alkuperäisestä) | 1.131 sec |
| reymont (PDF-tiedosto, 6627202 tavua) | 0.235 sec | 4031295 tavua (~60% alkuperäisestä) | 0.428 sec |
| mozilla 2/bloaturls.txt (Tekstitiedosto, 217 tavua) | 0.0003 sec | 115 tavua (~92% alkuperäisestä) | 0.0004 sec |

Perustuen edellämainittuihin testiaineistoihin, Huffman-algoritmin toteutuksen keskimääräinen pakkausnopeus on noin 30 megatavua sekunnissa ja purkunopeus hieman hitaampi. Pienemmän purkunopeuden selitys todennäköisimmin on hieman epätehokas tapa, jolla Huffman-luokassa suoritetaan koodatun bittiesityksen muunto halutuksi symboliksi kulkemalla Huffman puurakennetta alas kunnes haluttu lehtisolmu löytyy. Käyttämällä jotain muuta rakennetta bittijonojen täsmäytykseen luettaessa sisääntulevasta pakatusta datasta bittejä purkunopeus olisi mahdollisesti tehokkaampi.

### LZ77

| Testitiedosto | Pakkausaika | Pakattu koko | Purkamisaika |
| ----- | ------------- | ------ | ------ |
| dickens (Tekstitiedosto, 10192446 tavua) | 30.922 sec | 7854093 tavua (~77% alkuperäisestä) | 0.533 sec |
| mr (Kuvatiedosto, DICOM muodossa, 9970564 tavua) | 25.829 sec | 6336499 tavua (~64% alkuperäisestä) | 0.396 sec |
| nci (Tekstitiedosto, 33553445 tavua) | 29.234 sec | 9737020 tavua (~29% alkuperäisestä) | 0.710 sec |
| reymont (PDF-tiedosto, 6627202 tavua) | 9.527 sec | 4219019 tavua (~64% alkuperäisestä) | 0.282 sec |
| mozilla 2/bloaturls.txt (Tekstitiedosto, 217 tavua) | 0.001 sec | 167 tavua (~77% alkuperäisestä) | 0.0005 sec |

Perustuen edellämainittuihin testiaineistoihin, LZ77-algoritmin toteutuksen keskimääräinen pakkausnopeus on huomattavasti hitaampi kuin Huffmanilla, johtuen tarpeesta skannata pakkausikkunaa taaksepäin. Purkunopeuden osalta LZ77 purku on nopeampaa kuin Huffmanissa, todennäköisimmin johtuen siitä että Huffman toteutuksessa bittikoodauksen purkaminen jokaisen symbolin osalta vaatii Huffman-puun kulkemista juuresta symbolisolmuun kun taas LZ77 algoritmissa koodattu tieto on matemaattisesti nopeammin purettavissa tavujen bittimanipulaatiolla, sekä viittausten osalta useampi symboli kerralla kopioitavissa pakkausikkunasta bittikoodauksen purkamisen jälkeen.

Tilankäytön osalta algoritmi oli jonkin verran epätehokkaampi kuin Huffman algoritmi, poislukien nci tiedoston ja bloaturls.txt tiedoston pakkaukset joissa LZ77 tuotti paremman tuloksen. Erityisen lyhyen syötteen tiedostoissa, joita bloaturls.txt edusti, tämä ero on selitettävissä Huffman puun tallennuksen aiheuttamasta ylimääräisestä tiedosta.