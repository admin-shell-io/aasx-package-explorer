@echo off
echo Prepare build packages
echo ======================
echo This script will prepare a new folder, which will contain copies of the source code (cleaned) and the release files
echo Please make sure that you have COMPILED the RELEASE configuration!!
set /p id="Enter 6+ digit day code for build folder (e.g. yymmdd): "
set bp=..\build%id%\
echo build path prefix is: %bp%
rmdir /s /q ..\build%id%

echo Making directories

mkdir %bp%
mkdir %bp%\sources
mkdir %bp%\eclass
mkdir %bp%\AasxPackageExplorer
mkdir %bp%\plugins-restricted
mkdir %bp%\plugins-open
mkdir %bp%\plugins-webbrowser
mkdir %bp%\sample-aasx

dir %bp%
pause

echo Copy main
xcopy /s/e .\AasxPackageExplorer\bin\x64\Release %bp%\AasxPackageExplorer
rmdir /s/q %bp%\AasxPackageExplorer\eclass
rmdir /s/q %bp%\AasxPackageExplorer\plugins

pause

copy .\AasxPackageExplorer\LICENSE.txt %bp%\.
copy .\AasxPackageExplorer\LICENSE.txt %bp%\AasxPackageExplorer\.

copy .\README-packages.md %bp%\.

echo Copy plugins-open
for %%a in (
	AasxPluginBomStructure
	AasxPluginDocumentShelf
	AasxPluginExportTable
	AasxPluginGenericForms
	AasxPluginTechnicalData) do (
	echo Copy plugin %%a
	xcopy /s/e .\%%a\bin\Release\*.* %bp%\plugins-open\%%a\
)

echo Copy plugins-webbrowser
for %%a in (
	AasxPluginWebBrowser) do (
	echo Prepare x64 plugin %%a
	xcopy /s/e .\%%a\bin\x64\Release\*.* %bp%\plugins-webbrowser\%%a\
)

echo Copy plugins-restricted
xcopy /s/e ..\AasxMtpPlugin\AasxPluginMtpViewer\bin\Release\*.* %bp%\plugins-restricted\AasxPluginMtpViewer\
xcopy /s/e ..\AasxUaNetServerDevelop\Net46AasxServerPlugin\bin\Release\*.* %bp%\plugins-restricted\Net46AasxServerPlugin\
xcopy /s/e ..\AasxRestrictedPlugins\AasxPluginOpcUaClient\bin\Release\*.* %bp%\plugins-restricted\AasxPluginOpcUaClient\

echo Copy sample-aasx
xcopy ..\AasxSampleData\Portable-samples\* %bp%\sample-aasx

echo Copy eClass

xcopy /s/e .\AasxPackageExplorer\eclass %bp%\eclass

pause

rem Copy binaries together into different packages

echo Prepare portable-restricted-eclass

mkdir %bp%\portable-restricted-eclass
xcopy /s/e %bp%\AasxPackageExplorer\*.* %bp%\portable-restricted-eclass\AasxPackageExplorer\
xcopy /s/e %bp%\plugins-open\*.* %bp%\portable-restricted-eclass\AasxPackageExplorer\plugins\
xcopy /s/e %bp%\plugins-webbrowser\*.* %bp%\portable-restricted-eclass\AasxPackageExplorer\plugins\
xcopy /s/e %bp%\plugins-restricted\*.* %bp%\portable-restricted-eclass\AasxPackageExplorer\plugins\
xcopy /s/e %bp%\eclass\*.* %bp%\portable-restricted-eclass\AasxPackageExplorer\eclass\
xcopy /s/e %bp%\sample-aasx\*.* %bp%\portable-restricted-eclass\sample-aasx\

echo Prepare portable-restricted

mkdir %bp%\portable-restricted
xcopy /s/e %bp%\AasxPackageExplorer\*.* %bp%\portable-restricted\AasxPackageExplorer\
xcopy /s/e %bp%\plugins-open\*.* %bp%\portable-restricted\AasxPackageExplorer\plugins\
xcopy /s/e %bp%\plugins-webbrowser\*.* %bp%\portable-restricted\AasxPackageExplorer\plugins\
xcopy /s/e %bp%\plugins-restricted\*.* %bp%\portable-restricted\AasxPackageExplorer\plugins\
xcopy /s/e %bp%\sample-aasx\*.* %bp%\portable-restricted\sample-aasx\

echo Prepare portable-open

mkdir %bp%\portable-open
xcopy /s/e %bp%\AasxPackageExplorer\*.* %bp%\portable-open\AasxPackageExplorer\
xcopy /s/e %bp%\plugins-open\*.* %bp%\portable-open\AasxPackageExplorer\plugins\
xcopy /s/e %bp%\plugins-webbrowser\*.* %bp%\portable-open\AasxPackageExplorer\plugins\
xcopy /s/e %bp%\sample-aasx\*.* %bp%\portable-open\sample-aasx\

echo Prepare portable-small

mkdir %bp%\portable-small
xcopy /s/e %bp%\AasxPackageExplorer\*.* %bp%\portable-small\AasxPackageExplorer\
xcopy /s/e %bp%\plugins-open\*.* %bp%\portable-small\AasxPackageExplorer\plugins\
xcopy /s/e %bp%\sample-aasx\*.* %bp%\portable-small\sample-aasx\

pause

rem Copy source files

echo Copy source files

for %%a in (
	AasxAmlImExport 
	AasxCsharpLibrary 
	AasxGenerate 
	AasxIntegrationBase 
	AasxIntegrationBaseWpf 
	AasxIntegrationEmptySample 
	AasxMqtt 
	AasxMqttClient 
	AasxPackageExplorer
	AasxPluginBomStructure
	AasxPluginDocumentShelf
	AasxPluginExportTable
	AasxPluginGenericForms
	AasxPluginTechnicalData
	AasxPredefinedConcepts
	AasxRestConsoleServer
	AasxRestServerLibrary
	AasxWpfControlLibrary
	MsaglWpfControl) do (
	
	echo Archive source for %%a
	xcopy /s/e .\%%a %bp%\sources\%%a\
	rmdir /s/q %bp%\sources\%%a\bin
	rmdir /s/q %bp%\sources\%%a\obj
	rem because of AasxGenerate
	rmdir /s/q %bp%\sources\%%a\data
	rem because of eClass
	rmdir /s/q %bp%\sources\%%a\eclass
)

echo Source files archived

rem Zipping


for %%a in (
	plugins-restricted
	plugins-open
	plugins-webbrowser
	portable-restricted-eclass
	portable-restricted
	portable-open
	portable-small
	sources) do (
	
	echo Zipping %%a
	"C:\Program Files\7-Zip\7z.exe" a -r %bp%\%%a.zip %bp%\%%a
)

echo DONE
