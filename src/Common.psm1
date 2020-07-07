<#
This module contains common functions for continuous integration.
#>

<#
.Synopsis
Join the path to the directory where build tools reside.
#>
function GetToolsDir
{
    return Join-Path (Split-Path $PSScriptRoot -Parent) "tools"
}

<#
.Synopsis
Search for MSBuild in the path and at expected locations using `vswhere.exe`.
#>
function FindMSBuild
{
    $msbuild = $null

    $msbuildCommand = Get-Command "MSBuild.exe" -ErrorAction SilentlyContinue
    $msbuildFailedSearches = @()
    if ($null -ne $msbuildCommand)
    {
        $msbuild = $msbuildCommand.Source
    }
    else
    {
        $vswherePath = "${Env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
        if (!(Test-Path $vswherePath))
        {
            throw "Could not find vswhere at: $vswherePath"
        }

        $ids = 'Community', 'Professional', 'Enterprise', 'BuildTools' `
            | ForEach-Object { 'Microsoft.VisualStudio.Product.' + $_ }

        $instance = & $vswherePath -latest -products $ids -requires Microsoft.Component.MSBuild -format json `
            | Convertfrom-Json `
            | Select-Object -first 1

        $msbuildPath = Join-Path $instance.installationPath 'MSBuild\15.0\Bin\MSBuild.exe'
        if (Test-Path $msbuildPath)
        {
            $msbuild = $msbuildPath
        }
        else
        {
            $msbuildFailedSearches += $msbuildPath

            $msbuildPath = Join-Path $instance.installationPath 'MSBuild\Current\Bin\MSBuild.exe'
            if (Test-Path $msbuildPath)
            {
                $msbuild = $msbuildPath
            }
            else
            {
                $msbuildFailedSearches += $msbuildPath
            }
        }
    }

    if (!$msbuild)
    {
        throw "Could not find MSBuild in PATH and at these locations: $( $msbuildFailedSearches -join ';' )"
    }

    return $msbuild
}

<#
.Synopsis
Asserts that dotnet is on the path.
#>
function AssertDotnet
{
    if (!(Get-Command "dotnet" -ErrorAction SilentlyContinue))
    {
        if ($null -eq $env:LOCALAPPDATA)
        {
            throw "dotnet could not be found in the PATH."
        }
        else
        {
            throw "dotnet could not be found in the PATH. Look if you could find it, e.g., in " + `
               "$( Join-Path $env:LOCALAPPDATA "Microsoft\dotnet" ) and add it to PATH."
        }
    }
}

function FindDotnetToolVersion($PackageID) {
    AssertDotnet

    $version = ''

    $lines = (dotnet tool list)|Select-Object -Skip 2
    $lines += (dotnet tool list -g)|Select-Object -Skip 2
    ForEach ($line in $( $lines -split "`r`n" ))
    {
        $parts = $line -Split '\s+'
        if ($parts.Count -lt 3)
        {
            throw "Expected at least 3 columns in a line of `dotnet tool list`, got output: ${lines}"
        }

        $aPackageID = $parts[0]
        $aPackageVersion = $parts[1]

        if ($aPackageID -eq $PackageID)
        {
            $version = $aPackageVersion
            break
        }
    }

    return $version
}

<#
.Synopsis
Check the version of the given dotnet tool.
 #>
function AssertDotnetToolVersion($PackageID, $ExpectedVersion) {
    AssertDotnet

    $version = FindDotnetToolVersion -PackageID $PackageID
    if ($version -eq '')
    {
        throw "No $PackageID could be found. Have you installed it? " + `
               "Check the list of the installed dotnet tools with: " + `
               "`dotnet tool list` and `dotnet tool list -g`."
    }
    else
    {
        if ($version -ne $ExpectedVersion)
        {
            throw "Expected $PackageID version $ExpectedVersion, but got: $version;" + `
                   "Check the list of the installed dotnet tools with: " + `
                   "`dotnet tool list` and `dotnet tool list -g`."
        }
        # else: the version is correct.
    }
}

<#
.Synopsis
Check the version of dotnet-format so that the code is always formatted in the same manner.
#>
function AssertDotnetFormatVersion
{
    AssertDotnetToolVersion -packageID "dotnet-format" -expectedVersion "3.3.111304"
}

<#
.Synopsis
Check the version of dead-csharp so that the dead code is always detected in the same manner.
#>
function AssertDeadCsharpVersion
{
    AssertDotnetToolVersion -packageID "deadcsharp" -expectedVersion "1.0.0-beta4"
}

function FindInspectCode
{
    $toolsDir = GetToolsDir
    $inspectcode = Join-Path $toolsDir "JetBrains.ReSharper.CommandLineTools.2020.1.2\tools\inspectcode.exe"

    if (!(Test-Path $inspectcode))
    {
        throw "The inspectcode.exe could not be found at: $inspectcode;" + `
               "did you install it with nuget " + `
               "(see $( Join-Path $PSScriptRoot "InstallBuildDependencies.ps1" ))?"
    }
    return $inspectcode
}

function FindNunit3Console
{
    $toolsDir = GetToolsDir
    $nunit3Console = Join-Path $toolsDir "NUnit.ConsoleRunner.3.11.1\tools\nunit3-console.exe"
    if (!(Test-Path $nunit3Console))
    {
        throw "The nunit3-console.exe could not be found at: $nunit3Console; " + `
               "did you install or restore the dependencies of the solution?"
    }

    return $nunit3Console
}

function FindOpenCoverConsole
{
    $toolsDir = GetToolsDir
    $openCoverConsole = Join-Path $toolsDir "OpenCover.4.7.922\tools\OpenCover.Console.exe"
    if (!(Test-Path $openCoverConsole))
    {
        throw "The OpenCover.Console.exe could not be found at: $openCoverConsole;" + `
               "did you install it with nuget " + `
               "(see $( Join-Path $PSScriptRoot "InstallBuildDependencies.ps1" ))?"
    }
    return $openCoverConsole
}

function FindReportGenerator
{
    $toolsDir = GetToolsDir
    $reportGenerator = Join-Path $toolsDir "ReportGenerator.4.6.0\tools\net47\ReportGenerator.exe"
    if (!(Test-Path $reportGenerator))
    {
        throw "The ReportGenerator.exe could not be found at: $reportGenerator;" + `
               "did you install it with nuget " + `
               "(see $( Join-Path $PSScriptRoot "InstallBuildDependencies.ps1" ))?"
    }
    return $reportGenerator
}

function CreateAndGetArtefactsDir
{
    $repoRoot = Split-Path $PSScriptRoot -Parent
    $artefactsDir = Join-Path $repoRoot "artefacts"
    New-Item -ItemType Directory -Force -Path "$artefactsDir"|Out-Null
    return $artefactsDir
}

Export-ModuleMember -Function `
    GetToolsDir, `
     AssertDotnet, `
     AssertDotnetToolVersion, `
     AssertDotnetFormatVersion, `
     AssertDeadCsharpVersion, `
     FindMSBuild, `
     FindInspectCode, `
     FindNunit3Console, `
     FindOpenCoverConsole, `
     FindReportGenerator, `
     CreateAndGetArtefactsDir
