<#
.SYNOPSIS
This script runs a validation on schedma violations for all .AASX files in the directory "aasx-package-explorer\sample-aasx".
The sample files can be downloaded via "aasx-package-explorer\DownloadSamples.ps1".
#>

$ErrorActionPreference = "Stop"

function LogAndExecute($Expression)
{
    Write-Host "---"
    Write-Host "Running: $Expression"
    Write-Host "---"

    Invoke-Expression $Expression
}

function Main
{
    Get-ChildItem -File -Path ..\..\..\..\sample-aasx -Filter *.aasx | Foreach-Object {
		# Write-Host "$($_.Fullname)"
		$cmd = ".\AasxToolkit.exe load `"$($_.Fullname)`" check+fix save sample.xml validate sample.xml"
		Write-Host ""
		Write-Host -ForegroundColor Yellow "Executing: $cmd"
		Invoke-Expression $cmd
	}
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
