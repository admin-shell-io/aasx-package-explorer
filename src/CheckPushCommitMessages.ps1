#!/usr/bin/env pwsh

<#
.SYNOPSIS
This script checks that the commit messages of the (potential) git push correspond to the convention.

.DESCRIPTION
This script uses the opinionanted-commit-message (https://github.com/mristin/opinionated-commit-message) for
local usage (see https://github.com/mristin/opinionated-commit-message#local-usage).

We use `git` CLI to obtain the commit messages contained in the next (potential) push.
The local `HEAD` is compared against the `origin/master`.
#>

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    CreateAndGetArtefactsDir


function Main
{
    if ($null -eq (Get-Command "git" -ErrorAction SilentlyContinue))
    {
        throw "Unable to find 'git' in your PATH"
    }

    Set-Location $PSScriptRoot

    $artefactsDir = CreateAndGetArtefactsDir
    $reportPath = Join-Path $artefactsDir "CheckPushCommitMessages.txt"
    if (Test-Path $reportPath)
    {
        Remove-Item -Path $reportPath -Force
    }

    # Get commit hashes not available in the master
    $hashesText = git log 'origin/master..HEAD' '--format=format:%H'|Out-String

    [string[]]$hashes = $()
    foreach($line in ($hashesText -Split "`n"))
    {
        $trimmed = $line.Trim()
        if($trimmed.Length -gt 0)
        {
            $hashes += $trimmed
        }
    }

    foreach($hash in $hashes)
    {
        $message = (git log --format=%B -n 1 $hash|Out-String).TrimEnd()
        Write-Host "--- Verifying the message: ---"
        Write-Host $message
        Write-Host "---"
        try
        {
            & ./OpinionatedCommitMessage.ps1 `
                -message $message `
                -pathToAdditionalVerbs AdditionalVerbsInImperativeMood.txt
        }
        catch
        {
            Write-Host "---"
            Write-Host "One or more commit messages failed the check, but you can ignore them."
            Write-Host
            Write-Host (
                "Mind that we only check the title and the description " +
                "of the pull request in our CI."
            )
        }
    }
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
