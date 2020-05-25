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
    FindInspectCode


$msbuild = FindMSBuild
Write-Host $msbuild.GetType()

Write-Host "Using MSBuild from: $msbuild"

cd $PSScriptRoot
& $msbuild --% /p:Configuration=Debug /p:Platform=x64 