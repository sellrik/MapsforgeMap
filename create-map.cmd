echo ########## Converting pbf to osm ##########
call lib\osmosis\bin\osmosis.bat --read-pbf data\hungary-latest.osm.pbf --write-xml data\map.osm

echo ########## Copying "jel" tags to ways ##########
lib\transform\Transform.exe --source data\map.osm --target data\map_hiking_way.osm

echo ########## Creating "jel" symbol nodes ##########
lib\transform\Transform.exe --source data\map.osm --target data\map_hiking_symbol.osm --createTagNodes

echo ########## Merging files ##########
call lib\osmosis\bin\osmosis.bat --rx data\map_hiking_way.osm --rx data\map_hiking_symbol.osm --rx data\out_file_lon16.11_22.90lat45.73_48.59_srtm1v3.0.osm --merge --merge --wx data\map_hiking_srtm.osm

echo ########## Creating map ##########
call lib\osmosis\bin\osmosis.bat --read-xml data\map_hiking_srtm.osm --mw file=data\hungary.map tag-conf-file=tag-mapping_modified.xml