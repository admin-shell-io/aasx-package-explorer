<#
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
    FindInspectCode, `
    CreateAndGetArtefactsDir

$msbuild = FindMSBuild
Write-Host $msbuild.GetType()

Write-Host "Using MSBuild from: $msbuild"

$configuration = "Debug"
$artefactsDir = CreateAndGetArtefactsDir

$outputPath = Join-Path $artefactsDir (Join-Path "build" $configuration)

Write-Host "Building to: $outputPath"

cd $PSScriptRoot
& $msbuild `
    /p:OutputPath=$outputPath `
    /p:Configuration=$configuration `
    /p:Platform=x64