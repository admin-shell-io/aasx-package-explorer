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
    if ($null -eq $msbuildCommand)
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
        throw "Could not find MSBuild. Searched in PATH and at the following locations: $( $msbuildFailedSearches -join ';' )"
    }

    return $msbuild
}


<#
.Synopsis
Find `inspectcode.exe`eiher on PATH or at expected location.
#>
function FindInspectCode
{
    $inspectcode = ''
    if ($null -eq (Get-Command "inspectcode.exe" -ErrorAction SilentlyContinue))
    {
        $inspectcode = 'inspectcode.exe'
    }
    else
    {
        $resharperDir = "${Env:ProgramFiles(x86)}\resharper.2020.1.2"
        $inspectcode = Join-path $resharperDir 'inspectcode.exe'
        if (!(Test-Path $inspectcode))
        {
            throw 'Resharper inspectcode.exe could not be found neither in PATH nor at: $inspectcode; " + `
                "have you installed it (https://www.jetbrains.com/help/resharper/InspectCode.html)?';
        }
    }

    return $inspectcode
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
            throw "dotnet could not be found in the PATH. Look if you could find it, e.g., in " +   `
              "$( Join-Path $env:LOCALAPPDATA "Microsoft\dotnet" ) and add it to PATH."
        }
    }
}

<#
.Synopsis
Check the version of dotnet-format.

.Description
Check the version of dotnet-format. 
This is important so that we always format the code in the same manner.
#>
function AssertDotnetFormatVersion
{
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

        $packageID = $parts[0]
        $packageVersion = $parts[1]

        if ($packageID -eq "dotnet-format")
        {
            $version = $packageVersion
        }
    }

    $expectedVersion = "3.3.111304"
    if ($version -eq '')
    {
        throw "No dotnet-format could be found. Have you installed it " +   `
              "(https://github.com/dotnet/format)? " +   `
              "Check the list of the installed dotnet packages with: " +   `
              "`dotnet tool list` and `dotnet tool list -g`."
    }
    else
    {
        if ($version -ne $expectedVersion)
        {
            throw "Expected dotnet-format version $expectedVersion, but got: $version;" +   `
                  "Check the list of the installed dotnet packages with: " +   `
                  "`dotnet tool list` and `dotnet tool list -g`."
        }
        else
        {
            # The version is correct.
        }
    }
}

function FindNunit3Console
{
    $toolsDir = GetToolsDir
    $nunit3Console = Join-Path $toolsDir "NUnit.ConsoleRunner.3.11.1\tools\nunit3-console.exe"
    if (!(Test-Path $nunit3Console))
    {
        throw "The nunit3-console.exe could not be found at: $nunit3Console; " +   `
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
        throw "The OpenCover.Console.exe could not be found at: $openCoverConsole;" +   `
              "did you install it with nuget " +   `
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
        throw "The ReportGenerator.exe could not be found at: $reportGenerator;" +   `
              "did you install it with nuget " +   `
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
    GetToolsDir,   `
    AssertDotnet,   `
    AssertDotnetFormatVersion,   `
    FindMSBuild,   `
    FindInspectCode,   `
    FindNunit3Console,   `
    FindOpenCoverConsole,   `
    FindReportGenerator,   `
    CreateAndGetArtefactsDir
