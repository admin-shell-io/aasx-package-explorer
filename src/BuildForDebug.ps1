param(
    [Parameter(HelpMessage = "If set, cleans up the previous build instead of performing a new one")]
    [switch]
    $clean = $false,
    [Parameter(HelpMessage = "If set, builds and publishes only the given project")]
    [string]
    $project = $null
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

        $dotnetArgs = @(
            "publish",
            "--configuration", $configuration,
            "--runtime", "win-x64",
            "--output", $outputPath
        )

        if($null -ne $project)
        {
            $dotnetArgs += $project
        }

        & dotnet $dotnetArgs

        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0)
        {
            throw "dotnet publish failed with the exit code: $exitCode"
        }
    }
    else
    {
        Write-Host "Cleaning up the build ..."

        & dotnet.exe clean `
            --configuration $configuration `
            --runtime win-x64

        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0)
        {
            throw "dotnet clean failed with the exit code: $exitCode"
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
