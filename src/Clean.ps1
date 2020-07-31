<#
.SYNOPSIS
This script cleans the build.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    FindMSBuild

Import-Module (Join-Path $PSScriptRoot BuildCommon.psm1) -Function `
    BuildConfiguration, `
    BuildOutputPath


function Main
{
    $msbuild = FindMSBuild

    Write-Host "Using MSBuild from: $msbuild"

    Set-Location $PSScriptRoot
    & $msbuild /t:Clean

    $buildExitCode = $LASTEXITCODE
    Write-Host "MSBuild exit code: $buildExitCode"
    if ($buildExitCode -ne 0)
    {
        throw "Clean failed."
    }

    $outputPath = BuildOutputPath
    if(($outputPath -eq "") -or ($outputPath -eq "."))
    {
        throw "Unexpected build output path: $outputPath"
    }

    $configuration = BuildConfiguration
    if($configuration -eq "")
    {
        throw "Unexpected empty build configuration"
    }

    $name = Split-Path -Path $outputPath -Leaf
    if($name -ne $configuration)
    {
        throw "Expected build output path to match configuration $configuration, but got: $outputPath"
    }

    if(Test-Path $outputPath)
    {
        Write-Host "Removing: $outputPath"
        Remove-Item -Recurse -Force $outputPath
    }
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
