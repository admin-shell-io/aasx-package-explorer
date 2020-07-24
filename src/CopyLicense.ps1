#!/usr/bin/env pwsh
<#
.DESCRIPTION
This script copies the LICENSE.txt from the repository root in all
the first-level subdirectories within `src/`.
Please make sure to add the excludes and the includes to this script.
The includes trump the excludes.
#>

function Main
{
    $srcDir = $PSScriptRoot

    Write-Host "The src directory is: $srcDir"
    $licenseTxt = Join-Path $srcDir "LICENSE.txt"

    # Excludes and includes need to be absolute paths.

    $excludes = @{
    # Example:
    # $( Join-Path $srcDir "AasxPluginBomStructure" ) = true
    }

    $includes = @(
    $( Join-Path $srcDir "AasxPluginWebBrowser/Resources" ),
    $( Join-Path $srcDir "AasxPluginTechnicalData/Resources" ),
    $( Join-Path $srcDir "AasxPluginExportTable/Resources" ),
    $( Join-Path $srcDir "AasxPluginGenericForms/Resources" ),
    $( Join-Path $srcDir "AasxPluginDocumentShelf/Resources" )
    )

    $acceptedDirs = @( )
    foreach ($dir in $( Dir -Directory $srcDir|Select -Expand FullName ))
    {
        if (!$excludes.ContainsKey($dir) -or !$excludes[$dir])
        {
            $acceptedDirs += $dir
        }
        else
        {
            Write-Host "The subdirectory is exluded intentionally: $dir"
        }
    }

    $acceptedDirs += $includes

    foreach ($dir in $acceptedDirs)
    {
        if (!(Test-Path -LiteralPath $dir -PathType Container))
        {
            throw "Expected a directory, but it is not: $dir"
        }
    }

    Write-Host "Copying the license to:"
    foreach ($dir in $acceptedDirs)
    {
        Write-Host $dir
        Copy-Item $licenseTxt -Destination $dir
    }
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
