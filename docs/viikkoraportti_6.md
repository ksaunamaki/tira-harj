# Viikkoraportti, viikko 6

Tällä viikolla tarkoituksena oli saada Deflate algoritmi toimimaan pakkauksen osalta, mutta käytännössä kaksi asiaa on osoittautunut erittäin haasteelliseksi ymmärtää kunnolla useammankin eri sivuston/kuvauksen lukemisen jälkeen: dynaamisen Huffman pakkausblokin otsaketiedojen eksakti luonne, sekä bittikoodauksen mekaniikka.  

Ensimmäisen kohdan osalta ongelmaksi on tullut ymmärtää yksikäsitteisesti kuinka Literal/Length koodisto sekä distance koodisto pitäisi tarkalleen ottaen "tuplapakata", ja mitä mikäkin eri Huffman koodisto joita speksissä kuvataan tarkalleen tarkoittavat; selitykset asiasta lähes kaikessa lähdemateriaalissa on varsin epähavainnollisesti, monimutkaisesti tai hyvin lyhyesti selitetty, jonka johdosta kokonaiskuvaa en vieläkään ole onnistunut muodostamaan siten että voisi kyseiset koodit luoda.  

Toisen kohdan osalta ongelmaksi on tullut se, että Deflate kuvauksessa puhutaan paikoitellen siitä, että bittiesitykset pitäisi koodata oikealta vasemmalle tavuissa mutta epäselväksi on jäänyt pitääkö myös itse bittijonojen olla käännetty kun empiirinen havainto deflate -koodatusta datasta taas on, että vaikka tiedot "pakataan" tavuihin oikealta vasemmalle, niin itse bittijonot ovat ns. oikeinpäin eikä käännettyinä. Tämä kohta selkeästi vaatii vielä hieman tutkimista, ja nykyisen koodausmetodin sopeuttamista tähän pakkaamistapaan, koska nykyinen koodi pakkaa bitit vasemmalta oikealle.

Valitettavasti yhtäkään visuaalista esitystapaa käyttävää sivustoa ei ole vielä löytynyt, jossa kerrottaisiin vaihe vaiheelta *tavurakenteen näyttävien kuvien kera* miten koodaus tapahtuu ja miten eri vaiheiden tuottamat bitit pakataan ulosmenevään tavuvirtaan. Tämäntyyppinen sivusto olisi ollut erittäin tervetullut koodauksen ymmärtämiseen, koska pelkästään sanallisesti selitettynä bittivirtaan perustuva koodaus jossa on useita erillisiä elementtejä jää väistämättä paikoin hieman ambivalentiksi sen suhteen mikä on korrekti toimia.

## Tuntikirjanpito

| Päivä | Käytetty aika | Kuvaus |
| ----- | ------------- | ------ |
| 8.10. |   3h       | Deflate toteutusta  |
| 9.10. |   2h       | Deflate toteutusta  |
| 11.10. |  5h       | Deflate toteutusta  |
| 13.10. |  1h       | Vertaisarviointi  |
| 14.10. |  1,5h       | Deflate toteutusta  |
| Yhteensä | 12,5h       |        |