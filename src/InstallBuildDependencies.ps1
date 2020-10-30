<#
.SYNOPSIS
This script installs the dependencies necessary to build the solution.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet

function Main
{
    AssertDotnet

    Set-Location $PSScriptRoot

    Write-Host "Restoring packages for the solution..."
    dotnet restore
}

$previousLocation = Get-Location; try
{
    Main
}
finally
{
    Set-Location $previousLocation
}
