if(!(Test-Path env:DOTNET_ROOT)) {
    $dotnetRoot = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet"
    if(!(Test-Path $dotnetRoot)) {
        throw "Expected dotnet root to exist, but it does not: $dotnetRoot"
    }

    Write-Host "Setting DOTNET_ROOT environment variable to: $dotnetRoot"
    [Environment]::SetEnvironmentVariable(
        "DOTNET_ROOT",
        $dotnetRoot)
}

$dotnetFormatPath=Join-Path $env:UserProfile ".dotnet\tools\dotnet-format.exe"

cd $PSScriptRoot

& $dotnetFormatPath
