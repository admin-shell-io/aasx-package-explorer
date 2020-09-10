<#
.SYNOPSIS
This script runs all the pre-merge checks locally.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    CreateAndGetArtefactsDir

function IsISE {
    try {
        return $null -ne $psISE;
    }
    catch {
        return $false;
    }
}

function LogAndExecute($Expression, $Timestamp, $ForegroundColor, $Emoji)
{
    Write-Host "---"
    Write-Host "Running: $Expression"
    Write-Host "---"

    if (IsISE)
    {
        $symbol = $Emoji
    }
    else
    {
        $symbol = ""
    }

    if ($Expression.StartsWith("./Check"))
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
        throw "Unhandled title for the expression: $($Expression|ConvertTo-Json)"
    }

    & powershell $Expression|ForEach-Object {
        Write-Host -ForegroundColor $ForegroundColor -BackgroundColor White `
            -NoNewline "$symbol${title}:${Timestamp}:"
        Write-Host " $_"
    }
}

function PickColor($ArtefactsDir) {
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
    $foregroundColor = PickColor -ArtefactsDir $artefactsDir
    $emoji = PickEmoji -ArtefactsDir $artefactsDir
    $timestamp = [DateTime]::Now.ToString('HH:mm:ss')

    LogAndExecute `
        -Expression "./CheckLicenses.ps1" `
        -Timestamp $timestamp -ForegroundColor $foregroundColor -Emoji $emoji

    LogAndExecute `
        -Expression "./CheckFormat.ps1" `
        -Timestamp $timestamp -ForegroundColor $foregroundColor -Emoji $emoji

    LogAndExecute `
        -Expression "./CheckBiteSized.ps1" `
        -Timestamp $timestamp -ForegroundColor $foregroundColor -Emoji $emoji

    LogAndExecute  `
        -Expression "./CheckDeadCode.ps1" `
        -Timestamp $timestamp -ForegroundColor $foregroundColor -Emoji $emoji

    LogAndExecute  `
        -Expression "./CheckTodos.ps1" `
        -Timestamp $timestamp -ForegroundColor $foregroundColor -Emoji $emoji

    LogAndExecute  `
        -Expression "./Doctest.ps1 -check" `
        -Timestamp $timestamp -ForegroundColor $foregroundColor -Emoji $emoji

    LogAndExecute  `
        -Expression "./BuildForDebug.ps1" `
        -Timestamp $timestamp -ForegroundColor $foregroundColor -Emoji $emoji

    LogAndExecute  `
        -Expression "./Test.ps1" `
        -Timestamp $timestamp -ForegroundColor $foregroundColor -Emoji $emoji

    LogAndExecute  `
        -Expression "./InspectCode.ps1" `
        -Timestamp $timestamp -ForegroundColor $foregroundColor -Emoji $emoji

    LogAndExecute `
        -Expression "./CheckPushCommitMessages.ps1" `
        -Timestamp $timestamp -ForegroundColor $foregroundColor -Emoji $emoji

    Write-Host
    Write-Host "All checks passed successfully. You can now push the commits."
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
