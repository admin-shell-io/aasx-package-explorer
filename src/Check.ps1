<#
This script builds the solution, runs Resharper InspectCode and dotnet-format to
check the quality of the code base.
#>

$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot
.\CheckPushCommitMessages.ps1
.\CheckLicenses.ps1
.\CheckFormat.ps1
.\CheckBiteSized.ps1
.\CheckDeadCode.ps1
.\CheckTodos.ps1
.\Doctest.ps1 -check
.\Build.ps1
.\Test.ps1
.\InspectCode.ps1
