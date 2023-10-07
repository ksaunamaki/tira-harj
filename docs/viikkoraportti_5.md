# Viikkoraportti, viikko 5

Vaikka tälläkin viikolla muut työt ovat antaneet vähemmän aikaa edistää projektia eteenpäin, Deflate algoritmin toteutuksen osalta ennakoimattoman paljon aikaa on mennyt algoritmin yksityiskohtien ymmärtämiseen. Tämä sen johdosta, että algoritmin netistä löytyvistä kuvauksissa osa Deflaten yksityiskohdista on selitetty varsin epämääräisellä tai ylimalkaisella tavalla, eikä niistä ole kunnolla käynyt ilmi miten koodauksen tuottama bittivirta tarkalleen ottaen pitäisi konstruoida. Hyvinä asiaa "rautalangasta vääntävinä" artikkeleina on löytynyt mm. [The Elegance of Deflate](http://www.codersnotes.com/notes/elegance-of-deflate/), [Understanding zlib](https://www.euccas.me/zlib/) sekä [ZLIB + Deflate file format](https://calmarius.net/index.php?lang=en&page=programming/zlib_deflate_quick_reference).

Artikkeleita lukiessa on selvinnyt, että jo toteutetut Huffman ja LZ77 luokat eivät välttämättä suoraan sovellu Deflate -algoritmin toteuttamiseksi kokonaisuudessaan, mutta todennäköisesti niiden sisältämiä alemman tason metodeita voi käyttää osana muokattua toteutusta Deflate luokassa. Ensimmäinen vaihe toteutuksesta, eli sisäänluettavien blokkien koodaus symboleiksi on luotu luokkaan muokattuna versiona aiemmasta LZ77 luokan vastaavasta.


## Tuntikirjanpito

| Päivä | Käytetty aika | Kuvaus |
| ----- | ------------- | ------ |
| 5.10. |   1h       | Tutustuminen Deflate-algoritmiin  |
| 6.10. |   2h       | Vertaisarviointi, deflate-algoritmin tutkiminen  |
| 7.10. |   3h       | Deflate toteutusta, viikkoraportti | 
| Yhteensä | 6h       |        |