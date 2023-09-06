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
    AssertDotnet, `
    GetArtefactsDir

function Main
{
    AssertDotnet

    $configuration = "Release"

    $outputPath = Join-Path $( GetArtefactsDir ) "build" `
        | Join-Path -ChildPath $configuration

    Set-Location $PSScriptRoot

    if (!$clean)
    {
        Write-Host "Building and publishing to: $outputPath"

        New-Item -ItemType Directory -Force -Path $outputPath|Out-Null

        # This list projects that are to be build for the release
        $projects = @(
        "AasxPackageExplorer"
        "AasxPluginAdvancedTextEditor",
        "AasxPluginBomStructure",
        "AasxPluginDocumentShelf",
        "AasxPluginExportTable",
        "AasxPluginGenericForms",
        "AasxPluginImageMap",
        "AasxPluginKnownSubmodels",
        "AasxPluginMtpViewer",
        "AasxPluginPlotting",
        "AasxPluginSmdExporter",
        "AasxPluginTechnicalData",
        "AasxPluginUaNetClient",
        "AasxPluginUaNetServer",
        "AasxPluginWebBrowser",
        "BlazorExplorer"
        )

        foreach ($project in $projects)
        {
            $csprojectPath = Join-Path $project "$project.csproj"
            $projectOutputPath = Join-Path $outputPath $project

            Write-Host "Building $project to: $projectOutputPath"

            #if ($project -ne "BlazorUI")
            if ($project -ne "BlazorExplorer")
            {
                & dotnet.exe publish `
                --output $projectOutputPath `
                --runtime win-x64 `
                --configuration $configuration `
                $csprojectPath
            }
            else
            {
                & dotnet.exe publish `
                --output $projectOutputPath `
                --configuration $configuration `
                $csprojectPath
            }


            $buildExitCode = $LASTEXITCODE
            Write-Host "dotnet publish exit code: $buildExitCode"
            if ($buildExitCode -ne 0)
            {
                throw "dotnet publish failed."
            }
        }
    }
    else
    {
        Write-Host "Cleaning up the build ..."

        & dotnet.exe clean --configuration $configuration

        $buildExitCode = $LASTEXITCODE
        Write-Host "dotnet clean exit code: $buildExitCode"
        if ($buildExitCode -ne 0)
        {
            throw "dotnet clean failed."
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