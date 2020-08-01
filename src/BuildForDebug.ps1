param(
    [Parameter(HelpMessage = "If set, cleans up the previous build instead of performing a new one")]
    [switch]
    $clean = $false
)

<#
.DESCRIPTION
This script builds the solution for debugging (manual or automatic testing).

It is expected that you installed the dev dependencies as well as
solution dependencies (see Install*.ps1 scripts).
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    FindMSBuild, `
    GetArtefactsDir

function Main
{
    $msbuild = FindMSBuild

    Write-Host "Using MSBuild from: $msbuild"

    $configuration = "Debug"

    $outputPath = Join-Path $(GetArtefactsDir) "build" `
        | Join-Path -ChildPath $configuration

    Set-Location $PSScriptRoot

    if(!$clean)
    {
        Write-Host "Building to: $outputPath"

        New-Item -ItemType Directory -Force -Path $outputPath|Out-Null

        & $msbuild `
            "/p:OutputPath=$outputPath" `
            "/p:Configuration=$configuration" `
            "/p:Platform=x64" `
            "/maxcpucount"

        $buildExitCode = $LASTEXITCODE
        Write-Host "MSBuild exit code: $buildExitCode"
        if ($buildExitCode -ne 0)
        {
            throw "MSBuild failed."
        }
    }
    else
    {
        Write-Host "Cleaning up the build ..."

        & $msbuild "/p:Configuration=$configuration" "/t:Clean"

        $buildExitCode = $LASTEXITCODE
        Write-Host "MSBuild exit code: $buildExitCode"
        if ($buildExitCode -ne 0)
        {
            throw "MSBuild failed."
        }

        if(Test-Path $outputPath)
        {
            Write-Host "Removing: $outputPath"
            Remove-Item -Recurse -Force $outputPath
        }
    }
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
