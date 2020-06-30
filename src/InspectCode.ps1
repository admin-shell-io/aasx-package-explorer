﻿<#
This script inspects the code quality.

It should be run after the build.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    FindInspectCode, `
    CreateAndGetArtefactsDir

function Main
{
    $inspectcode = FindInspectCode

    $artefactsDir = CreateAndGetArtefactsDir
    $codeInspectionPath = Join-Path $artefactsDir "resharper-code-inspection.xml"

    Set-Location $PSScriptRoot

    Write-Host "Inspecting the code with inspectcode.exe ..."

    # InspectCode passes over the properties to MSBuild,
    # see https://www.jetbrains.com/help/resharper/InspectCode.html#msbuild-related-parameters
    & $inspectcode `
        "--properties:Configuration=Debug" `
        "--properties:Platform=x64" `
        "-o=$codeInspectionPath" `
        AasxPackageExplorer.sln

    [xml]$inspection = Get-Content $codeInspectionPath
    $issues = $inspection.SelectNodes('//Issue')
    if ($issues.Count -ne 0)
    {
        $take = 50
        if ($issues.Count -lt $take)
        {
            $take = $issues.Count
        }
        for($i = 0; $i -lt $take; $i++)
        {
            Write-Host "Issue $( $i + 1 ) / $( $issues.Count ): $( $issues.Item($i).OuterXml )"
        }
        if ($take -lt $issues.Count)
        {
            Write-Hose "... and some more issues ($( $issues.Count ) in total)."
        }

        throw "There are $( $issues.Count ) InspectCode issue(s). " +      `
                 "The issues are stored in: $codeInspectionPath"
    }
}

Main
