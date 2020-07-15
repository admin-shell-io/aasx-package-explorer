# This script formats the code in-place.

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet, `
    AssertDotnetFormatVersion

AssertDotnet
AssertDotnetFormatVersion

cd $PSScriptRoot
dotnet format --exclude "**/DocTest*.cs"
