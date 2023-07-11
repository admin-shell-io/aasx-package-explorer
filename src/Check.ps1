<#
.SYNOPSIS
This script runs all the pre-merge checks locally.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet

function Main
{
    AssertDotnet
    Set-Location $PSScriptRoot
    & dotnet.exe run --project CheckScript
    Read-Host -Prompt "Press Enter to exit"
}

$previousLocation = Get-Location; try
{
    Main
}
finally
{
    Set-Location $previousLocation
}
