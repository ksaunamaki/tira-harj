# Testausdokumentti

## Yksikkötestit

### Mitä on testattu

Automaattiset yksikkötestit on määritelty ajettavaksi *(/src/algorithms/* hakemistossa oleville, varsinaisia pakkaus- ja purkualgoritmeja toteuttaville, luokille jotka ovat yhdessä .cs kooditiedostossa per toteutettu algoritmi osana **algorithms.csproj** projektitiedostoa. Yksikkötesteillä on pyritty saavuttamaan mahdollisimman suuri kattavuus koodirivien osalta, mutta johtuen pakkausalgoritmeille tyypillisestä sisäisen tilan ylläpidosta johtuvasta ajoittaisesta monimutkaisuudesta, osa testeistä väistämättä joudutaan ajamaan osin "ylhäältä alas" pohjaisesti; käytännössä siis niin että yksikkötestillä vertaillaan että algoritmin pakkausosiolle syötetty pakkaamaton data tulee pakkausalgoritmin purkuosiolta takaisin tavulleen samanmuotoisena (ns. roundtrip-testaus).

Niiltä osin, kun algoritmin yksittäisiä - normaalisti sisäisiä - metodeja jotka toteuttavat tietyn funktion, kuten esim. yksittäisen koodatun entiteetin lisääminen ulosmenevään tietovirtaan, voidaan testata suoraan kutsumalla ko. metodia, on näin tehty.

Yksikkötestausta EI ole määritelty itse algoritmiluokkia ajavalle konsolisovellukselle, koska sen funktio on toimia vain käyttöliittymänä algoritmien hyödyntämiseen, eikä sisällä varsinaisesti mitään itsenäistä algoritmeihin liittyvää logiikkaa. 

### Testidata

Testidatana on käytetty testiluokissa olevia kiinteitä testitavujonoja, joissa osan osalta (esim. Huffman algoritmi) on etukäteen ollut manuaalisesti laskettavissa koodattu esitystapa jota vertaillaan algoritmin tuottamaan tulokseen. Toisena vaihtoehtona olisi ollut käyttää ulkoisia testitiedostoja, esim. pakkausalgoritmien testauksessa tyypillisesti käytetyt [Silesia corpus](https://github.com/MiloszKrajewski/SilesiaCorpus) -testitiedostot, mutta niiden paikoitellen suuren tiedostokoon takia näitä ei tässä tapauksessa katsottu olevan järkevä lisätä repositoryn osaksi.

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

<div class="table-responsive"><table class="overview table-fixed stripped"><colgroup><col class="column-min-200"><col class="column90"><col class="column105"><col class="column100"><col class="column70"><col class="column98"><col class="column112"><col class="column90"><col class="column70"><col class="column98"><col class="column112"></colgroup><thead><tr class="header"><th></th><th colspan="6" class="center">Line coverage</th><th colspan="4" class="center">Branch coverage</th></tr><tr><th><i class="icon-down-dir_active"></i>Name</th><th class="right"><i class="icon-down-dir"></i>Covered</th><th class="right"><i class="icon-down-dir"></i>Uncovered</th><th class="right"><i class="icon-down-dir"></i>Coverable</th><th class="right"><i class="icon-down-dir"></i>Total</th><th colspan="2" class="center"><i class="icon-down-dir"></i>Percentage</th><th class="right"><i class="icon-down-dir"></i>Covered</th><th class="right"><i class="icon-down-dir"></i>Total</th><th colspan="2" class="center"><i class="icon-down-dir"></i>Percentage</th></tr></thead><tbody><tr codeelement-row=""><th><i class="icon-minus"></i> algorithms</th><th class="right">600</th><th class="right">55</th><th class="right">655</th><th class="right">1382</th><th class="right" title="600/655">91.6%</th><th class="right"><coverage-bar><table class="coverage"><td class="covered92 green"></td><td class="covered8 red"></td></table></coverage-bar></th><th class="right">167</th><th class="right">195</th><th class="right" title="167/195">85.6%</th><th class="right"><coverage-bar><table class="coverage"><td class="covered86 green"></td><td class="covered14 red"></td></table></coverage-bar></th></tr><tr class-row=""><td>Tiracompress.Algorithms.Huffman</td><td class="right"> 320 </td><td class="right"> 45 </td><td class="right"> 365 </td><td class="right"> 763 </td><td class="right" title="320/365"> 87.6% </td><td class="right"><coverage-bar><table class="coverage"><td class="covered88 green"></td><td class="covered12 red"></td></table></coverage-bar></td><td class="right"> 92 </td><td class="right"> 115 </td><td class="right" title="92/115"> 80% </td><td class="right"><coverage-bar><table class="coverage"><td class="covered80 green"></td><td class="covered20 red"></td></table></coverage-bar></td></tr><tr class-row=""><td>Tiracompress.Algorithms.Lz77</td><td class="right"> 280 </td><td class="right"> 10 </td><td class="right"> 290 </td><td class="right"> 619 </td><td class="right" title="280/290"> 96.5% </td><td class="right"><coverage-bar><table class="coverage"><td class="covered97 green"></td><td class="covered3 red"></td></table></coverage-bar></td><td class="right"> 75 </td><td class="right"> 80 </td><td class="right" title="75/80"> 93.7% </td><td class="right"><coverage-bar><table class="coverage"><td class="covered94 green"></td><td class="covered6 red"></td></table></coverage-bar></td></tr></tbody></table></div>

## Suorituskykytestaus

Ohjelmistoa ja pakkausalgoritmien toteutuksen tehokkuutta ei vielä ole testattu laajalla aineistolla, lähinnä manuaalitestaus on keskittynyt testaamaan että toteutetut algoritmit tuottavat identtisen lopputuleman lähdeaineistoon roundtrip-testatessa pakkausta ja syntyneen tiedoston takaisin purkua.

