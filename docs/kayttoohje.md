# Käyttöohje

## Esivaatimukset

Sovellus on toteutettu Microsoftin .NET 7 ohjelmointikirjastolla käyttäen C# kieltä. Vaikka sovelluksen ajamiseen tarvitaan koneelle asennetuksi minimissään vain .NET 7 Runtime -ajoympäristö, edellyttää se että sovelluksesta on tehty jo binäärikäännös .NET IL koodiksi.

Koska repositoryn omalle koneelle kloonamisen jälkeen käytettävissä on vain lähdekoodimuotoinen toteutus, ja koska koodikattavuuden raportin luomiseksi tulee ajaa testaus lähdekoodeja vastaan, on syytä asentaa koneelle suoraan .NET 7 SDK paketti. SDK on asennettavissa Windows, macOS ja Linux -käyttöjärjestelmille.

.NET ajoympäristöt ja SDK (joka jo sisältää runtimen) on ladattavissa Microsoftin sivuilta täältä: https://dotnet.microsoft.com/en-us/download/dotnet/7.0 (tällä hetkellä saatavilla on versio 7.0.401)

## Valmistelevat toimenpiteet

SDK:n asennuksen jälkeen tulee suorittaa kertaluontoinen toimenpide koneelle kloonatun tai kopioidun repositoryn */src/ -hakemistossa*. Aja seuraavat komennot terminaalissa */src/* hakemistossa (**dotnet** -komento on .NET ajoympäristön/SDK:n komentotyökalu):

```
dotnet tool restore
```

Tämä komento palauttaa sovellukseen määritellyn (**dotnet-tools.json** määritystiedosto *.config* -alihakemistossa) dotnet-reportgenerator-globaltool -työkalun käyttövalmiiksi.

## Sovelluksen ajo

Sovellus on ajettavissa SDK:n alaisuudessa suoraan pääohjelman projektin kautta, joka on */src/program/* hakemistossa oleva **tiracompress.csproj**. Mikäli sovellus olisi käännetty binäärimuotoon, olisi kyseinen binääritiedosto myös ajettavissa hieman toisenlaisen dotnet -syntaksin avulla.

Alla olevissa suoritusesimerkeissä käytetään SDK:n avulla suoraan yhdistettyä kääntämistä ja ajamista. Ilman eri mainitaa kaikki ao. komennot tulee suorittaa terminaalista */src/* hakemistossa.

**Käynnistää ohjelman ja tulostaa käytettävissä olevat komentoriviparametrit:**
```
dotnet run --project program/tiracompress.csproj
```

Annettaessa parametreja pitää dotnet -työkalun ja ajettavalle välitettävät parametrit erottaa toisistaan -- parametrilla, kuten alla olevassa esimerkissä.

**Käynnistää ohjelman ja pakkaa /tmp/silesia/dickens -tiedoston Huffman algoritmilla /tmp/dickens.huffman tiedostoksi:**
```
dotnet run --project program/tiracompress.csproj -- --input:/tmp/silesia/dickens --output:/tmp/dickens.huffman --mode:compress --algorithm:huffman
```

### Käytettävissä olevat parametrit
--input:*infile* = Ohjelmalle annettava syötetiedoston polku jonka sisältö käsitellään  
--output:*outfile* = Ohjelmalle annettava ulosvientitiedoston polku jonne käsitelty sisältö kirjoitetaan  
--mode:[compress|uncompress] = Pakataanko vai puretaanko syötetiedoston sisältö  
--algorithm:[huffman|lz77] = Käytettävä algoritmi (Huffman- ja LZ77-koodaukset toteutettuina)  

## Yksikkötestauksen ajo

Yksikkötestaukset ovat */src/tests/* -hakemistossa omana .NET projektinaan. Nämä voidaan ajaa seuraavalla komennolla:

```
dotnet test tests/tests.csproj
```

Testien ajaminen - kuten myös itse pääohjelman ajaminen - automaattisesti tekee koodin tyylitarkistuksen (Code quality analysis) käyttäen .NET sisäänrakennettua analysaattoria, joka on */src/* -hakemistossa olevan **.editorconfig** tiedoston avulla määritetty generoitumaan käännösvaroituksina.

## Koodikattavuusraportin luonti

Testauskattavuusraportti on ajettavissa kaksivaiheisesti.

Ensimmäisessä vaiheessa ajetaan testit siten, että tuloksena luodaan coverlet -kirjaston avulla XML-raportti kattavuudesta:

```
dotnet test tests/tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput="coverage.xml"
```

Tämän komennon ajaminen tuo jo perustasolla kattavuusstatistiikan tulosteena konsolille, mutta *tests* -alihakemisoon generoidun XML -tiedoston avulla voidaan luoda erillinen HTML muotoinen raportti käyttäen alussa mainittua dotnet-reportgenerator-globaltool -työkalua.

```
dotnet tool run reportgenerator -reports:tests/coverage.xml -targetdir:tests/coveragereport -reporttypes:Html 
```

Lopputulemana */src/tests/coveragereport/* -hakemistosta löytyy HTML muotoinen kattavuusraportti, joka on selattavissa hakemistossa olevan index.html tiedoston kautta.