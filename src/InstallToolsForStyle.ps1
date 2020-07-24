<#
.SYNOPSIS
This script installs the tools necessary to build and check the solution.
#>

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet

function Main {
    Set-Location $PSScriptRoot

    Write-Host "Restoring dotnet tools for the solution ..."
    dotnet tool restore
}

Push-Location
try { Main } finally { Pop-Location }
