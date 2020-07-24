<#
.SYNOPSIS
This script runs all the pre-merge checks locally.
#>

$ErrorActionPreference = "Stop"

function Main
{
    Set-Location $PSScriptRoot
    .\CheckPushCommitMessages.ps1
    .\CheckLicenses.ps1
    .\CheckFormat.ps1
    .\CheckBiteSized.ps1
    .\CheckDeadCode.ps1
    .\CheckTodos.ps1
    .\Doctest.ps1 -check
    .\Build.ps1
    .\Test.ps1
    .\InspectCode.ps1
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
