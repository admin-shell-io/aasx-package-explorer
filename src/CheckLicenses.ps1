#!/usr/bin/env pwsh
<#
.DESCRIPTION
This script inspects all the individual subdirectories to check that
they contain LICENSE.txt. It also checks that there are no conflicting
LICENSE and LICENSE.TXT (mind the case) files.
#>

function CheckLineLength($Path)
{
    $text = Get-Content -Path $Path
    $lines = $text.Split(
        @("`r`n", "`r", "`n"),
        [StringSplitOptions]::None)

    $errors = @()
    for($i = 0; $i -lt $lines.Count; $i++)
    {
        $line = $lines[$i]
        if($line.Length -gt 80)
        {
            $msg = "${Path}:$($i+1): Line exceeds the maximum of 80 characters: "
            $msg += $line|ConvertTo-Json
            $errors += $msg
        }
    }

    return $errors
}

function Main
{
    $srcDir = $PSScriptRoot

    $includes = @($srcDir)

    $excludes = @{
        $( Join-Path $srcDir ".config" ) = $true
        $( Join-Path $srcDir ".idea" ) = $true
        $( Join-Path $srcDir ".vs" ) = $true
        $( Join-Path $srcDir "bin" ) = $true
        $( Join-Path $srcDir "packages" ) = $true
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
            $problems += "${dir}: LICENSE.txt is missing."
        }
        else
        {
            $problems = $problems + (CheckLineLength -Path $licenseTxt)

        }

        foreach ($filename in @("LICENSE"))
        {
            $unexpected = Join-Path $dir $filename
            if (Test-Path -LiteralPath $unexpected)
            {
                $problems += "${unexpected}: Unexpected file name ${filename}"
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
        throw $msg
    }

    Write-Host "All subdirectories contain LICENSE.txt. "
    Write-Host "Mind that the content has not been checked."
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
