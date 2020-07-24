<#
.SYNOPSIS
This script generates the documentation for developers in the docdev directory.
#>

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    GetToolsDir, `
    CreateAndGetArtefactsDir


function Main
{
    Set-Location $PSScriptRoot

    $toolsDir = GetToolsDir
    $docfxExe = Join-Path $toolsDir "docfx.console.2.56.1" `
        | Join-Path -ChildPath "tools" `
        | Join-Path -ChildPath "docfx.exe"

    if(!(Test-Path -Path $docfxExe))
    {
        throw ("The docfx.exe could not be found: $docfxExe; " + `
            "Did you install it using InstallDocdevDependencies.ps1?")
    }

    $artefactsDir = CreateAndGetArtefactsDir

    $repoDir = Split-Path -Parent $PSScriptRoot
    $docfxProjectDir = Join-Path $repoDir "docdev" `
        | Join-Path -ChildPath "docfx_project"

    Set-Location $docfxProjectDir
    & $docfxExe "docfx.json"
    if($LASTEXITCODE -ne 0)
    {
        throw "docfx failed. See above for error logs."
    }

    $siteDir = Join-Path $artefactsDir "gh-pages" `
        | Join-Path -ChildPath "devdoc"

    Write-Host "The documentation has been generated to: '$siteDir'"
    Write-Host "You can serve it locally with:"
    Write-Host "'$docfxExe' serve '$siteDir'"
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
