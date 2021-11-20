Környezet előkészítése, telepítés: TODO
1. Osmosis: https://github.com/mapsforge/mapsforge/blob/master/docs/Getting-Started-Map-Writer.md
2. phyghtmap: http://katze.tfiu.de/projects/phyghtmap/#Download

Térkép létrehozás folyamat:
1. Osm adatok letöltése innen (osm.pmf): </br>
https://download.geofabrik.de/europe/hungary.html </br>

2. A tesztelés miatt a Budai-hegység terültére szűrés: </br>
(Budai-hegység területe: 18.89465,47.453804,19.10751,47.629478) </br>
osmosis --read-pbf data\hungary-latest.osm.pbf --bounding-box top=47.629478 left=18.89465 bottom=47.453804 right=19.10751 --write-xml data\budaihegyseg.osm </br>

3. Szintvonalak létrehozása: </br>
phyghtmap -a 18.89465:47.453804:19.10751:47.629478 -o out_file --write-timestamp --max-nodes-per-tile=0 --max-nodes-per-way=200 --source=srtm1 --srtm-version=3.0 </br>

4. A két adathalmaz egyesítése (térkép + szintvonal): </br>
osmosis --rx data/budaihegyseg.osm --rx data/out_file_lon18.89_19.11lat47.45_47.63_srtm1v3.0.osm --merge --wx data\budaihegyseg_srtm.osm </br>

5. Turistajelzések létrehozása: </br>
Azt vettem észre/úgy tudom, hogy a mapsforge nem támogatja a relation-öket. Ezért írtam egy programot (Transform almappa), ami a "jel" tag-eket átmásolja a way-ekre a relation-ökröl. </br>

lib\transform\Transform.exe --source data\budaihegyseg_srtm.osm --target data\budaihegyseg_srtm_hiking.osm </br>
A budaihegyseg_srtm.osm fájlből keletkezik a budaihegyseg_srtm_hiking.osm fájl, ami tartalmazza az átmásolt tag-eket. Sajnos nem túl gyors...</br>

6. Térkép elkészítése: </br>
osmosis --read-xml data\budaihegyseg_srtm_hiking.osm --mw file=data\budaihegyseg.map tag-conf-file=tag-mapping.xml bbox=47.453804,18.89465,47.629478,19.10751 </br>
A tag-mapping.xml fájl tartalmazza a térkép által támogatott elemeket.

Egész Magyarországra:
osmosis --read-pbf data\hungary-latest.osm.pbf --write-xml data\hungary.osm </br>

phyghtmap -a 16.11262:45.73218:22.90201:48.58766 -o out_file --write-timestamp --max-nodes-per-tile=0 --max-nodes-per-way=200 --start-node-id=10000000000 --start-way-id=10000000000 --source=srtm1 --srtm-version=3.0 </br>

lib\transform\Transform.exe --source data\hungary.osm --target data\hungary_hiking.osm </br>

osmosis --rx data/hungary_hiking.osm --rx data/out_file_lon16.11_22.90lat45.73_48.59_srtm1v3.0.osm --merge --wx data\hungary_hiking_srtm.osm </br>

osmosis --read-xml data\hungary_hiking_srtm.osm --mw file=data\hungary.map tag-conf-file=tag-mapping.xml </br>

Telepítés folyamat:
1. A térképfájl felmásolása a telefonon az alábbi mappába </br>
\Locus\mapsVector
2. Sablon (themes\test.xml és themes\symbol mappa) felmásolása az alábbi mappába: </br>
\Locus\mapsVector\_themes\test\t

Alternatív Sablon telepítés: </br>
A telefonon a linkre kattintva a locus automatikusan telepíti a témát: </br>
[Téma telepítése](https://raw.githubusercontent.com/sellrik/MapsforgeMap/master/theme_locus_action.xml)
