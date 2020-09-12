param(
    [Parameter(HelpMessage = "If set, only downloads the dependencies without actually installing them.")]
    [switch]
    $DryRun = $false
)

<#
.DESCRIPTION
This script installs the dependencies necessary for building and developing the solution.
The script is expected to run only once per development machine.
The dependencies include, for example, Visual Studio Build Tools and nuget.
#>

$ErrorActionPreference = "Stop"

function Main
{
    [string[]]$steps = @( )

    $installationDir = Join-Path (Split-Path $PSScriptRoot -Parent) "installation-dev-dependencies"

    Write-Host "Installation directory is: $installationDir"
    New-Item -ItemType Directory -Force -Path $installationDir

    ##
    # VS Buildtools
    ##

    $url = 'https://aka.ms/vs/16/release/vs_buildtools.exe'
    $targetPath = Join-Path $installationDir "vs_buildtools.exe"
    if (!(Test-Path $targetPath))
    {
        Write-Host "Downloading Visual Studio build tools 16 from $url to: $installationDir"
        Invoke-WebRequest $url -OutFile $targetPath
    }
    else
    {
        Write-Host "File exists, not re-downloading: $targetPath"
    }

    $cmd = Join-Path $installationDir "vs_buildtools.exe"
    # See https://docs.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-build-tools for
    # component IDs
    [string[]]$cmdArgs = @(
           "--add", "Microsoft.VisualStudio.Workload.MSBuildTools",
           "--add", "Microsoft.Net.Component.4.6.1.SDK",
           "--add", "Microsoft.Net.Component.4.6.1.TargetingPack",
           "--add", "Microsoft.NetCore.Component.Runtime.3.1",
           "--add", "Microsoft.NetCore.Component.SDK",
           "--add", "Microsoft.VisualStudio.Component.NuGet.BuildTools",
           "--quiet", "--norestart")

    if ($true -eq $DryRun)
    {
        $steps += "'$cmd' $( $cmdArgs -Join " " )"
    }
    else
    {
        & $cmd $cmdArgs

        $vswherePath = "${Env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
        Write-Host "Using vswhere to locate MS Build tools: $vswherePath"

        $ids = 'Community', 'Professional', 'Enterprise', 'BuildTools' `
            | ForEach-Object { 'Microsoft.VisualStudio.Product.' + $_ }

        $instance = & $vswherePath -latest -products $ids -requires Microsoft.Component.MSBuild -format json `
            | Convertfrom-json `
            | Select-Object -first 1

        Write-Host "MSBuild was installed to: $($instance.installationPath)"
    }

    ##
    # Nuget
    ##

    $targetPath = Join-Path $installationDir "nuget.exe"
    if (!(Test-Path $targetPath))
    {
        Write-Host "Downloading latest nuget to: $nugetDir"
        Invoke-WebRequest `
            https://dist.nuget.org/win-x86-commandline/latest/nuget.exe  `
            -OutFile $targetPath
    }
    else
    {
        Write-Host "File exists, not re-downloading: $targetPath"
    }

    $nugetDir = Join-Path ${Env:ProgramFiles(x86)} "nuget"
    if($true -eq $DryRun)
    {
        $steps += "Copy nuget from $targetPath to: $nugetDir"
        $steps += "Add nuget directory to PATH: $nugetDir"
    }
    else
    {
        Write-Host "Creating nuget directory: $nugetDir"
        New-Item -ItemType Directory -Force -Path $nugetDir

        Write-Host "Copyng nuget from $targetPath to: $nugetDir"
        $destinationPath = Join-Path $nugetDir "nuget.exe"
        if(!(Test-Path $destinationPath))
        {
            Copy-Item -Path $targetPath -Destination $destinationPath
        }
        else
        {
            Write-Host "File nuget.exe already exists, not overwriting: $destinationPath"
        }

        Write-Host "Adding nuget directory to system path: $nugetDir"
        [Environment]::SetEnvironmentVariable(
                "Path",
                [Environment]::GetEnvironmentVariable("Path", [EnvironmentVariableTarget]::Machine) + ";$nugetDir",
                [EnvironmentVariableTarget]::Machine)
    }

    ##
    # dotnet
    ##

    $targetPath = Join-Path $installationDir "dotnet-install.ps1"
    if (!(Test-Path $targetPath))
    {
        Write-Host "Downloading dotnet-install.ps1 to: $installationDir"
        Invoke-WebRequest `
            https://dot.net/v1/dotnet-install.ps1 `
            -OutFile $installationDir\dotnet-install.ps1
    }
    else
    {
        Write-Host "File exists, not re-downloading: $targetPath"
    }

    $cmd = $targetPath
    if ($true -eq $DryRun)
    {
        $steps += "'$cmd' -Version 3.1.202"
    }
    else
    {
        # (Marko Ristin) Passing in the $cmdArgs just did not work in Powershell.
        # I am at loss why it always caused "Can not transform argument ..."
        & $cmd -Version 3.1.202
    }

    if ($true -eq $DryRun)
    {
        $steps += "'$cmd' -Runtime dotnet -Version 3.1.4"
    }
    else
    {
        # (Marko Ristin) Passing in the $cmdArgs just did not work in Powershell.
        # I am at loss why it always caused "Can not transform argument ..."
        & $cmd -Runtime dotnet -Version 3.1.4
    }

    if ($true -eq $DryRun)
    {
        Write-Host ""
        Write-Host "This was a just a dry run. Here are the steps if you want to execute them manually:"
        Write-Host ""
        foreach ($step in $steps)
        {
            Write-Host "* $step"
        }
    }
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
