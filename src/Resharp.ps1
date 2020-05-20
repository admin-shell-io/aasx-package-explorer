# This script runs Resharper tools (InspectCode, dupFinder and CleanupCode) to improve
# the quality of the code base.
#
# This script is expected to be run after the solution has been built.

$inspectcode = ''
if ((Get-Command "inspectcode.exe" -ErrorAction SilentlyContinue) -ne $null) 
{ 
   $inspectcode = 'inspectcode.exe'
} else {
    $resharperDir="${Env:ProgramFiles(x86)}\resharper.2020.1.2"
    $inspectcode = Join-path $resharperDir 'inspectcode.exe'
    if(!(Test-Path $inspectcode)) {
        throw 'Resharper inspectcode.exe could not be found neither in PATH nor at: $inspectcode';
    }
}

cd $PSScriptRoot

# InspectCode passes over the properties to MSBuild,
# see https://www.jetbrains.com/help/resharper/InspectCode.html#msbuild-related-parameters
& $inspectcode `
    --properties:Configuration=Debug `
    --properties:Platform=x64 `
    "-o=resharper-code-inspection.xml" `
    AasxPackageExplorer.sln
 
$fails = @()

[xml]$inspection = Get-Content "resharper-code-inspection.xml"
$issueCount = $inspection.SelectNodes('//Issue').Count
if( $issueCount -ne 0 ) {
    $fails += "* There are $issueCount InspectCode issue(s). " + `
        "The issues are stored in: $PSScriptRoot\resharper-code-inspection.xml"
}

if($fails.Count -ne 0) {
    $parts = @("Resharping failed:") + $fails
    Write-Error ($parts -join "`r`n") -ErrorAction Stop
}
