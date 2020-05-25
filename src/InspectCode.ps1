<#
This script inspects the code quality.

It should be run after the build.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    FindInspectCode

$inspectcode = FindInspectCode

Write-Host "Inspecting the code with inspectcode.exe ..."

# InspectCode passes over the properties to MSBuild,
# see https://www.jetbrains.com/help/resharper/InspectCode.html#msbuild-related-parameters
& $inspectcode `
    --properties:Configuration=Debug `
    --properties:Platform=x64 `
    "-o=resharper-code-inspection.xml" `
    AasxPackageExplorer.sln
 
[xml]$inspection = Get-Content "resharper-code-inspection.xml"
$issueCount = $inspection.SelectNodes('//Issue').Count
if( $issueCount -ne 0 ) {
    throw "There are $issueCount InspectCode issue(s). " + `
        "The issues are stored in: $PSScriptRoot\resharper-code-inspection.xml"
}
