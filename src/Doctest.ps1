#!/usr/bin/env pwsh
param(
    [Parameter(HelpMessage = "If set, doctests are only checked in a dry-run, not generated")]
    [switch]
    $check = $false
)

<#
.SYNOPSIS
This script checks that all the doctests were generated correctly.
#>

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet, `
    AssertDoctestCsharpVersion

function Main
{
    $excludes = @{
        # Paths are relative to `src/`.
        "packages" = $true
        ".config" = $true
        ".idea" = $true
        "bin" = $true
    }

    Set-Location $PSScriptRoot

    AssertDotnet
    AssertDoctestCsharpVersion

    $srcDir = $PSScriptRoot

    if($check)
    {
        Write-Host "Checking the doctests beneath: $srcDir"
    }
    else
    {
        Write-Host "Generating the doctests beneath: $srcDir"
    }

    $doctestableNames = @()

    foreach($subdir in (Get-ChildItem -Path $srcDir -Directory))
    {
        $name = Split-Path -Path $subdir -Leaf
        if($name.EndsWith(".Tests") -or ($excludes.ContainsKey($name) -and $excludes[$name]))
        {
            Write-Host "Ignored: $name"
            continue;
        }
        $doctestableNames += $name
    }

    $doctestableNames = $doctestableNames | Sort-Object

    $sep = [IO.Path]::DirectorySeparatorChar

    $cmd = "dotnet"
    $cmdArgs = @("doctest-csharp", "--input-output") + `
            $doctestableNames + `
            @("--excludes", "**${sep}obj${sep}**")

    if($check)
    {
        $cmdArgs += "--check"
    }

    $quotedCmdArgs = $cmdArgs | ForEach-Object { "'$_'"}
    Write-Host "Executing: $cmd $($quotedCmdArgs -Join " ")"

    & $cmd $cmdArgs
    if($LASTEXITCODE -ne 0)
    {
        throw "doctest-csharp failed; see the log above."
    }
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
