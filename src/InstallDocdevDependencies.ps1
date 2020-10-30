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
    
    AssertDotnet

    Set-Location $PSScriptRoot
    $toolsDir = GetToolsDir
    New-Item -ItemType Directory -Force -Path $toolsDir|Out-Null

    Write-Host "Installing DocFX 2.56.1 ..."
    nuget install docfx.console -Version 2.56.1 -OutputDirectory $toolsDir

    dotnet tool restore
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
