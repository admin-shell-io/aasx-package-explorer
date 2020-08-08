<#
.SYNOPSIS
This script inspects the code quality after the build.
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

    $cachesHome = Join-Path $artefactsDir "inspectcode-caches"
    New-Item -ItemType Directory -Force -Path "$cachesHome"|Out-Null

    # InspectCode passes over the properties to MSBuild,
    # see https://www.jetbrains.com/help/resharper/InspectCode.html#msbuild-related-parameters
    & $inspectcode `
        "--properties:Configuration=Debug" `
        "--properties:Platform=x64" `
        "-o=$codeInspectionPath" `
        "--caches-home=$cachesHome" `
        '--exclude=*\obj\*;packages\*;*\bin\*' `
        AasxPackageExplorer.sln

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

        throw "There are $( $issues.Count ) InspectCode issue(s). " +      `
                 "The issues are stored in: $codeInspectionPath"
    }
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }

