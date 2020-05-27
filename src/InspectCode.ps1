<#
This script inspects the code quality.

It should be run after the build.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    FindInspectCode, `
    CreateAndGetArtefactsDir

$inspectcode = FindInspectCode

$artefactsDir = CreateAndGetArtefactsDir
$codeInspectionPath = Join-Path $artefactsDir "resharper-code-inspection.xml"

Write-Host "Inspecting the code with inspectcode.exe ..."

# InspectCode passes over the properties to MSBuild,
# see https://www.jetbrains.com/help/resharper/InspectCode.html#msbuild-related-parameters
& $inspectcode `
    --properties:Configuration=Debug `
    --properties:Platform=x64 `
    "-o=$codeInspectionPath" `
    AasxPackageExplorer.sln
 

[xml]$inspection = Get-Content $codeInspectionPath
$issueCount = $inspection.SelectNodes('//Issue').Count
if( $issueCount -ne 0 ) {
    throw "There are $issueCount InspectCode issue(s). " + `
        "The issues are stored in: $codeInspectionPath"
}
