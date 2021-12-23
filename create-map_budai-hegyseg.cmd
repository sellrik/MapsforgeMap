echo Converting pbf to osm 
call lib\osmosis\bin\osmosis.bat --read-pbf data\hungary-latest.osm.pbf --write-xml data\map.osm
REM call lib\osmosis\bin\osmosis.bat --read-pbf data\hungary-latest.osm.pbf --bounding-box top=47.629478 left=18.89465 bottom=47.453804 right=19.10751 --write-xml data\map.osm

echo ########## Creating trailmarks ##########
lib\transform\Transform.exe --source data\map.osm --target data\map_trailmarks.osm

echo Merging files
call lib\osmosis\bin\osmosis.bat --rx data/map.osm --rx data/out_file_lon16.11_22.90lat45.73_48.59_srtm1v3.0.osm --merge --wx data\map_srtm.osm 
call lib\osmosis\bin\osmosis.bat --rx data/map_srtm.osm --rx data/map_trailmarks.osm --merge --wx data\map_srtm_trailmarks.osm

echo Creating map
call lib\osmosis\bin\osmosis.bat --read-xml data\map_srtm_trailmarks.osm --mw file=data\budai-hegyseg.map bbox=47.453804,18.89465,47.629478,19.10751 tag-conf-file=tag-mapping_modified.xml