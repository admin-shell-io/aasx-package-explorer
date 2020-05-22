# This script builds the solution. 
#
# It is expected that you installed 
# the dev dependencies (with InstallDevDependenciesOnMV.ps1 or manually) 
# as well as solution dependencies (InstallBuildDependencies.ps1 or manually).

$vswherePath = "${Env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (!(Test-Path $vswherePath)) {
    throw "Could not find vswhere at: $vswherePath"
}


$ids = 'Community', 'Professional', 'Enterprise', 'BuildTools' | foreach { 'Microsoft.VisualStudio.Product.' + $_ }
$instance = & $vswherePath -latest -products $ids -requires Microsoft.Component.MSBuild -format json `
    | Convertfrom-Json `
    | Select-Object -first 1
$msbuildPath = Join-Path $instance.installationPath 'MSBuild\Current\Bin\MSBuild.exe' 
if (!(Test-Path $msbuildPath)) {
    throw "Could not find MSBuild. Expected at: $msbuildPath"
}

Write-Host "Using MSBuild from: $msbuildPath"

cd $PSScriptRoot
& $msbuildPath --% /p:Configuration=Debug /p:Platform=x64 