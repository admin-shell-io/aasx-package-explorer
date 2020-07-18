<#
.SYNOPSIS
This script installs the dependencies needed to generate documentation for developers.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet, `
    GetToolsDir


function Main
{
    if ($null -eq (Get-Command "nuget.exe" -ErrorAction SilentlyContinue))
    {
       throw "Unable to find nuget.exe in your PATH"
    }

    Push-Location

    try
    {
        Set-Location $PSScriptRoot
        $toolsDir = GetToolsDir
        New-Item -ItemType Directory -Force -Path $toolsDir

        Write-Host "Installing DocFX 2.56.1 ..."
        nuget install docfx.console -Version 2.56.1 -OutputDirectory $toolsDir
    }
    finally
    {
        Pop-Location
    }
}

Main