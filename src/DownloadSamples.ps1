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
