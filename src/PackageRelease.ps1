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

    # ---

    $archPath = Join-Path $outputDir "portable.zip"
    Write-Host "* Packaging: $archPath"
    [string[]]$paths = @(
    (Join-Path $outputDir "AasxPackageExplorer"),
    (Join-Path $outputDir "plugins-open"),
    (Join-Path $outputDir "plugins-webbrowser")
    )

    Compress-Archive -Path $paths -DestinationPath $archPath

    # ---

    $archPath = Join-Path $outputDir "portable-small.zip"
    Write-Host "* Packaging: $archPath"
    [string[]]$paths = @(
    (Join-Path $outputDir "AasxPackageExplorer"),
    (Join-Path $outputDir "plugins-open")
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
            '^[0-9]{4}-(0[1-9]|10|11|12)-(0[1-9]|1[0-9]|2[0-9]|3[0-1])' +
            '(\.(alpha|beta))?$')

    $latestVersionRe = [Regex]::new('^LATEST(\.(alpha|beta))?$')

    if ((!$latestVersionRe.IsMatch($version)) -and
            (!$versionRe.IsMatch($version)))
    {
        throw ("Unexpected version; " +
                "expected either year-month-day (*e.g.*, 2019-10-23) " +
                "followed by an optional maturity tag " +
                "(*e.g.*, 2019-10-23.alpha) " +
                "or LATEST, but got: $version")
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