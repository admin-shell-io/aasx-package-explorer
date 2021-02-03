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
        "18_Hitachi.aasx",
        "34_Festo.aasx"
        )

        # Set up the default proxy. This is necessary for many enterprise
        # settings, as the direct connection with Invoke-WebRequest does not
        # work.
        #
        # See also: https://www.reddit.com/r/PowerShell/comments/77m4lj/,
        # http://woshub.com/using-powershell-behind-a-proxy/ and
        # https://community.idera.com/database-tools/powershell/powertips/b/tips/posts/use-internet-connection-with-default-proxy

        $proxy = [System.Net.WebRequest]::GetSystemWebProxy()
        $proxy.Credentials = [System.Net.CredentialCache]::DefaultNetworkCredentials
        $webClient = New-Object System.Net.WebClient
        $webClient.proxy = $proxy
        $webClient.UseDefaultCredentials = $true

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
                $webClient.DownloadFile($url, $samplePathTmp)
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

                $nl = [Environment]::NewLine

                throw (
                    "The script failed to download the sample AASX from: $url.$nl$nl" +
                    "* Do you use a proxy and was it properly set up?$nl" +
                    "  * powershell `"[System.Net.WebRequest]::GetSystemWebProxy()`"?$nl" +
                    "* Was there enough disk space?$nl" +
                    "* Could the downloaded file be moved from $samplePathTmp to ${samplePath}?$nl" +
                    "* Can you access $url from your browser?"
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
