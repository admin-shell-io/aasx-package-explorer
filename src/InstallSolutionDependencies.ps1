<#
.SYNOPSIS
This script runs all the pre-merge checks locally.
#>

$ErrorActionPreference = "Stop"

function LogAndExecute($Expression, $Timestamp)
{
    Write-Host "---"
    Write-Host "Running: $Expression"
    Write-Host "---"

    if ( $Expression.StartsWith("./Install"))
    {
        $title = $Expression -replace '^./Install([a-zA-Z_0-9-]+)\.ps1.*$', '$1'
    }
    elseif ( $Expression.StartsWith("./Download"))
    {
        $title = $Expression -replace '^./Download([a-zA-Z_0-9-]+)\.ps1.*$', '$1'
    }
    else
    {
        throw "Unhandled title for the expression: $( $Expression|ConvertTo-Json )"
    }

    & powershell $Expression|ForEach-Object {
        Write-Host `
            -NoNewline `
            -ForegroundColor "DarkMagenta" `
            -BackgroundColor "White" `
            "${title}:${Timestamp}:"

        Write-Host " $_"
    }
}

function Main
{
    Set-Location $PSScriptRoot

    $timestamp = [DateTime]::Now.ToString('HH:mm:ss')

    LogAndExecute `
        -Expression "./InstallToolsForBuildTestInspect.ps1" `
        -Timestamp $timestamp

    LogAndExecute `
        -Expression "./InstallToolsForStyle.ps1" `
        -Timestamp $timestamp

    LogAndExecute `
        -Expression "./InstallBuildDependencies.ps1" `
        -Timestamp $timestamp

    LogAndExecute `
        -Expression "./DownloadSamples.ps1" `
        -Timestamp $timestamp


    Write-Host
    Write-Host "All solution dependencies have been installed."
}

$previousLocation = Get-Location; try
{
    Main
}
finally
{
    Set-Location $previousLocation
}
