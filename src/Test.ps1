<# 
This script runs all the unit tests specified in `.\tests.nunit` file.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    FindNunit3Console, `
    FindOpenCoverConsole, `
    CreateAndGetArtefactsDir, `
    FindReportGenerator

$nunit3Console = FindNunit3Console
$openCoverConsole = FindOpenCoverConsole
$reportGenerator = FindReportGenerator

cd $PSScriptRoot
$nunitProjectPath = Join-Path $PSScriptRoot "tests.nunit"

Write-Host "Running the tests specified in: $nunitProjectPath"

$artefactsDir = CreateAndGetArtefactsDir

$testResultsPath = Join-Path $artefactsDir "TestResults.xml"
$coverageResultsPath = Join-Path $artefactsDir "CoverageResults.xml"

& $openCoverConsole `
    -target:$nunit3Console `
    -targetargs:("--noheader --shadowcopy=false " + `
        "--result=$testResultsPath $nunitProjectPath") `
    -targetdir:$(Join-Path $artefactsDir "\build\Debug") `
    -output:$coverageResultsPath `
    -register:user `
    -filter:"+[Aasx*]*"

if(!$?) {
    throw "The unit test(s) failed."
}

# Scripts are expected at the root of src/
$srcDir = $PSScriptRoot

$coverageReportPath = Join-Path $artefactsDir "CoverageReport"
& $reportGenerator `
    -reports:$coverageResultsPath `
    -targetdir:$coverageReportPath `
    -sourcedirs:$srcDir