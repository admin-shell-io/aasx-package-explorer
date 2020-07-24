<#
.SYNOPSIS
This script checks that all the TODOs in the code follow the convention.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet,  `
    AssertOpinionatedCsharpTodosVersion

function Main
{
    AssertOpinionatedCsharpTodosVersion

    Set-Location $PSScriptRoot
    Write-Host "Inspecting the TODOs in the code..."
    dotnet opinionated-csharp-todos `
        --inputs '**/*.cs' `
        --excludes 'packages/**' '**/obj/**' 'MsaglWpfControl/**'
    if($LASTEXITCODE -ne 0)
    {
        throw "Failed to validate the TODOs in the code."
    }
}

Push-Location
try { Main } finally { Pop-Location }
