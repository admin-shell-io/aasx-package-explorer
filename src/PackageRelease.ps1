param(
    [Parameter(HelpMessage = "Version to be packaged", Mandatory = $true)]
    [string]
    $version
)

<#
.SYNOPSIS
This script packages files to be released.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    GetArtefactsDir

function PackageRelease($outputDir)
{
    $buildDir = Join-Path $( GetArtefactsDir ) "build" `
        | Join-Path -ChildPath "Release"

    if (!(Test-Path $buildDir))
    {
        throw ("The build directory with the release does " +
                "not exist: $buildDir; did you build the solution " +
                "with BuildForRelease.ps1?")
    }

    $samplesDir = Join-Path (Split-Path $PSScriptRoot -Parent) "sample-aasx"

    if (!(Test-Path $samplesDir))
    {
        throw ("The samples directory does not exist: $samplesDir; " +
                "did you download the samples with DownloadSamples.ps1?")
    }

    $eclassDir = Join-Path (Split-Path $PSScriptRoot -Parent) "eclass"

    if (!(Test-Path $eclassDir))
    {
        throw ("The eclass directory does not exist: $eclassDir; " +
                "did you copy the restricted ecl@ss files there manually?")
    }

    Write-Host "Packaging to: $outputDir"
    New-Item -ItemType Directory -Force -Path $outputDir|Out-Null


    Write-Host "* Copying AasxPackageExplorer ..."
    Copy-Item `
        -Path (Join-Path $buildDir "AasxPackageExplorer") `
        -Recurse `
        -Destination $outputDir

    Write-Host "* Copying README-packages.md ..."

    Copy-Item `
        -Path (Join-Path $PSScriptRoot "README-packages.md") `
        -Destination (Join-Path $outputDir "AasxPackageExplorer")

    Write-Host "* Copying plugins-open ..."
    $pluginsOpen = $(
    "AasxPluginBomStructure",
    "AasxPluginDocumentShelf",
    "AasxPluginExportTable",
    "AasxPluginGenericForms",
    "AasxPluginTechnicalData"
    )

    $pluginsOpenDir = Join-Path $outputDir "plugins-open"
    New-Item -ItemType Directory -Force -Path $pluginsOpenDir|Out-Null

    foreach ($plugin in $pluginsOpen)
    {
        Write-Host "  * Copying $plugin"

        Copy-Item `
            -Path (Join-Path $buildDir $plugin) `
            -Recurse `
            -Destination $pluginsOpenDir
    }

    Write-Host "* Copying plugins-webbrowser ..."
    $pluginsWebbrowserDir = Join-Path $outputDir "plugins-webbrowser"
    New-Item -ItemType Directory -Force -Path $pluginsWebbrowserDir|Out-Null
    Copy-Item `
        -Path (Join-Path $buildDir "AasxPluginWebBrowser") `
        -Recurse `
        -Destination $pluginsWebbrowserDir

    <#
    TODO (mristin, 2020-08-01): plugins-restricted are expected to be merged
    into the solution soon (next 1-2 months). The copy commands should come
    here.
    #>

    Write-Host "* Copyng samples from $samplesDir ..."
    Copy-Item -Path $samplesDir -Recurse -Destination $outputDir

    Write-Host "* Copyng eclass from $eclassDir ..."
    Copy-Item -Path $eclassDir -Recurse -Destination $outputDir

    # ---

    $archPath = Join-Path $outputDir "portable-restricted-eclass.zip"
    Write-Host "* Packaging: $archPath"
    [string[]]$paths = @(
    (Join-Path $outputDir "AasxPackageExplorer"),
    (Join-Path $outputDir "plugins-open"),
    (Join-Path $outputDir "plugins-webbrowser"),
    $eclassDir,
    $samplesDir
    )

    Compress-Archive -Path $paths -DestinationPath $archPath

    # ---

    $archPath = Join-Path $outputDir "portable-restricted.zip"
    Write-Host "* Packaging: $archPath"
    [string[]]$paths = @(
    (Join-Path $outputDir "AasxPackageExplorer"),
    (Join-Path $outputDir "plugins-open"),
    (Join-Path $outputDir "plugins-webbrowser"),
    <#
    TODO (mristin, 20-08-01): The restricted plug-ins are missing here.
    Please add them once they are available in the solution.
    #>
    $samplesDir
    )

    Compress-Archive -Path $paths -DestinationPath $archPath

    # ---

    $archPath = Join-Path $outputDir "portable-open.zip"
    Write-Host "* Packaging: $archPath"
    [string[]]$paths = @(
    (Join-Path $outputDir "AasxPackageExplorer"),
    (Join-Path $outputDir "plugins-open"),
    (Join-Path $outputDir "plugins-webbrowser"),
    $samplesDir
    )

    Compress-Archive -Path $paths -DestinationPath $archPath

    # ---

    $archPath = Join-Path $outputDir "portable-small.zip"
    Write-Host "* Packaging: $archPath"
    [string[]]$paths = @(
    (Join-Path $outputDir "AasxPackageExplorer"),
    (Join-Path $outputDir "plugins-open")
    # Samples are removed on purpose from the small release.
    )

    Compress-Archive -Path $paths -DestinationPath $archPath

    # ---

    # Do not copy the source code in the releases.
    # The source code will be distributed automatically through Github releases.

    Write-Host "Done packaging the release."
}

function Main
{
    if ($version -eq "")
    {
        throw "Unexpected empty version"
    }

    $versionRe = [Regex]::new(
            '^[0-9]{2}-(0[1-9]|11|12)-(0[1-9]|1[1-9]|2[1-9]|3[0-1])' +
                    '(\.(pre|post)[0-9]+)?$')

    if (!$versionRe.IsMatch($version))
    {
        throw ("Unexpected version; " +
                "expected year-month-day (*e.g.*, 19-10-23) followed by " +
                "an optional pre/post tag (*e.g.*, 19-10-23.pre3), " +
                "but got: $version")
    }

    $outputDir = Join-Path $( GetArtefactsDir ) "release" `
        | Join-Path -ChildPath $version

    if (Test-Path $outputDir)
    {
        Write-Host ("Removing previous release so that " +
                "the new release is packaged clean: $outputDir")
        Remove-Item -Recurse -Force $outputDir
    }

    PackageRelease -outputDir $outputDir
}

$previousLocation = Get-Location; try
{
    Main
}
finally
{
    Set-Location $previousLocation
}