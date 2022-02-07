<#
.SYNOPSIS
This script inspects the code quality after the build.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    CreateAndGetArtefactsDir

function Main
{
    Set-Location $PSScriptRoot

    $artefactsDir = CreateAndGetArtefactsDir
    $codeInspectionPath = Join-Path $artefactsDir "resharper-code-inspection.xml"
    New-Item -ItemType Directory -Force -Path "$artefactsDir"|Out-Null

    $cachesHome = Join-Path $artefactsDir "inspectcode-caches"
    New-Item -ItemType Directory -Force -Path "$cachesHome"|Out-Null

    # InspectCode passes over the properties to MSBuild,
    # see https://www.jetbrains.com/help/resharper/InspectCode.html#msbuild-related-parameters
    $pathToSolution = "AasxPackageExplorer.sln"

    Write-Host "Inspecting the code with inspectcode ..."
    Write-Host "* Output goes to: $codeInspectionPath"
    Write-Host "* Caches home is at: $cachesHome"
    Write-Host "* The working directory is: $(Get-Location)"
    Write-Host "* The path to solution is: $pathToSolution"

    # We need to allow for a custom path to InspectCode since dotnet jb command fails on systems
    # where there are spaces in the paths.
    if (Test-Path env:INSPECT_CODE_PATH)
    {
        Write-Host ( `
            "* The environment variable INSPECT_CODE_PATH has been defined, " + `
            "inspecting with it: $($env:INSPECT_CODE_PATH)")

        if ($null -eq (Test-Path $env:INSPECT_CODE_PATH -ErrorAction SilentlyContinue))
        {
            throw "Unable to find the specified INSPECT_CODE_PATH: $( $env:INSPECT_CODE_PATH )"
        }

        & $env:INSPECT_CODE_PATH `
        "-o=$codeInspectionPath" `
        "--caches-home=$cachesHome" `
        '--exclude=*\obj\*;packages\*;*\bin\*;*\*.json' `
        "$pathToSolution"
    }
    else
    {
        Write-Host "* Inspecting the code with dodtnet jb inspectcode"

        & dotnet.exe jb inspectcode `
            "-o=$codeInspectionPath" `
            "--caches-home=$cachesHome" `
            '--exclude=*\obj\*;packages\*;*\bin\*;*\*.json' `
            "$pathToSolution"
    }

    [xml]$inspection = Get-Content $codeInspectionPath

    $issues = $inspection.SelectNodes('//Issue')
    if ($issues.Count -ne 0)
    {
        # Compute histogram of the issues

        $histogram = @{}
        foreach($issue in $issues)
        {
            $typeId = $issue.TypeId
            if($histogram.ContainsKey($typeId))
            {
                $histogram[$typeId]++
            }
            else
            {
                $histogram[$typeId] = 1
            }
        }

        Write-Host
        Write-Host "The distribution of the issues:"
        foreach($kv in $histogram.GetEnumerator()|Sort-Object Value -Descending)
        {
            Write-Host (" * {0,-60} {1,6}" -f ($kv.Key + ":"), $kv.Value)
        }

        # Display a couple of the issues

        $take = 20
        if ($issues.Count -lt $take)
        {
            $take = $issues.Count
        }

        Write-Host
        Write-Host "The first $take issue(s):"
        for($i = 0; $i -lt $take; $i++)
        {
            Write-Host "Issue $( $i + 1 ) / $( $issues.Count ): $( $issues.Item($i).OuterXml )"
        }
        if ($take -lt $issues.Count)
        {
            Write-Host "... and some more issues ($( $issues.Count ) in total)."
        }

        throw (
            "There are $( $issues.Count ) InspectCode issue(s). " +
            "The issues are stored in: $codeInspectionPath. " +
            "Please fix the issues either manually or semi-automatically " +
            "using an IDE such as Rider (https://www.jetbrains.com/rider/ and " +
            "https://www.jetbrains.com/help/resharper/Code_Analysis__Quick-Fixes.html). " +
            "For the information about individual issues, have a look at: " +
            "https://www.jetbrains.com/help/resharper/Reference__Code_Inspections_CSHARP.html#BestPractice"
        )
    }
    else
    {
        Write-Host "There were no issues detected."
    }
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }

