param(
    [Parameter(HelpMessage = "If set, cleans up the previous build instead of performing a new one")]
    [switch]
    $clean = $false
)

<#
.DESCRIPTION
This script builds the solution for the release.

It is expected that you installed the dev dependencies as well as
solution dependencies (see Install*.ps1 scripts).
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    FindMSBuild,  `
     GetArtefactsDir

function Main
{
    $msbuild = FindMSBuild

    Write-Host "Using MSBuild from: $msbuild"

    $configuration = "Release"

    $outputPath = Join-Path $( GetArtefactsDir ) "build" `
        | Join-Path -ChildPath $configuration

    Set-Location $PSScriptRoot

    if (!$clean)
    {
        Write-Host "Building to: $outputPath"

        New-Item -ItemType Directory -Force -Path $outputPath|Out-Null

        # This list projects that are to be build for the release
        $projects = @(
        "AasxPackageExplorer"
        "AasxPluginBomStructure",
        "AasxPluginDocumentShelf",
        "AasxPluginExportTable",
        "AasxPluginGenericForms",
        "AasxPluginMtpViewer",
        "AasxPluginTechnicalData",
        "AasxPluginUaNetClient",
        "AasxPluginUaNetServer",
        "AasxPluginWebBrowser"
        )

        foreach ($project in $projects)
        {
            $csprojectPath = Join-Path $project "$project.csproj"
            $projectOutputPath = Join-Path $outputPath $project

            Write-Host "Building $project to: $projectOutputPath"

            & $msbuild `
                "/p:OutputPath=$projectOutputPath" `
                "/p:Configuration=$configuration" `
                "/p:Platform=x64" `
                /maxcpucount `
                $csprojectPath `
                /t:build

            $buildExitCode = $LASTEXITCODE
            Write-Host "MSBuild exit code: $buildExitCode"
            if ($buildExitCode -ne 0)
            {
                throw "MSBuild failed."
            }
        }
    }
    else
    {
        Write-Host "Cleaning up the build ..."

        & $msbuild "/p:Configuration=$configuration" /t:Clean

        $buildExitCode = $LASTEXITCODE
        Write-Host "MSBuild exit code: $buildExitCode"
        if ($buildExitCode -ne 0)
        {
            throw "MSBuild failed."
        }

        if (Test-Path $outputPath)
        {
            Write-Host "Removing: $outputPath"
            Remove-Item -Recurse -Force $outputPath
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
