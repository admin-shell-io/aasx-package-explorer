#!/usr/bin/env pwsh
<#
.SYNOPSIS
    This script checks the headers of the source files and makes sure that the
    copyright notices and references to the licenses are in place.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet


function Main
{
    AssertDotnet
    Set-Location $PSScriptRoot

    # We exclude some projects and files from the check as we do not re-license
    # the source code, but merely include them in our code base from external
    # sources.

    & dotnet.exe run --project CheckHeadersScript `
        --inputs "**\*.cs" "**\*.xaml" `
        --excludes `
            "packages\**" `
            "**\obj\**" `
            "**\bin\**" `
            "**\AssemblyInfo.cs" `
            "**\Properties\*.Designer.cs" `
            "**\DocTest*.cs" `
            "*.Tests\**\*.cs" `
            "*.GuiTests\**\*.cs" `
            "AasxOpenidClient\*.cs" `
            "AasxUaNetServer\AasxServer\AasNodeManager.cs" `
            "AasxPluginUaNetClient\Plugin.cs" `
            "AasxPluginUaNetClient\UASampleClient.cs" `
            "AasxUaNetServer\Base\*.cs" `
            "AasxUaNetServer\SampleServer.cs" `
            "AasxUaNetServer\SampleServer.SampleModel.cs" `
            "AasxUaNetServer\SampleServer.UserAuthentication.cs" `
            "AasxUaNetServer\UaServerWrapper.cs" `
            "MsaglWpfControl\*" `
            "CheckHeadersScript\*.cs" `
            "CheckScript\*.cs" `
            "AasxIntegrationBaseWpf\OriginalResources\*.xaml" `
            "WpfMtpControl\Resources\PNID_DIN_EN_ISO_10628.xaml" `
            "WpfMtpControl\Resources\PNID_Festo.xaml" `
            "WpfXamlTool\Resources\preset0.xaml" `
            "WpfXamlTool\Resources\preset1.xaml" `
            "AasxFileServerRestLibrary\**" `
            "es6numberserializer\**" `
            "jsoncanonicalizer\**" `
            "AasCore.Aas3_0/**" `
            "AasxCsharpLib_bkp/**" `
            "AasxServer.DomainModelV3_0_RC02/**"

    if($LASTEXITCODE -ne 0)
    {
        throw "The CheckHeadersScript failed, see above."
    }

    Write-Host "All the inspected headers were OK."
}

$previousLocation = Get-Location; try
{
    Main
}
finally
{
    Set-Location $previousLocation
}
