<#
This script checks the format of the code.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet, `
    AssertDotnetFormatVersion


AssertDotnet
AssertDotnetFormatVersion

cd $PSScriptRoot
Write-Host "Inspecting the code format with dotnet-format..."

$reportPath = Join-Path $PSScriptRoot "dotnet-format-report.json"
dotnet format --dry-run --report $reportPath
$formatReport = Get-Content $reportPath |ConvertFrom-Json
if($formatReport.Count -ge 1) {
    throw "There are $($formatReport.Count) dotnet-format issue(s). " + `
        "The report is stored in: $reportPath"
}
