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
    $( Join-Path $srcDir ".config" ) = $true
    $( Join-Path $srcDir ".idea" ) = $true
    $( Join-Path $srcDir "packages" ) = $true
    $( Join-Path $srcDir "bin" ) = $true

    $( Join-Path $srcDir "AasxPluginBomStructure" ) = $true
    $( Join-Path $srcDir "AasxPluginDocumentShelf" ) = $true
    $( Join-Path $srcDir "AasxPluginExportTable" ) = $true
    $( Join-Path $srcDir "AasxPluginGenericForms" ) = $true
    $( Join-Path $srcDir "AasxPluginMtpViewer" ) = $true
    $( Join-Path $srcDir "AasxPluginTechnicalData" ) = $true
    $( Join-Path $srcDir "AasxPluginUaNetClient" ) = $true
    $( Join-Path $srcDir "AasxPluginUaNetServer" ) = $true
    $( Join-Path $srcDir "AasxPluginWebBrowser" ) = $true
    }

    $includes = @(
    # Example:
    # $( Join-Path $srcDir "SomeProject/Somesubdir" )
    )

    $acceptedDirs = @( )
    foreach ($dir in $( Get-ChildItem -Directory $srcDir|Select-Object -Expand FullName ))
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

    Write-Host "Copying the license from $licenseTxt to:"
    foreach ($dir in $acceptedDirs)
    {
        $destination = Join-Path $dir "LICENSE.txt"
        Write-Host $destination
        Copy-Item $licenseTxt -Destination $destination
    }
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
