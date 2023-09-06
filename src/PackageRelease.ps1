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

    ##
    # Define plugins
    ##
    
    $smallPlugins = $(
        "AasxPluginAdvancedTextEditor",
        "AasxPluginBomStructure",
        "AasxPluginDocumentShelf",
        "AasxPluginExportTable",
        "AasxPluginGenericForms",
        "AasxPluginImageMap",
        "AasxPluginMtpViewer",
        "AasxPluginPlotting",
        "AasxPluginSmdExporter",
        "AasxPluginTechnicalData"
        "AasxPluginUaNetClient",
        "AasxPluginUaNetServer"
    )

    blazerPlugins = $(
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
    )

    $allPlugins = $smallPlugins.Clone()
    $allPlugins += "AasxPluginWebBrowser"

    #function MakePackage($identifier)
    function MakePackage($identifier, $plugins)
    {
        $destinationDir = Join-Path $outputDir $identifier

        Write-Host ("Making the package $($identifier|ConvertTo-Json) to: " +
            $destinationDir)

        Write-Host "* Packaging to: $destinationDir"
        New-Item -ItemType Directory -Force -Path $destinationDir|Out-Null

        ##
        # AASX Package Explorer
        ##

        Write-Host "* Copying AasxPackageExplorer to: $destinationDir"
        Copy-Item `
            -Path (Join-Path $buildDir "AasxPackageExplorer") `
            -Recurse `
            -Destination $destinationDir

        $aasxPEDir = Join-Path $destinationDir "AasxPackageExplorer"

        # We need to include the backup directory in the release as the package explorer
        # expects it during runtime.
        #
        # See the default option: AasxPackageExplorer.options.json#/BackupDir.
        $backupDir = Join-Path $aasxPEDir "backup"
        New-Item -ItemType Directory -Force -Path $backupDir|Out-Null

        Write-Host "* Copying README-packages.md to: $aasxPEDir"
        Copy-Item `
            -Path (Join-Path $PSScriptRoot "README-packages.md") `
            -Destination $aasxPEDir

        ##
        # Plug-ins
        ##

        $pluginsDir = Join-Path $aasxPEDir "plugins"
        New-Item -ItemType Directory -Force -Path $pluginsDir|Out-Null

        foreach ($plugin in $plugins)
        {
            Write-Host "* Copying $plugin to: $pluginsDir"
            Copy-Item `
                -Path (Join-Path $buildDir $plugin) `
                -Recurse `
                -Destination $pluginsDir
        }

        ##
        # Compress
        ##

        $archPath = Join-Path $outputDir "$identifier.zip"
        Write-Host "* Compressing: $archPath"
        Compress-Archive `
            -Path (Join-Path $destinationDir "AasxPackageExplorer") `
            -DestinationPath $archPath
    }

    function MakePackageBlazor($identifier, $plugins)
    {
        $destinationDir = Join-Path $outputDir $identifier
        $aasxBlazorDir = Join-Path $destinationDir "BlazorExplorer"

        Write-Host ("Making the package $($identifier|ConvertTo-Json) to: " +
            $destinationDir)

        Write-Host "* Packaging to: $destinationDir"
        New-Item -ItemType Directory -Force -Path $destinationDir|Out-Null

        ##
        # BlazorExplorer
        ##

        Write-Host "* Copying BlazorExlorer to: $destinationDir"
        Copy-Item `
            -Path (Join-Path $buildDir "BlazorExplorer") `
            -Recurse `
            -Destination $destinationDir

        ##
        # Plug-ins
        ##

        $pluginsDir = Join-Path $aasxBlazorDir "plugins"
        New-Item -ItemType Directory -Force -Path $pluginsDir|Out-Null

        foreach ($plugin in $plugins)
        {
            Write-Host "* Copying $plugin to: $pluginsDir"
            Copy-Item `
                -Path (Join-Path $buildDir $plugin) `
                -Recurse `
                -Destination $pluginsDir
        }

        ##
        # Compress
        ##

        $archPath = Join-Path $outputDir "$identifier.zip"
        Write-Host "* Compressing: $archPath"
        Compress-Archive `
            -Path (Join-Path $destinationDir "BlazorExplorer") `
            -DestinationPath $archPath
    }

    ##
    # Make packages
    ##

    MakePackage -identifier "aasx-package-explorer" -plugins $allPlugins

    MakePackage -identifier "aasx-package-explorer-small" #-plugins $smallPlugins

    MakePackageBlazor -identifier "aasx-package-explorer-blazorexplorer" -plugins $blazerPlugins

    MakePackageBlazor -identifier "aasx-package-explorer-blazorexplorer-small"

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