# This script installs the dependencies necessary to build the solution.
# The script uses nuget and the dependencies are stored in the packages/ 
# subdirectory.

if ((Get-Command "nuget.exe" -ErrorAction SilentlyContinue) -eq $null) 
{ 
   throw "Unable to find nuget.exe in your PATH"
}

cd $PSScriptRoot

Write-Host "Installing .NET compilers 2.10.0 ..."
nuget install Microsoft.NET.Compilers -Version 2.10.0

Write-Host "Restoring packages for the solution ..."
nuget.exe restore AasxPackageExplorer.sln
