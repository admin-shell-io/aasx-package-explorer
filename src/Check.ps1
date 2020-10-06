<#
.SYNOPSIS
This script runs all the pre-merge checks locally.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    CreateAndGetArtefactsDir

class Setting
{
    [string]$Timestamp
    [string]$ForegroundColor  # empty string -> do not show colors
    [string]$Emoji  # empty string -> do not show emoji

    Setting($Timestamp, $ForegroundColor, $Emoji) {
       $this.Timestamp = $Timestamp
       $this.ForegroundColor = $ForegroundColor
       $this.Emoji = $Emoji
    }
}

function LogAndExecute([string]$Expression, [Setting]$Setting)
{
    Write-Host "---"
    Write-Host "Running: $Expression"
    Write-Host "---"

    if ( $Expression.StartsWith("./Check"))
    {
        $title = $Expression -replace '^./Check([a-zA-Z_0-9-]+)\.ps1.*$', '$1'
    }
    elseif ($Expression.StartsWith("./Doctest"))
    {
        $title = "Doctest"
    }
    elseif ($Expression.StartsWith("./Build"))
    {
        $title = "Build"
    }
    elseif ($Expression.StartsWith("./Test"))
    {
        $title = "Test"
    }
    elseif ($Expression.StartsWith("./InspectCode"))
    {
        $title = "Inspect"
    }
    else
    {
        throw "Unhandled title for the expression: $( $Expression|ConvertTo-Json )"
    }

    if ("" -eq $Setting.Emoji)
    {
        $prefix = "${title}:${Timestamp}:"
    }
    else
    {
        $prefix = "$($Setting.Emoji)${title}:${Timestamp}:"
    }

    if ("" -eq $Setting.ForegroundColor)
    {
        & powershell $Expression|ForEach-Object {
            Write-Host -NoNewline $prefix
            Write-Host " $_"
        }
    }
    else
    {
        & powershell $Expression|ForEach-Object {
            Write-Host `
                -ForegroundColor $Setting.ForegroundColor `
                -BackgroundColor White `
                -NoNewline $prefix
            Write-Host " $_"
        }
    }
}

function PickColor($ArtefactsDir)
{
    $colorPath = Join-Path $ArtefactsDir "CheckLastColor.txt"
    if (Test-Path $colorPath)
    {
        $lastColor = Get-Content $colorPath
    }
    else
    {
        $lastColor = ""
    }

    $colors = @(
    "Black",
    "DarkBlue",
    "DarkGreen",
    "DarkCyan",
    "DarkMagenta",
    "DarkGray",
    "Blue",
    "Magenta"
    )

    $lastColorIndex = [array]::indexof($colors, $lastColor)
    if ($lastColorIndex -eq -1)
    {
        $lastColorIndex = 0
    }
    $colorIndex = ($lastColorIndex + 1) % $colors.Length
    $color = $colors[$colorIndex]

    Set-Content -Path $colorPath -Value $color

    return $color
}

function PickEmoji($ArtefactsDir)
{
    $emojiPath = Join-Path $ArtefactsDir "CheckLastEmoji.txt"
    if (Test-Path $emojiPath)
    {
        $lastEmoji = (Get-Content $emojiPath) -as [int]
    }
    else
    {
        $lastEmoji = 0
    }

    $emojis = @(
    0x1F916,
    0x1F600,
    0x1F921,
    0x1F63A,
    0x1F44A,
    0x1F47E,
    0x1F91E,
    0x1f64f,
    0x1f4aa,
    0x1f440
    )

    $lastEmojiIndex = [array]::indexof($emojis, $lastEmoji)
    if ($lastEmojiIndex -eq -1)
    {
        $lastEmojiIndex = 0
    }
    $emojiIndex = ($lastEmojiIndex + 1) % $emojis.Length
    $emoji = $emojis[$emojiIndex]

    Set-Content -Path $emojiPath -Value $emoji.ToString()

    return [char]::ConvertFromUtf32($emoji)
}

function Main
{
    Set-Location $PSScriptRoot

    $artefactsDir = CreateAndGetArtefactsDir

    $timestamp = [DateTime]::Now.ToString('HH:mm:ss')
    $setting = [Setting]::new($timestamp, "", "")

    function IsISE
    {
        try
        {
            return $null -ne $psISE;
        }
        catch
        {
            return $false;
        }
    }

    $isISE = IsISE
    if ($isISE)
    {
        <#
        (mristin, 2020-10-03) I could make only ISE display emojis properly.
        The other consoles such as Git Bash did not play ball.
        #>
        $setting.Emoji = PickEmoji -ArtefactsDir $artefactsDir

        <#
        (mristin, 2020-10-03) We could not make @MichaelHoffmeisterFesto
        Powershell ISE display the colors properly though they worked just
        fine in my ISE (version 5.1.18362.752). We had to apply the SimCity
        trick and disable coloring for this particular host.

        For more context on SimCity trick, see 
        https://www.joelonsoftware.com/2004/06/13/how-microsoft-lost-the-api-war/
        #>
        $hostVersion = (Get-Host).Version.ToString()
        if ($hostVersion -ne "5.1.17134.858")
        {
            $setting.ForegroundColor = PickColor -ArtefactsDir $artefactsDir 
        }
    }

    LogAndExecute `
        -Expression "./CheckLicenses.ps1" `
        -Setting $setting

    LogAndExecute `
        -Expression "./CheckFormat.ps1" `
        -Setting $setting

    LogAndExecute `
        -Expression "./CheckBiteSized.ps1" `
        -Setting $setting

    LogAndExecute `
        -Expression "./CheckDeadCode.ps1" `
        -Setting $setting

    LogAndExecute `
        -Expression "./CheckTodos.ps1" `
        -Setting $setting

    LogAndExecute `
        -Expression "./Doctest.ps1 -check" `
        -Setting $setting

    LogAndExecute `
        -Expression "./BuildForDebug.ps1" `
        -Setting $setting

    LogAndExecute `
        -Expression "./Test.ps1" `
        -Setting $setting

    LogAndExecute `
        -Expression "./InspectCode.ps1" `
        -Setting $setting

    LogAndExecute `
        -Expression "./CheckPushCommitMessages.ps1" `
        -Setting $setting

    Write-Host
    Write-Host "All checks passed successfully. You can now push the commits."
}

$previousLocation = Get-Location; try
{
    Main
}
finally
{
    [Console]::ResetColor()
    Write-Host
    Set-Location $previousLocation
}
