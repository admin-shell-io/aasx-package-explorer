#!/usr/bin/env pwsh
<#
.SYNOPSIS
This script checks that the C# files are "bite sized": no long lines, not too long.
#>

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnetToolVersion

function Main {
    AssertDotnetToolVersion -PackageID "BiteSized" -ExpectedVersion "1.0.0-beta3"

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
            "AasxToolkit/Program.cs" `
            "AasxPluginExportTable/ExportTableFlyout.xaml.cs" `
            "MsaglWpfControl/GraphViewer.cs" `
            "MsaglWpfControl/VEdge.cs" `
            "MsaglWpfControl/VNode.cs" `
        --max-lines-in-file 100000 `
        --max-line-length 120 `
        --ignore-lines-matching '[a-z]+://[^ \t]+$'

    if($LASTEXITCODE -ne 0)
    {
        throw (
            "The bite-sized check failed. " +
            "Please have a close look at the output above, " +
            "in particular the lines prefixed with `"FAIL`"."
        )
    }
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
