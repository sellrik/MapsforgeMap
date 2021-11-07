REM Download data from Geofabrik. For example: https://download.geofabrik.de/europe/hungary.html -> source.osm.pbf
REM Convert .osm.pbf to .osm. Alternatively download .osm directly

REM lib\osmosis\bin\osmosis --read-pbf data\source.osm.pbf --write-xml data\source.osm

REM Budai-hegység: 18.89465,47.453804,19.10751,47.629478
REM -step 10 -> nem tűnik valósnak, mintha csak fel lenne osztva...
lib\Srtm2Osm\Srtm2Osm -bounds1 47.453804 18.89465 47.629478 19.10751 -merge data\source.osm -o data\srtm.osm -large -d data\srtm -cat 500 100

REM create maspforge map
osmosis --read-xml data\srtm.osm --mw file=data\target.map tag-conf-file=tag-mapping.xml bbox=47.453804,18.89465,47.629478,19.10751

pause