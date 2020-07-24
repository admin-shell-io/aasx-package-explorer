<#
.SYNOPSIS
This script renders various reports on TODOs in the code.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet,  `
    AssertOpinionatedCsharpTodosVersion, `
    CreateAndGetArtefactsDir

function RenderBadges($ReportPath, $BadgeDir)
{
    if(!(Test-Path -Path $ReportPath))
    {
        throw "Could not find the report file: $ReportPath"
    }

    $report = Get-Content -Raw -Path $ReportPath|ConvertFrom-Json

    $counts = @{
        "TODO"=0
        "BUG"=0
        "HACK"=0
    }
    foreach($fileRecords in $report)
    {
        foreach($record in $fileRecords.records)
        {
            if(!$counts.ContainsKey($record.prefix))
            {
                $counts[$record.prefix] = 0
            }

            $counts[$record.prefix]++
        }
    }

    $prefixes = $counts | Select-Object -ExpandProperty Keys | Sort-Object

    $null = New-Item -ItemType Directory -Force -Path $BadgeDir

    foreach($prefix in $prefixes)
    {
        $filename = "$($prefix + "s").svg"
        $indexOfInvalidChar = $FileName.IndexOfAny(
                [System.IO.Path]::GetInvalidFileNameChars())
        if($indexOfInvalidChar -ne -1)
        {
            throw "Invalid filename for the prefix: $filename"
        }

        $count = $counts[$prefix]

        $uri = ("https://img.shields.io/static/v1?"  +
                "label=$([uri]::EscapeDataString($prefix + "s"))&" +
                "message=$([uri]::EscapeDataString($count))&" +
                "color=blue")

        $resp = Invoke-WebRequest -URI $uri

        $path = Join-Path $BadgeDir $filename

        Set-Content -Path $path -Value $resp.Content

        Write-Host "Saved badge to: $path"
    }
}

$nl = [Environment]::NewLine
$todoRe = [Regex]::new(' \(([^)]+), ([0-9]{4}-[0-9]{2}-[0-9]{2})\): ')
$suffixWhitespaceRe = [Regex]::new('^[ \t]*')

function ParseSuffix($Suffix)
{
    $mtch = $todoRe.Match($Suffix)
    if (!$mtch.Success)
    {
        throw "Unexpected suffix not matching ${todoRe}: $Suffix"
    }

    $author = $mtch.Groups[1].Value
    $date = $mtch.Groups[2].Value
    $remainder = $Suffix.Substring($mtch.Value.Length)

    $lines = $remainder.Split(@("`r`n", "`r", "`n"), [StringSplitOptions]::None)
    if($lines.Count -lt 2)
    {
        $text = $remainder
    }
    elseif($lines.Count -eq 2)
    {
        $lines[1] = $lines[1].Trim()
        $text = $lines -Join $nl
    }
    else
    {
        $wsMtch = $suffixWhitespaceRe.Match($lines[1])
        if($wsMtch.Success)
        {
            $minMargin = $wsMtch.Value.Length

            for($i=2; $i -lt $lines.Length; $i++)
            {
                $line = $lines[$i]

                for($j = 0; ($j -lt $line.Length) -and ($j -lt $minMargin); $j++)
                {
                    $char = $line[$j]
                    if($char -ne $wsMtch.Value[$j])
                    {
                        $minMargin = $j
                    }
                }
            }
        }

        for($i = 1; $i -lt $lines.Length; $i++)
        {
            $lines[$i] = $lines[$i].Substring($minMargin)
        }
        $text = $lines -Join $nl
    }

    [hashtable]$result = @{ }
    $result.Author = $author
    $result.Date = $date
    $result.Text = $text

    return $result
}

function TaskListByFile($Report, $UrlPrefix)
{
    [string[]]$parts = @()

    $sorted = $Report|Sort-Object -Property "path"
    foreach ($fileRecords in $sorted)
    {
        $parts += "## $( $fileRecords.path )$nl$nl"
        foreach ($record in $fileRecords.records)
        {
            $parsed = ParseSuffix -Suffix $record.suffix

            $parts += (
            "[Line $( $record.line + 1), column $( $record.column + 1)]($nl" +
                    $UrlPrefix + "/" + ($fileRecords.path -Replace "\\", "/") +
                    "#L$( $record.line + 1)$nl" +
                    "), $nl"
            )

            $parts += "$( $parsed.Author ),$nl"
            $parts += "$( $parsed.Date )$nl$nl"

            $lines = $parsed.Text.Split(@("`r`n", "`r", "`n"), [StringSplitOptions]::None)
            for($i = 0; $i -lt $lines.Length; $i++)
            {
                $parts += "    " + $lines[$i] + $nl
            }
            $parts += $nl
        }
    }

    return $parts -Join ""
}

function GenerateTaskList($ReportPath, $TaskListDir)
{
    if (!(Test-Path -Path $ReportPath))
    {
        throw "Could not find the report file: $ReportPath"
    }

    $report = $null
    try
    {
        $report = Get-Content -Path $ReportPath|ConvertFrom-Json
    }
    catch
    {
        throw "Failed to parse the report $ReportPath as JSON file: $($_.Exception)"
    }

    $null = New-Item -ItemType Directory -Force -Path $TaskListDir

    $urlPrefix = "https://github.com/admin-shell-io/aasx-package-explorer/blob/master/src"
    $byFile = TaskListByFile -Report $report -UrlPrefix $urlPrefix

    $byFilePath = Join-Path $TaskListDir "task-list-by-file.md"
    Set-Content -Path $byFilePath -Value $byFile -Encoding UTF8

    Write-Host "Saved task list grouped by file to: $byFilePath"
}

function Main
{
    AssertOpinionatedCsharpTodosVersion

    Push-Location
    try
    {
        Set-Location $PSScriptRoot

        $artefactsDir = CreateAndGetArtefactsDir

        $reportDir = $siteDir = Join-Path $artefactsDir "gh-pages" `
            | Join-Path -ChildPath "todos"

        $null = New-Item -ItemType Directory -Force -Path $reportDir

        $reportPath = Join-Path $reportDir "report.json"

        Write-Host "Collecting the TODOs from the code..."
        dotnet opinionated-csharp-todos `
            --inputs '**/*.cs' `
            --excludes 'packages/**' '**/obj/**' 'MsaglWpfControl/**' `
            --report-path $reportPath
        if($LASTEXITCODE -ne 0)
        {
            throw "Failed to collect the TODOs in the code."
        }

        $badgeDir = Join-Path $reportDir "badges"
        RenderBadges -ReportPath $reportPath -BadgeDir $badgeDir

        $taskListDir = Join-Path $reportDir "task-list"
        GenerateTaskList -ReportPath $reportPath -TaskListDir $taskListDir
    }
    finally
    {
        Pop-Location
    }
}

Main
