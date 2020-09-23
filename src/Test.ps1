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
    This script runs all the unit tests specified in the testDlls variable below.

.EXAMPLE
    Test.ps1

    This runs all the unit tests and creates the coverage report.

.EXAMPLE
    Test.ps1 -Explore

    This lists all the unit tests.

.EXAMPLE
    Test.ps1 -Test "AasxDictionaryImport.Cdd.Tests.Test_PropertyWrapper.ReferenceType"

    This executes a single unit test.

.EXAMPLE
    Test.ps1 -Test "AasxDictionaryImport"

    This executes all the tests prefixed with AasxDictionaryImport.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    FindNunit3Console,  `
    FindOpenCoverConsole,  `
    CreateAndGetArtefactsDir,  `
    FindReportGenerator, `
    GetSamplesDir

function Main
{
    $nunit3Console = FindNunit3Console
    $openCoverConsole = FindOpenCoverConsole
    $reportGenerator = FindReportGenerator

    Set-Location $PSScriptRoot

    $artefactsDir = CreateAndGetArtefactsDir

    $testResultsPath = Join-Path $artefactsDir "TestResults.xml"
    $coverageResultsPath = Join-Path $artefactsDir "CoverageResults.xml"

    $targetDir = Join-Path $artefactsDir "build\Debug"

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
        -Path (Join-Path $targetDir "*.Tests.dll") `
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

    if (Test-Path env:SAMPLE_AASX_DIR)
    {
        $prevEnvSampleAasxDir = $env:SAMPLE_AASX_DIR
    }
    else
    {
        $prevEnvSampleAasxDir = $null
    }

    try
    {
        $env:SAMPLE_AASX_DIR = $samplesDir

        # If -Test is not specified, run all the unit tests with coverage.
        if ($Test -eq "")
        {
            & $openCoverConsole `
                -target:$nunit3Console `
                -targetargs:( `
                    "--noheader --shadowcopy=false " +
                     "--result=$testResultsPath " +
                     "--stoponerror " +
                     ($testDlls -Join " ")
                ) `
                -targetdir:$targetDir `
                -output:$coverageResultsPath `
                -register:Path64 `
                -filter:"+[Aasx*]*" `
                -returntargetcode
            if ($LASTEXITCODE -ne 0)
            {
                throw "Running the unit tests with OpenCover failed."
            }

            # Scripts are expected at the root of src/
            $srcDir = $PSScriptRoot

            $coverageReportPath = Join-Path $artefactsDir "CoverageReport"
            & $reportGenerator `
                -reports:$coverageResultsPath `
                -targetdir:$coverageReportPath `
                -sourcedirs:$srcDir
        }
        else
        {
            Push-Location
            try
            {
                Set-Location $targetDir
                & $nunit3Console $absTestDlls --test=$Test
                if ($LASTEXITCODE -ne 0)
                {
                    throw "Running the unit tests with Nunit3 console failed."
                }
            }
            finally
            {
                Pop-Location
            }
        }
    }
    finally
    {
        if ($null -ne $prevEnvSampleAasxDir)
        {
            $env:SAMPLE_AASX_DIR = $prevEnvSampleAasxDir
        }
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
