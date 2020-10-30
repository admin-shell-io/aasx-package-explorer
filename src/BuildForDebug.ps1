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
    AssertDotnet,      `
    GetArtefactsDir

function Main
{
    AssertDotnet

    $configuration = "Debug"

    $outputPath = Join-Path $( GetArtefactsDir ) "build" `
        | Join-Path -ChildPath $configuration

    Set-Location $PSScriptRoot

    if (!$clean)
    {
        Write-Host "Building and publishing to: $outputPath"

        New-Item -ItemType Directory -Force -Path $outputPath|Out-Null

        & dotnet.exe publish `
            --configuration $configuration `
            --runtime win-x64 `
            --output $outputPath

        $exitCode = $LASTEXITCODE
        Write-Host "dotnet publish exit code: $exitCode"
        if ($exitCode -ne 0)
        {
            throw "dotnet publish failed."
        }
    }
    else
    {
        Write-Host "Cleaning up the build ..."

        & dotnet.exe clean `
            --configuration $configuration `
            --runtime win-x64

        $exitCode = $LASTEXITCODE
        Write-Host "dotnet clean exit code: $exitCode"
        if ($exitCode -ne 0)
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
