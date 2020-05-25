<# 
This script runs all the unit tests specified in `.\tests.nunit` file.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function FindNunit3Console

$nunit3Console = FindNunit3Console

cd $PSScriptRoot
$nunitProjectPath = Join-Path $PSScriptRoot "tests.nunit" 
Write-Host "Running the tests specified in: $nunitProjectPath"
& $nunit3Console $nunitProjectPath

if(!$?) {
    throw "The unit test(s) failed."
}