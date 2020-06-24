#!/usr/bin/env pwsh
<#
This script checks that the C# files are "bite sized": no long lines, not too long.
#>

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnetToolVersion

function Main {
    AssertDotnetToolVersion -PackageID "BiteSized" -ExpectedVersion "1.0.0-beta1"

    Set-Location $PSScriptRoot

    # Exclude:
    # * Auto-generated files
    # * Files containing too many hard-wired strings
    # * External, third-party files

    dotnet bite-sized `
        --inputs "**/*.cs" `
        --excludes `
            "**/obj/**" `
            "packages/**" `
            "**/Settings.Designer.cs" `
            "**/Resources.Designer.cs" `
            "AasxGenerate/Program.cs" `
            "AasxPluginExportTable/ExportTableFlyout.xaml.cs" `
            "MsaglWpfControl/GraphViewer.cs" `
            "MsaglWpfControl/VEdge.cs" `
            "MsaglWpfControl/VNode.cs" `
        --max-lines-in-file 100000 `
        --max-line-length 120

    if($LASTEXITCODE -ne 0)
    {
        throw "The bite-sized check failed."
    }
}

Main