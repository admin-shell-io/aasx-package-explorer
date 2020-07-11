@echo off
echo Prepare build packages
echo ======================
echo This script will prepare a new folder, which will contain copies of the source code (cleaned) and the release files
echo
set /p id="Enter 6+ digit day code for build folder (e.g. yymmdd): "
set bp=..\build%id%\
echo build path prefix is: %bp%
rmdir /s/q %bp%
mkdir %bp%
mkdir %bp%\sources
mkdir %bp%\restricted-release-with-browser
mkdir %bp%\restricted-release-no-browser
mkdir %bp%\public-release-with-browser
mkdir %bp%\public-release-no-browser
mkdir %bp%\restricted-plugin-Net46AasxServerPlugin
mkdir %bp%\restricted-plugin-AasxPluginOpcUaClient



rem Copy files
rem xcopy /s/e .\AasxCsharpLibrary %bp%\sources\AasxCsharpLibrary\
rmdir /s/q %bp%\sources\AasxCsharpLibrary\bin
rmdir /s/q %bp%\sources\AasxCsharpLibrary\obj

rem xcopy /s/e .\AasxGenerate %bp%\sources\AasxGenerate\
rmdir /s/q %bp%\sources\AasxGenerate\bin
rmdir /s/q %bp%\sources\AasxGenerate\obj
rmdir /s/q %bp%\sources\AasxGenerate\data

rem xcopy /s/e .\AasxPackageExplorer %bp%\sources\AasxPackageExplorer\
rmdir /s/q %bp%\sources\AasxPackageExplorer\bin
rmdir /s/q %bp%\sources\AasxPackageExplorer\obj
rmdir /s/q %bp%\sources\AasxPackageExplorer\eclass

rem xcopy /s/e .\AasxWpfControlLibrary %bp%\sources\AasxWpfControlLibrary\
rmdir /s/q %bp%\sources\AasxWpfControlLibrary\bin
rmdir /s/q %bp%\sources\AasxWpfControlLibrary\obj

rem xcopy /s/e .\AasxWpfControlLibrary %bp%\sources\AasxAmlImExport\
rmdir /s/q %bp%\sources\AasxAmlImExport\bin
rmdir /s/q %bp%\sources\AasxAmlImExport\obj

rem xcopy /s/e .\AasxWpfControlLibrary %bp%\sources\AasxIntegrationBase\
rmdir /s/q %bp%\sources\AasxIntegrationBase\bin
rmdir /s/q %bp%\sources\AasxIntegrationBase\obj

rem xcopy /s/e .\AasxWpfControlLibrary %bp%\sources\AasxIntegrationEmptySample\
rmdir /s/q %bp%\sources\AasxIntegrationEmptySample\bin
rmdir /s/q %bp%\sources\AasxIntegrationEmptySample\obj

rem xcopy /s/e .\AasxWpfControlLibrary %bp%\sources\AasxRestConsoleServer\
rmdir /s/q %bp%\sources\AasxRestConsoleServer\bin
rmdir /s/q %bp%\sources\AasxRestConsoleServer\obj

rem xcopy /s/e .\AasxWpfControlLibrary %bp%\sources\AasxRestServerLibrary\
rmdir /s/q %bp%\sources\AasxRestServerLibrary\bin
rmdir /s/q %bp%\sources\AasxRestServerLibrary\obj

rem copy .\AasxPackageExplorer\LICENSE.txt %bp%\.



rem Zipping
rem xcopy *.sln %bp%\sources\
rem "C:\Program Files\7-Zip\7z.exe" a -r %bp%\sources.zip %bp%\sources

rem xcopy /s/e .\AasxPackageExplorer\bin\x64\Release %bp%\restricted-release-with-browser
rem "C:\Program Files\7-Zip\7z.exe" a -r %bp%\restricted-release-with-browser.zip %bp%\restricted-release-with-browser

rem xcopy /s/e .\AasxPackageExplorer\bin\x64\Release %bp%\public-release-with-browser
rem rmdir /s/q %bp%\public-release-with-browser\eclass
rem "C:\Program Files\7-Zip\7z.exe" a -r %bp%\public-release-with-browser.zip %bp%\public-release-with-browser

rem xcopy /s/e .\AasxPackageExplorer\bin\x64\ReleaseWithoutCEF %bp%\restricted-release-no-browser
rem "C:\Program Files\7-Zip\7z.exe" a -r %bp%\restricted-release-no-browser.zip %bp%\restricted-release-no-browser

rem xcopy /s/e .\AasxPackageExplorer\bin\x64\ReleaseWithoutCEF %bp%\public-release-no-browser
rem rmdir /s/q %bp%\public-release-no-browser\eclass
rem "C:\Program Files\7-Zip\7z.exe" a -r %bp%\public-release-no-browser.zip %bp%\public-release-no-browser

rem xcopy /s/e ..\AasxUaNetServerDevelop\Net46AasxServerPlugin\bin\Release\*.* %bp%\restricted-plugin-Net46AasxServerPlugin\.
rem "C:\Program Files\7-Zip\7z.exe" a -r %bp%\restricted-plugin-Net46AasxServerPlugin.zip %bp%\public-plugin-Net46AasxServerPlugin

rem xcopy /s/e ..\AasxRestrictedPlugins\AasxPluginOpcUaClient\bin\Release\*.* %bp%\restricted-plugin-AasxPluginOpcUaClient\.
rem "C:\Program Files\7-Zip\7z.exe" a -r %bp%\restricted-plugin-AasxPluginOpcUaClient.zip %bp%\restricted-plugin-AasxPluginOpcUaClient

rem Portable
mkdir %bp%\portable
mkdir %bp%\portable\AasxPackageExplorer
mkdir %bp%\portable\AasxPluginBomStructure
mkdir %bp%\portable\AasxPluginDocumentShelf
mkdir %bp%\portable\AasxPluginGenericForms
mkdir %bp%\portable\AasxPluginMtpViewer

xcopy /s/e .\AasxPackageExplorer\bin\x64\Release %bp%\portable\AasxPackageExplorer
rmdir /s/q %bp%\portable\AasxPackageExplorer\eclass
xcopy /s/e .\AasxPluginBomStructure\bin\Release %bp%\portable\AasxPluginBomStructure
xcopy /s/e .\AasxPluginDocumentShelf\bin\Release %bp%\portable\AasxPluginDocumentShelf
xcopy /s/e .\AasxPluginGenericForms\bin\Release %bp%\portable\AasxPluginGenericForms
xcopy /s/e ..\AasxMtpPlugin\AasxPluginMtpViewer\bin\Release %bp%\portable\AasxPluginMtpViewer

xcopy .\AasxPackageExplorer\bin\x64\Release\start-aasx-explorer-portable.bat %bp%\portable

xcopy ..\AasxSampleData\Portable-samples\* %bp%\portable

rem "C:\Program Files\7-Zip\7z.exe" a -r %bp%\portable.zip %bp%\portable