<#
This script builds the solution, runs Resharper InspectCode and dotnet-format to
check the quality of the code base.
#>

$ErrorActionPreference = "Stop"

cd $PSScriptRoot
.\CheckFormat.ps1
.\Build.ps1
.\Test.ps1
.\InspectCode.ps1