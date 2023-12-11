<#
.SYNOPSIS
This script checks the format of the code.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet,  `
    AssertDotnetFormatVersion,  `
    CreateAndGetArtefactsDir

function Main
{
    AssertDotnetFormatVersion

    Set-Location $PSScriptRoot
    Write-Host "Inspecting the code format with dotnet-format..."

    $artefactsDir = CreateAndGetArtefactsDir

    $reportPath = Join-Path $artefactsDir "dotnet-format-report.json"
   
    # MIHO: dotnet format seems to changed --check with --verify-no-changes
	# therefore try to detect
	$checkswitch = "--check"
	$fmthelp = dotnet format --help | Out-String
	if ($fmthelp.Contains("--verify-no-changes")) 
	{		
		$checkswitch = "--verify-no-changes"
	}

	Write-Host "Using dotnet format switch: $checkswitch"

    dotnet format $checkswitch --report $reportPath --exclude "**/DocTest*.cs"
    
    $formatReport = Get-Content $reportPath |ConvertFrom-Json
    $warn_count = 0
    $error_count = 0
    for($i=0 ; $i -lt $formatReport.Count; $i++) { 
        if($formatReport[$i].FileChanges[0].FormatDescription -like "*warning*"){
            $warn_count++
        }else{
            $error_count++
        }
    }

    Write-Host "found $warn_count warnings!"
    Write-Host "found $error_count errors!"

    if ($error_count -ge 1)
    {
        throw (
            "There are $( $formatReport.Count ) dotnet-format issue(s). " +
            "The report is stored in: $reportPath. " +
            "Please reformat the code with FormatCode.ps1."
        )
    }
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
