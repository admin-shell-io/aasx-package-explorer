@echo off
echo WARNING!
echo ========
echo If you go ahead, this batch file will copy LICENSE.TXT to all dependent locations. Proceed?
pause
@echo on
echo 
copy LICENSE.TXT .\AasxCsharpLibrary\.
copy LICENSE.TXT .\AasxGenerate\.
copy LICENSE.TXT .\AasxPackageExplorer\.
copy LICENSE.TXT .\AasxRestConsoleServer\.
copy LICENSE.TXT .\AasxWpfControlLibrary\.
copy LICENSE.TXT .\AasxWpfControlLibrary\Resources\.
