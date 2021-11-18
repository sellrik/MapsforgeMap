Térkép létrehozás folyamat:
1. Osm adatok letöltése innen (osm.pmf): </br>
https://download.geofabrik.de/europe/hungary.html
2. A tesztelés miatt a Budai-hegység terültére szűrés: </br>
(Budai-hegység területe: 18.89465,47.453804,19.10751,47.629478) </br>
osmosis --read-pbf data\hungary-latest.osm.pbf --bounding-box top=47.629478 left=18.89465 bottom=47.453804 right=19.10751 --write-xml data\budaihegyseg.osm
3. Szintvonalak létrehozása: </br>
phyghtmap -a 18.89465:47.453804:19.10751:47.629478 -o out_file --write-timestamp --max-nodes-per-tile=0 --max-nodes-per-way=200 --source=srtm1 --srtm-version=3.0
4. A két adathalmaz egyesítése (térkép + szintvonal): </br>
osmosis --rx data/budaihegyseg.osm --rx data/out_file_lon18.89_19.11lat47.45_47.63_srtm1v3.0.osm --merge --wx data\budaihegyseg_srtm.osm
5. Turistajelzések létrehozása: </br>
Azt vettem észre/úgy tudom, hogy a mapsforge nem támogatja a relation-öket. Ezért írtam egy programot (Transform almappa), ami a "jel" tag-eket átmásolja a way-ekre a relation-ökröl. </br>
Egyenlőre nem paraméterezhető szépen, ez még hátravan.
A budaihegyseg_srtm.osm fájlből keletkezik a budaihegyseg_srtm_hiking.osm fájl, ami tartalmazza az átmásolt tag-eket.
6. Térkép elkészítése: </br>
osmosis --read-xml data\budaihegyseg_srtm_hiking.osm --mw file=data\budaihegyseg.map tag-conf-file=tag-mapping.xml bbox=47.453804,18.89465,47.629478,19.10751 </br>
A tag-mapping.xml fájl tartalmazza a térkép által támogatott elemeket.

Telepítés folyamat:
1. A térképfájl felmásolása a telefonon az alábbi mappába </br>
\Internal shared storage\Locus\mapsVector
2. Sablon (themes\test.xml és themes\symbol mappa) felmásolása az alábbi mappába: </br>
\Internal shared storage\Locus\mapsVector\_themes\test
