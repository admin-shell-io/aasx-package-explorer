<#
.SYNOPSIS
This module provides DRY build settings.
#>

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    GetArtefactsDir

function BuildConfiguration
{
    return "Debug"
}

function BuildOutputPath
{
    $configuration = BuildConfiguration
    $artefactsDir = GetArtefactsDir
    return Join-Path $artefactsDir (Join-Path "build" $configuration)
}
