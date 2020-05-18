# This script installs the dependencies necessary for building and developing the solution.
# The script is expected to run only once per development machine.
# The dependencies include, for example, Git, Visual Studio 2017 Build Tools and nuget.

if(!$PSScriptRoot) {
    $defaultInstallationDir = Join-Path (Get-Location).path "installation"
} else {
    $defualtInstallationDir = Join-Path $PSScriptRoot "installation"
}

param ($installationDir=$defaultInstallationDir)

Write-Host "Installation directory is: $installationDir"


New-Item -ItemType Directory -Force -Path $installationDir

Write-Host "Downloading Git to: $installationDir"
Invoke-WebRequest `
    https://github.com/git-for-windows/git/releases/download/v2.26.2.windows.1/Git-2.26.2-64-bit.exe `
    -OutFile $installationDir\Git-2.26.2-64-bit.exe

Write-Host "Installing Git ..."
$gitDir="C:\Program Files\Git"
@"
[Setup]
Lang=default
Dir=$gitDir
Group=Git
NoIcons=0
SetupType=default
Components=icons,ext\reg\shellhere,assoc,assoc_sh
Tasks=
PathOption=Cmd
SSHOption=OpenSSH
CRLFOption=CRLFAlways
BashTerminalOption=ConHost
PerformanceTweaksFSCache=Enabled
UseCredentialManager=Enabled
EnableSymlinks=Disabled
EnableBuiltinDifftool=Disabled
"@ | Set-Content -Path $installationDir\git-config.inf
& "$installationDir\Git-2.26.2-64-bit.exe" /VERYSILENT /LOADINF="$installationDir\git-config.inf"

Write-Host "Adding Git to system path ..."
 [System.Environment]::SetEnvironmentVariable(`
    "Path", `
    [System.Environment]::GetEnvironmentVariable('Path', [System.EnvironmentVariableTarget]::Machine) + `
    ";$gitDir\cmd")

$url = 'https://aka.ms/vs/15/release/vs_buildtools.exe'
Write-Host "Downloading Visual Studio 2017 build tools from $url to: $installationDir"
Invoke-WebRequest $url -OutFile $installationDir\vs_buildtools.exe

& "$installationDir\vs_buildtools.exe" `
    --add Microsoft.VisualStudio.Workload.MSBuildTools `
    --add Microsoft.Net.Component.4.6.1.SDK `
    --add Microsoft.Net.Component.4.6.1.TargetingPack `
    --add Microsoft.NetCore.Component.Runtime.3.1 `
    --add Microsoft.NetCore.Component.SDK `
    --add Microsoft.VisualStudio.Component.NuGet.BuildTools `
    --quiet --norestart

$vswherePath = "${Env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$ids = 'Community', 'Professional', 'Enterprise', 'BuildTools' | foreach { 'Microsoft.VisualStudio.Product.' + $_ }
$instance = & $vswherePath -latest -products $ids -requires Microsoft.Component.MSBuild -format json `
    | Convertfrom-json `
    | Select-Object -first 1
$msbuildPath = Join-Path $instance.installationPath 'MSBuild\15.0\Bin\MSBuild.exe' 

Write-Host "MSBuild can be found at: $msbuildPath"

$nugetDir = "${Env:ProgramFiles(x86)}\nuget"
New-Item -ItemType Directory -Force -Path $nugetDir
Write-Host "Downloading latest nuget to: $nugetDir"
Invoke-WebRequest `
    https://dist.nuget.org/win-x86-commandline/latest/nuget.exe  `
    -OutFile $nugetDir\nuget.exe

Write-Host "Adding nuget to system path ..."
[Environment]::SetEnvironmentVariable(
    "Path",
    [Environment]::GetEnvironmentVariable("Path", [EnvironmentVariableTarget]::Machine) + ";$nugetDir",
    [EnvironmentVariableTarget]::Machine)

Write-Host "Downloading resharper to: $installationDir"
Invoke-WebRequest `
    https://download.jetbrains.com/resharper/ReSharperUltimate.2020.1.2/JetBrains.ReSharper.CommandLineTools.2020.1.2.zip `
    -OutFile $installationDir\JetBrains.ReSharper.CommandLineTools.2020.1.2.zip

$resharperDir="${Env:ProgramFiles(x86)}\resharper.2020.1.2"
Write-Host "Unzipping resharper to: $resharperDir"
Expand-Archive `
    -LiteralPath $installationDir\JetBrains.ReSharper.CommandLineTools.2020.1.2.zip `
    -DestinationPath $resharperDir

Write-Host "Downloading dotnet-install.ps1 to: $installationDir"
Invoke-WebRequest `
    https://dot.net/v1/dotnet-install.ps1 `
    -OutFile $installationDir\dotnet-install.ps1

& $installationDir\dotnet-install.ps1 -Version 3.1.202
& $installationDir\dotnet-install.ps1 -Runtime dotnet -Version 3.1.4

Write-Host "Install dotnet-format ..."
dotnet tool install --global dotnet-format --version 3.3.111304
