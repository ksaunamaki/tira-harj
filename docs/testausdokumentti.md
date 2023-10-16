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

<div class="table-responsive"><table class="overview table-fixed stripped"><colgroup><col class="column-min-200"><col class="column90"><col class="column105"><col class="column100"><col class="column70"><col class="column98"><col class="column112"><col class="column90"><col class="column70"><col class="column98"><col class="column112"></colgroup><thead><tr class="header"><th></th><th colspan="6" class="center">Line coverage</th><th colspan="4" class="center">Branch coverage</th></tr><tr><th><i class="icon-down-dir_active"></i>Name</th><th class="right"><i class="icon-down-dir"></i>Covered</th><th class="right"><i class="icon-down-dir"></i>Uncovered</th><th class="right"><i class="icon-down-dir"></i>Coverable</th><th class="right"><i class="icon-down-dir"></i>Total</th><th colspan="2" class="center"><i class="icon-down-dir"></i>Percentage</th><th class="right"><i class="icon-down-dir"></i>Covered</th><th class="right"><i class="icon-down-dir"></i>Total</th><th colspan="2" class="center"><i class="icon-down-dir"></i>Percentage</th></tr></thead><tbody><tr codeelement-row=""><th><i class="icon-minus"></i> algorithms</th><th class="right">1095</th><th class="right">103</th><th class="right">1198</th><th class="right">2521</th><th class="right" title="1095/1198">91.4%</th><th class="right"><coverage-bar><table class="coverage"><td class="covered91 green"></td><td class="covered9 red"></td></table></coverage-bar></th><th class="right">278</th><th class="right">334</th><th class="right" title="278/334">83.2%</th><th class="right"><coverage-bar><table class="coverage"><td class="covered83 green"></td><td class="covered17 red"></td></table></coverage-bar></th></tr><tr class-row=""><td><a href="algorithms_Deflate.html">Tiracompress.Algorithms.Deflate</td><td class="right"> 420 </td><td class="right"> 79 </td><td class="right"> 499 </td><td class="right"> 1064 </td><td class="right" title="420/499"> 84.1% </td><td class="right"><coverage-bar><table class="coverage"><td class="covered84 green"></td><td class="covered16 red"></td></table></coverage-bar></td><td class="right"> 102 </td><td class="right"> 137 </td><td class="right" title="102/137"> 74.4% </td><td class="right"><coverage-bar><table class="coverage"><td class="covered74 green"></td><td class="covered26 red"></td></table></coverage-bar></td></tr><tr class-row=""><td><a href="algorithms_Huffman.html">Tiracompress.Algorithms.Huffman</td><td class="right"> 360 </td><td class="right"> 19 </td><td class="right"> 379 </td><td class="right"> 806 </td><td class="right" title="360/379"> 94.9% </td><td class="right"><coverage-bar><table class="coverage"><td class="covered95 green"></td><td class="covered5 red"></td></table></coverage-bar></td><td class="right"> 99 </td><td class="right"> 117 </td><td class="right" title="99/117"> 84.6% </td><td class="right"><coverage-bar><table class="coverage"><td class="covered85 green"></td><td class="covered15 red"></td></table></coverage-bar></td></tr><tr class-row=""><td><a href="algorithms_Lz77.html">Tiracompress.Algorithms.Lz77</td><td class="right"> 315 </td><td class="right"> 5 </td><td class="right"> 320 </td><td class="right"> 651 </td><td class="right" title="315/320"> 98.4% </td><td class="right"><coverage-bar><table class="coverage"><td class="covered98 green"></td><td class="covered2 red"></td></table></coverage-bar></td><td class="right"> 77 </td><td class="right"> 80 </td><td class="right" title="77/80"> 96.2% </td><td class="right"><coverage-bar><table class="coverage"><td class="covered96 green"></td><td class="covered4 red"></td></table></coverage-bar></td></tr></tbody></table></div>

Rivi- ja haaraumakattavuuden totaaleja tiputtavaa hieman automaattitesteissä mukana olevan Deflate-luokan testit, johtuen siitä että Deflate toteutus on keskeneräinen.

## Suorituskykytestaus

Ohjelmistoa ja pakkausalgoritmien toteutuksen tehokkuutta ei vielä ole testattu laajalla aineistolla, lähinnä manuaalitestaus on keskittynyt testaamaan että toteutetut algoritmit tuottavat identtisen lopputuleman lähdeaineistoon roundtrip-testatessa pakkausta ja syntyneen tiedoston takaisin purkua.
