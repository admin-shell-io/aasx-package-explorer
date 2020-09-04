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

function Main
{
    if ($null -eq (Get-Command "git" -ErrorAction SilentlyContinue))
    {
        throw "Unable to find 'git' in your PATH"
    }

    Set-Location $PSScriptRoot

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
        & ./OpinionatedCommitMessage.ps1 `
            -message $message `
            -pathToAdditionalVerbs AdditionalVerbsInImperativeMood.txt
    }
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
