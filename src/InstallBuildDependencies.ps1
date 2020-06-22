# This script installs the dependencies necessary to build the solution.
# The script uses nuget and the dependencies are stored in the packages/ 
# subdirectory.

if ((Get-Command "nuget.exe" -ErrorAction SilentlyContinue) -eq $null)
{ 
   throw "Unable to find nuget.exe in your PATH"
}

$toolsDir = Join-Path (Split-Path $PSScriptRoot -Parent) "tools"
New-Item -ItemType Directory -Force -Path $toolsDir

Write-Host "Installing the tools in: $toolsDir"

Write-Host "Installing .NET compilers 2.10.0 ..."
nuget install Microsoft.NET.Compilers -Version 2.10.0 `
    -OutputDirectory $toolsDir

Write-Host "Installing Nunit Console Runner ..."
nuget install NUnit.ConsoleRunner -Version 3.11.1 `
    -OutputDirectory $toolsDir

nuget install NUnit.Extension.NUnitProjectLoader -Version 3.6.0 `
    -OutputDirectory $toolsDir

Write-Host "Installing OpenCover ..."
nuget install OpenCover -Version 4.7.922 -OutputDirectory $toolsDir
nuget install ReportGenerator -Version 4.6.0 -OutputDirectory $toolsDir

Write-Host "Installing Resharper CLI ..."
nuget install JetBrains.ReSharper.CommandLineTools -Version 2020.1.2 -OutputDirectory $toolsDir

Write-Host "Restoring packages for the solution ..."
cd $PSScriptRoot
nuget.exe restore AasxPackageExplorer.sln
