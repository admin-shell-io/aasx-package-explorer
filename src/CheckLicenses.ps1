#!/usr/bin/env pwsh
<#
.Description
This script inspects all the individual subdirectories to check that
they contain LICENSE.txt. It also checks that there are no conflicting
LICENSE and LICENSE.TXT (mind the case) files.
#>

function Main
{
    param($srcDir)

    $includes = @($srcDir)

    $excludes = @{
        $( Join-Path $srcDir ".config" ) = $true
        $( Join-Path $srcDir ".idea" ) = $true
    }

    $acceptedDirs = @()
    foreach ($dir in $( Get-ChildItem -Directory $srcDir -Force|Select-Object -Expand FullName ))
    {
        if (($excludes.ContainsKey($dir)) -and ($excludes[$dir]))
        {
            Write-Host "The subdirectory is exluded intentionally: $dir"
        }
        else
        {
            $acceptedDirs += $dir
        }
    }

    $acceptedDirs += $includes

    $problems = @()
    foreach ($dir in $acceptedDirs)
    {
        if (!(Test-Path -LiteralPath $dir -PathType Container))
        {
            throw "Expected a directory, but it is not: $dir"
        }

        $licenseTxt = Join-Path $dir "LICENSE.txt"
        if (!(Test-Path -LiteralPath $licenseTxt))
        {
            $problems += "LICENSE.txt is missing in: $dir"
        }

        foreach ($filename in @("LICENSE"))
        {
            $unexpected = Join-Path $dir $filename
            if (Test-Path -LiteralPath $unexpected)
            {
                $problems += "Unexpected ${filename}: $unexpected"
            }
        }
    }

    if ($problems.Length -ge 1)
    {
        $parts = @("There were problem(s) with licenses:")
        foreach ($problem in $problems)
        {
            $parts += "* $problem"
        }

        $nl = [Environment]::NewLine
        $msg = $parts -Join $nl
        Write-Host $msg
        Exit 1
    }

    Write-Host "All subdirectories contain LICENSE.txt. "
    Write-Host "Mind that the content has not been checked."
}

Main($PSScriptRoot)
