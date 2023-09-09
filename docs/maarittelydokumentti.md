# Määrittelydokumentti

Tämä dokumentti määrittelee Helsingin Yliopiston Tietojenkäsittelytieteen kandiohjelman syksyn 2023 1. periodin Aineopintojen harjoitustyö: Algoritmit ja tekoäly -kurssin laboratoriotyötoteutuksen.

## Aihe

Laboratoriotyön aiheena on tiedon tiivistys-/pakkausalgoritmit. Käytännön toteutuksena on konsolissa/terminaalissa ajettava sovellus, joka toteuttaa valitut häviöttämät pakkausalgoritmit ja tekee niiden perusteella tiedon tiivistystä sekä tiivistetyn tiedon palauttamisen. Tarkoituksena on toteuttaa ja vertailla tiivistysalgoritmeja **Lempel-Ziv 77** (LZ77), **Huffman koodaus** sekä näiden yhdistelmää **Deflate**. Algoritmit valittiin niiden suhteellisen matalan toteuttamiskompleksisuudesta käytettävissä olevan kurssinn läpivientiajan puitteissa. Lisäksi koska Deflate rakentuu LZ77 ja Huffman koodauksien päälle, voidaan se toteuttaa vähäisellä vaivalla käyttäen näiden toteutuksia.

 Toteutettavalle sovellukselle voidaan antaa syötteenä seuraavat parametrit:

- Polku levyllä olevaan tiedostoon joka toimii tiivistyksen/purkamisen lähdedatana
- Tiedostopolku tiedostoon joka luodaan toiminnon seurauksena, jonka sisältö on joko pakattu tai purettu versio syötetiedostosta riippuen toimintatilasta.
- Toimintatila-lippu siitä pakataanko vai puretaanko syötetiedoston sisältö
- Algoritmi-lippu jolla voidaan määritellä joku kolmesta käytettävästä algoritmista

Osana sovelluksen suoritusta annettu tiedosto joko pakataan tai puretaan valitun toimintatilan mukaisesti, käyttäen valittua algoritmia. Ohjelma aina ylikirjoittaa tiedoston levyllä joka on määritely luotavaksi toiminnon seurauksena.

Sovelluksen ajon aikana sovellus tulostaa konsolille tilatietoja ja statistiikkaa pakkaus- tai purkutoimenpiteen lopputuloksesta. Tulosteen perusteella voidaan todeta eri algoritmien tilatehokkuus sekä suoritukseen kuluvan ajann käyttö. Sovelluksessa pyritään käyttämään valitun ohjelmointikielen sekä sen standardikirjaston tarjoamia tietorakenteita pakkausalgoritmien toteutuksessa.

## Ohjelmointikielet

Laboratoriotyö toteutetaan C# kielellä (versio: Microsoft .NET 7, [ajoympäristö .NET Runtime ladattavissa eri käyttäjärjestelmille täältä](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)).

Yksikkötestausta ja koodikattavuutta varten käytetään myöhemmin toteutusvaiheessa valittavaa .NET 7 projekteja tukevia vakiotyökaluja.

Vertaisarviointeja varten ohjelmointikielen osaamista on myös Pythonista, sekä tarvittaessa myös muista yleisistä ohjelmointikielistä niin että toteutuksen seuraaminen onnistuu.

## Algoritmit

Ensisijaisina lähteinä algoritmien toteutuksessa käytetään seuraavia sivustoja:

LZ77: [Wikipedia, LZ77 and LZ78](https://en.wikipedia.org/wiki/LZ77_and_LZ78)  
Huffman: [GeeksForGeeks.org, Huffman Coding](https://www.geeksforgeeks.org/huffman-coding-greedy-algo-3/)  
Deflate: [RFC 1951](https://datatracker.ietf.org/doc/html/rfc1951)  

Lisäksi toteutuksessa hyödynnetään tarvittaessa muita jullkisesti saatavilla olevia sivustoja ja dokumentteja, joissa edellämainittujen algoritmien toteutusta on kuvattu.

## Dokumentaatio

Kaikki dokumentaatio liittyen laboratorityöhön on kirjoitettu suomeksi. Itse ohjelmassa yleisten käytäntöjen mukaisesti muuttujien nimeäminen ym. ohjelmakoodiin liittyvät rakenteet ovat englanniksi, mutta koodikommentointi suomeksi.

