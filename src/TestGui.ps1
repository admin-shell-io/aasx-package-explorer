#!/usr/bin/env pwsh
param(
    [Parameter(HelpMessage = "If set, list the names of the test cases and exit")]
    [switch]
    $Explore = $false,

    [Parameter(HelpMessage = "If set, execute only the given tests")]
    [string]
    $Test = ""
)

<#
.SYNOPSIS
    This script runs all the GUI tests specified in the testDlls variable below.

.EXAMPLE
    TestGui.ps1

    This runs all the GUI tests.

.EXAMPLE
    TestGui.ps1 -Explore

    This lists all the GUI tests.

.EXAMPLE
    TestGui.ps1 -Test "AasxPackageExplorer.GuiTests.TestBasic.Test_that_the_application_starts_without_errors"

    This executes a single GUI test.

.EXAMPLE
    Test.ps1 -Test "AasxPackageExplorer.GuiTests"

    This executes all the tests prefixed with AasxPackageExplorer.GuiTests.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    FindNunit3Console,  `
    GetSamplesDir, `
    GetArtefactsDir

function Main
{
    $nunit3Console = FindNunit3Console

    Set-Location $PSScriptRoot

    $artefactsDir = GetArtefactsDir

    $targetDir = Join-Path $artefactsDir "build" | Join-Path -ChildPath "Debug"

    $samplesDir = GetSamplesDir
    if(!(Test-Path $samplesDir))
    {
        throw (
            "The directory containing samples could not be found: " +
            "$samplesDir; these samples are necessary to " +
            "perform the integration tests. " +
            "Did you maybe forget to download them with DownloadSamples.ps1?"
        )
    }

    # Glob test DLLs relative to $targetDir
    $testDlls = Get-ChildItem `
        -Path (Join-Path $targetDir "*.GuiTests.dll") `
        -File `
        -Name

    [string[]]$absTestDlls = @()
    foreach ($testDll in $testDlls)
    {
        $absTestDll = Join-Path $targetDir $testDll
        if (!(Test-Path $absTestDll))
        {
            throw "Assertion violated, test DLL could not be found: $absTestDll"
        }
        $absTestDlls += $absTestDll
    }

    if ($Explore)
    {
        Push-Location
        try
        {
            Set-Location $targetDir
            & $nunit3Console $absTestDlls --explore
        }
        finally
        {
            Pop-Location
        }
        exit 0
    }

    $env:SAMPLE_AASX_DIR = $samplesDir
    $env:AASX_PACKAGE_EXPLORER_RELEASE_DIR = $targetDir

    Push-Location
    try
    {
        Set-Location $targetDir

        # If -Test is not specified, run all the tests
        if ($Test -eq "")
        {
            & $nunit3Console `
                --stoponerror `
                $absTestDlls
        }
        else
        {
            & $nunit3Console `
                --test=$Test `
                --stoponerror `
                $absTestDlls
        }

        if ($LASTEXITCODE -ne 0)
        {
            throw "Running the GUI test(s) with Nunit3 console failed."
        }
    }
    finally
    {
        Pop-Location
    }
}

$previousLocation = Get-Location; try
{
    Main
}
finally
{
    Set-Location $previousLocation
}
