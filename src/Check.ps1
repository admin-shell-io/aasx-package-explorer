<#
This script builds the solution, runs Resharper InspectCode and dotnet-format to
check the quality of the code base.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet, `
    AssertDotnetFormatVersion, `
    FindMSBuild, `
    FindInspectCode

##
# Setup
##

AssertDotnet
AssertDotnetFormatVersion

$msbuild = FindMSBuild
$inspectcode = FindInspectCode

##
# Check
##

cd $PSScriptRoot

$fails = @()

##
# dotnet-format
##

Write-Host "Inspecting the code format with dotnet-format..."

$reportPath = Join-Path $PSScriptRoot "dotnet-format-report.json"
dotnet format --dry-run --report $reportPath
$formatReport = Get-Content $reportPath |ConvertFrom-Json
if($formatReport.Count -ge 1) {
    $fails += "* There are $($formatReport.Count) dotnet-format issue(s). " + `
        "The report is stored in: $reportPath"
}

##
# Build
##

Write-Host "Buiding with MSBuild: $msbuild"
& $msbuild --% /p:Configuration=Debug /p:Platform=x64 
if(!$?) {
    throw "Build failed."
}

##
# inspectcode.exe
##

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
    $fails += "* There are $issueCount InspectCode issue(s). " + `
        "The issues are stored in: $PSScriptRoot\resharper-code-inspection.xml"
}

if($fails.Count -ne 0) {
    $parts = @("Checks failed:") + $fails
    Write-Error ($parts -join "`r`n") -ErrorAction Stop
}
