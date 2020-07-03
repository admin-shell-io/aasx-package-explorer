<#
.SYNOPSIS
This script runs all the unit tests specified in the testDlls variable below.
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
        throw ("The directory containing samples could not be found: " +
                "$samplesDir; these samples are necessary to " +
                "perform the integration tests.")
    }



    # Relative to $targetDir
    $testDlls = @( "AasxCsharpLibrary.Tests.dll", "AasxDictionaryImport.Tests.dll" )

    foreach ($testDll in $testDlls)
    {
        $absTestDll = Join-Path $targetDir $testDll
        if (!(Test-Path $absTestDll))
        {
            throw ("Test DLL could not be found: $absTestDll; " +
                    "did you compile the solution with BuildForDebug.ps1?")
        }
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
            throw "The unit test(s) failed."
        }
    }
    finally
    {
        if ($null -ne $prevEnvSampleAasxDir)
        {
            $env:SAMPLE_AASX_DIR = $prevEnvSampleAasxDir
        }
    }

    # Scripts are expected at the root of src/
    $srcDir = $PSScriptRoot

    $coverageReportPath = Join-Path $artefactsDir "CoverageReport"
    & $reportGenerator `
        -reports:$coverageResultsPath `
        -targetdir:$coverageReportPath `
        -sourcedirs:$srcDir
}

$previousLocation = Get-Location; try
{
    Main
}
finally
{
    Set-Location $previousLocation
}
