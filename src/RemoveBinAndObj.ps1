#!/usr/bin/env pwsh

param(
    [Parameter(
            HelpMessage = "If set, deletes the directories without any prompt")]
    [switch]
    $Force = $false
)

<#
.DESCRIPTION
This script removes recursively all `bin` and `obj` directories beneath `src`
directory.

You often need to remove these directories manually in case of clean builds
or substantial changes to the C# solution (*e.g.*, converting csproj from
legacy to SDK style).
#>

function Main
{
    Set-Location $PSScriptRoot

    $binDirs = Get-ChildItem `
            -Path $PSScriptRoot `
            -Filter "bin" `
            -Directory `
            -Recurse `
            -ErrorAction SilentlyContinue `
            -Force | ForEach-Object{ $_.FullName } |Sort-Object

    $objDirs = Get-ChildItem `
            -Path $PSScriptRoot `
            -Filter "obj" `
            -Directory `
            -Recurse `
            -ErrorAction SilentlyContinue `
            -Force | ForEach-Object{ $_.FullName } |Sort-Object

    $morituri = $binDirs + $objDirs

    if (!$Force)
    {
        Write-Host "The following directories will be deleted:"
        foreach ($path in $morituri)
        {
            Write-Host $path
        }

        Write-Host
        Write-Host (
        "All the temporary data (such as files temporarily copied " +
                "for debugging etc.) will be also lost!")
        Write-Host
        $confirmation = Read-Host "Are you sure you want to proceed (y/n)"
        if ($confirmation -ne 'y')
        {
            return
        }
    }

    foreach($path in $morituri)
    {
        Write-Host "Deleting: $path"
        Remove-Item -Path $path -Recurse -Force
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
