<#
This script builds the solution, runs Resharper InspectCode and dotnet-format to
check the quality of the code base.
#>

$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot
.\CheckLicense.ps1
.\CheckFormat.ps1
.\CheckDeadCode.ps1
.\Build.ps1
.\Test.ps1
.\InspectCode.ps1
