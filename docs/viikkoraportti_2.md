# Viikkoraportti, viikko 2

Laboratoriotyön varsinainen koodaus alkoi tällä viikolla, ja aloitusvaiheessa aikaa meni hieman ylimääräistä sekä yksikkötestauksen, tyylitarkistukseen että koodikattavuuteen liittyvien käytänteiden/työkalujen selvittämiseen valitun ohjelmointikielen osalta. Näiden osalta kuitenkin löytyi nopeasti varsin standardinmukaisesti .NET:n kanssa käytetyt xUnit kirjasto, sisäänrakennettu Code quality analysis -toiminto, coverlet kirjasto sekä reportgenerator dotnet -työkalu coverlet tulosteen konvertoimiseen HTML muotoiseksi raportiksi. Lisäksi ohjelmiston optimaalisen toteutusrakenteen (pääohjelma, algoritmit toteuttava moduuli, testausmoduuli) muodostamiseen meni hieman ylimääräistä aikaa suhteessa jatkoon.

Koodattavan ratkaisun osalta tällä viikolla on toteutettu itse kutsuttava pääohjelma, jonka avulla voidaan valita käytettävä tiivistysalgoritmi sekä määritellä syöte- ja ulostulotiedostot. Toteutettavista algoritmeista ensimmäisenä on koodattu perustason Huffman-koodaus käyttäen 2-pass menetelmää syötetiedoston prosessointiin, ensimmäisellä lukukerralla symbolien eli tavujen esiintymistiheyksien laskeminen ja toisella lukukerralla varsinainen tiedon tiivistys tiheyksistä lasketun koodaustaulukon avulla ulostulotiedostoon. Purkaessa riittää tiedon kertaalleen luku, koska silloin riittää hyödyntää jo olemassaolevaa puuta joka kulkee koodatun tiedon mukana.

Itse Huffman algoritmin toteutuksen osalta aikaa meni hieman ymmärtää itse koodausprosessi etenkin Huffman-puun muodostuksen yksityiskohtien osalta, mutta koska netistä löytyi useampikin esimerkki siitä vaiheistettuna, avautui logiikka lopulta jonka jälkeen itse ohjelmallinen toteutus oli varsin nopea rakentaa. Yksi spesifi kompastuskohta muutoin jo toimivaa koodausta testatessa oli havainto, että takaisinpurettu tekstitiedosto sisälsi odottamattomasti muutamia korruptoituneita pätkiä, ja tarkempi selvittely osoitti että tietyissä tilanteissa Huffman koodauksen tuottama bittijono - eli puun syvyys - yhtä symbolia kohten saattoi ylittää 16 bitin pituuden, mikä aluksi oli koodissa varattu maksimipituudeksi. Kun tämä laajennettiin tukemaan aina 32-bitin pituisia koodeja, poistui nämä korruptiot.

Purdue -yliopiston sivuilta löytyi myös sovellettava kuvaus siitä, miten varsinaisen koodatun tiedoston lisäksi itse koodaukseen käytettävä tieto (eli Huffman-puu) kannattaa tallentaa (https://engineering.purdue.edu/ece264/17au/hw/HW13?alt=huffman) tiedoston osaksi, jotta purkaminen voidaan suorittaa pelkästään ko. tiedoston sisältämien tietojen perusteella. Tämän koodauksen esitystapaa olisi mahdollista optimoida tilallisesti, nyt jokaisen solmun merkitsemiseen tuhlataan yksi tavu kun sen optimaalisesti tarvitsisi olla vain kaksi bittiä (lehtisolmu, välisolmu ja lopetusmerkki).

Toteutuksen osalta on pyritty tekemään ennen kaikkea ymmärrettävä koodi jakamalla eri Huffman -pakkauksen vaiheet loogisesti omiksi metodeikseen, sekä välttämään monimutkaisia rakenteita, vaikka toteutuksen varmasti voisi tehdä optimaalisemminkin ja/tai yhdistelemällä eri vaiheita yhdeksi.

Toteutusta on testattu koneelta löytyvillä satunnaisilla tiedostoilla, sekä osalla Silesia corpus -datasetista (https://github.com/MiloszKrajewski/SilesiaCorpus) jota yleisesti käytetään tiedon tiivistämiseen käytettävien algoritmien testaamiseen. Ohjelma on koodaukseen ja testaukseen käytetyllä M2 -prosessorilla varustetulla MacBook Air kannettavalla varsin nopea ottaen huomioon että algoritmia ei ole erityisesti optimoitu; koodaten Silesia corpuksesta löytyvän 10 megatavun tekstitiedoston (dickens) n. 0,3 sekunnissa ja 40 megatavun tekstitiedoston (webster) n. 1 sekunnissa.


## Tuntikirjanpito

| Päivä | Käytetty aika | Kuvaus |
| ----- | ------------- | ------ |
| 10.9.  | 3h            | Projektin tiedostorakenteen alustus ja testi-tooling |
| 11.9.  | 4h            | Huffman algoritmin toteutus ja testien lisääminen |
| 12.9.  | 3h            | Huffman algoritmin toteutuksen jatko |
| 13.9.  | 3h            | Purkutoiminnot ja dokumentointi |
| 14.9.  | 2h            | Testien täydentäminen, dokumentointi |
| Yhteensä | 15h         |        |