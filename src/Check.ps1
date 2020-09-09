<#
.SYNOPSIS
This script runs all the pre-merge checks locally.
#>

$ErrorActionPreference = "Stop"

function LogAndExecute($Expression)
{
    Write-Host "---"
    Write-Host "Running: $Expression"
    Write-Host "---"

    Invoke-Expression $Expression
}

function Main
{
    # LogAndExecute "$(Join-Path $PSScriptRoot "CheckPushCommitMessages.ps1")"
    LogAndExecute "$(Join-Path $PSScriptRoot "CheckLicenses.ps1")"
    LogAndExecute "$(Join-Path $PSScriptRoot "CheckFormat.ps1")"
    LogAndExecute "$(Join-Path $PSScriptRoot "CheckBiteSized.ps1")"
    LogAndExecute "$(Join-Path $PSScriptRoot "CheckDeadCode.ps1")"
    LogAndExecute "$(Join-Path $PSScriptRoot "CheckTodos.ps1")"
    LogAndExecute "$(Join-Path $PSScriptRoot "Doctest.ps1") -check"
    LogAndExecute "$(Join-Path $PSScriptRoot "BuildForDebug.ps1")"
    LogAndExecute "$(Join-Path $PSScriptRoot "Test.ps1")"
    LogAndExecute "$(Join-Path $PSScriptRoot "InspectCode.ps1")"
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
