REM C:\Users\Balazs\Downloads\platform-tools\adb.exe connect 192.168.0.100:43767

C:\Users\Balazs\Downloads\platform-tools\adb.exe push data/hungary.map /sdcard/Locus/mapsVector/hungary.map

C:\Users\Balazs\Downloads\platform-tools\adb.exe push themes/ /sdcard/Locus/mapsVector/_themes/test/

C:\Users\Balazs\Downloads\platform-tools\adb.exe shell am force-stop menion.android.locus
C:\Users\Balazs\Downloads\platform-tools\adb.exe shell am start -n menion.android.locus/com.asamm.locus.basic.features.mainActivity.MainActivityMap

REM C:\Users\Balazs\Downloads\platform-tools\adb.exe shell pm path menion.android.locus
REM C:\Users\Balazs\Downloads\platform-tools\adb.exe pull /data/app/~~RTW18tnpoDIinzLJpV9jJQ==/menion.android.locus-l8AQThZ65XfKi0I68MhnVQ==/base.apk