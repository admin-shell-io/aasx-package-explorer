<#
.SYNOPSIS
This script checks that there is no dead code in the comments.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet,  `
    AssertDeadCsharpVersion

function Main
{
    AssertDeadCsharpVersion

    Set-Location $PSScriptRoot
    Write-Host "Looking for the dead code in comments with dead-csharp ..."

    dotnet dead-csharp --inputs "**/*.cs" --excludes "**/obj/**" "packages/**" "**/Properties/**" "AasxFileServerRestLibrary/**" "es6numberserializer\**" "jsoncanonicalizer\**" "AasxCsharpLib_bkp/**" "AasCore.Aas3_0/**" "AasxServer.DomainModelV3_0_RC02/**"
    Read-Host -Prompt "Press Enter to exit"
    if($LASTEXITCODE -ne 0)
    {
        throw (
            "The dead-csharp check failed. " +
            "Please have a close look at the output above, " +
            "in particular the lines prefixed with `"FAIL`"."
        )
    }
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
