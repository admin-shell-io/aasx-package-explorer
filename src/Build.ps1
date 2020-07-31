﻿<#
.DESCRIPTION
This script builds the solution.

It is expected that you installed 
the dev dependencies (with InstallDevDependenciesOnMV.ps1 or manually) 
as well as solution dependencies (InstallBuildDependencies.ps1 or manually).
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet, `
    AssertDotnetFormatVersion, `
    FindMSBuild, `
    FindInspectCode

Import-Module (Join-Path $PSScriptRoot BuildCommon.psm1) -Function `
    BuildConfiguration, `
    BuildOutputPath

function Main
{
    $msbuild = FindMSBuild

    Write-Host "Using MSBuild from: $msbuild"

    $configuration = BuildConfiguration
    $outputPath = BuildOutputPath

    Write-Host "Building to: $outputPath"

    New-Item -ItemType Directory -Force -Path $outputPath|Out-Null

    Set-Location $PSScriptRoot
    & $msbuild `
        /p:OutputPath=$outputPath `
        /p:Configuration=$configuration `
        /p:Platform=x64 `
        /maxcpucount

    $buildExitCode = $LASTEXITCODE
    Write-Host "Build exit code: $buildExitCode"
    if ($buildExitCode -ne 0)
    {
        throw "Build failed."
    }
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
