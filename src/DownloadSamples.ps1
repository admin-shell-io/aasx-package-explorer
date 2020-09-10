param(
    [Parameter(HelpMessage = "If set, cleans up the previously downloaded samples")]
    [switch]
    $clean = $false
)
<#
.SYNOPSIS
This script downloads samples AASXs from www.admin-shell-io.com.
#>

function Main
{
    Set-Location $PSScriptRoot

    $samplesDir = Join-Path (Split-Path $PSScriptRoot -Parent) "sample-aasx"

    if (!$clean)
    {
        New-Item -ItemType Directory -Force -Path $samplesDir|Out-Null

        $urlPrefix = "http://admin-shell-io.com/samples/aasx/"
        $sampleFilenames = @(
        "01_Festo.aasx",
        "02_Bosch.aasx",
        "03_Bosch.aasx",
        "04_Bosch.aasx",
        "05_Bosch.aasx",
        "06_Bosch.aasx",
        "07_PhoenixContact.aasx",
        "08_SchneiderElectric.aasx",
        "09_SchneiderElectric.aasx",
        "10_SchneiderElectric.aasx",
        "11_SchneiderElectric.aasx",
        "12_Pepperl+Fuchs.aasx",
        "13_DKE.aasx",
        "14_Siemens.aasx",
        "15_Siemens.aasx",
        "16_Lenze.aasx",
        "17_ABB.aasx",
        "18_Hitachi_HX_DigTyp40.aasx"
        )

        # Set up the default proxy. This is necessary for many enterprise
        # settings, as otherwise the connection with Invoke-WebRequest won't
        # work.
        #
        # See also: https://www.reddit.com/r/PowerShell/comments/77m4lj/ and
        # http://woshub.com/using-powershell-behind-a-proxy/

        $proxyAddress = [System.Net.WebProxy]::GetDefaultProxy().Address
        if ($null -ne $proxyAddress)
        {
            [system.net.webrequest]::defaultwebproxy = New-Object system.net.webproxy($proxyAddress)
            [system.net.webrequest]::defaultwebproxy.credentials = `
                [System.Net.CredentialCache]::DefaultNetworkCredentials
            [system.net.webrequest]::defaultwebproxy.BypassProxyOnLocal = $true
        }

        foreach($sampleFilename in $sampleFilenames)
        {
            $url = $urlPrefix + $sampleFilename
            $samplePath = Join-Path $samplesDir $sampleFilename

            if(Test-Path $samplePath)
            {
                Write-Host "File exists: $samplePath; skipping."
                continue
            }

            Write-Host "Downloading $url -> $samplePath"
            $samplePathTmp = $samplePath + ".DownloadSamples.ps1.temp"
            try
            {
                Invoke-WebRequest -Uri $url -OutFile $samplePathTmp
                Move-Item -Path $samplePathTmp -Destination $samplePath
            }
            catch
            {
                if ($null -eq $proxyAddress)
                {
                    $proxyMessage = "no default proxy could be inferred by the script"
                }
                else
                {
                    $proxyMessage = "the script uses the default proxy: $("$proxyAddress"|ConvertTo-Json))"
                }

                throw (
                    "The script failed to download the sample AASX from: $url. " +
                    "Do you use a proxy and was it properly set up ($proxyMessage)? " +
                    "Was there enough disk space? " +
                    "Could the downloaded file be moved from $samplePathTmp to ${samplePath}? " +
                    "Can you access $url from your browser?"
                )
            }
            finally
            {
                if(Test-Path $samplePathTmp)
                {
                    Remove-Item $samplePathTmp
                }
            }
        }
    }
    else
    {
        if (Test-Path $samplesDir)
        {
            Write-Host "Removing: $samplesDir"
            Remove-Item -Recurse -Force $samplesDir
        }
    }
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
