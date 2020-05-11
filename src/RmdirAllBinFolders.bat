@echo off
echo WARNING!
echo ========
echo If you go ahead, this batch file will remove all .\Bin\ folders from the underlying projects. This is reduced for maximum cleanup, e.g. for preparing a deployment
pause
@echo on
rmdir /s/q .\AasxCsharpLibrary\bin
rmdir /s/q .\AasxCsharpLibrary\obj
rmdir /s/q .\AasxGenerate\bin
rmdir /s/q .\AasxGenerate\obj
rmdir /s/q .\AasxWpfControlLibrary\bin
rmdir /s/q .\AasxWpfControlLibrary\obj
rmdir /s/q .\AasxPackageExplorer\bin
rmdir /s/q .\AasxPackageExplorer\obj
@pause