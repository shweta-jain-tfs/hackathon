[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$SourceRoot = (Get-Location).Path
)

<#
.SYNOPSIS
Creates a defaults-only settings CSV by parsing the TetrisSoftware C# source files.

.DESCRIPTION
This script does not build, load, or start TetrisSoftware. It reads the settings source
files, follows settings classes that have [RootConfigSection], and writes
default-settings.csv to the current working directory.

The parser supports the default initializer conventions used by the settings models:
literal values, enums, and new nested settings object initializers. Page-list collections
are omitted, matching the application's defaults-only export. Defaults
that are calculated in constructors, methods, or other arbitrary C# expressions cannot
be determined from text alone and are reported at the end of the run.

.EXAMPLE
.\GenerateDefaultSettingsCsv.ps1

.EXAMPLE
.\GenerateDefaultSettingsCsv.ps1 C:\git\TetrisSoftware
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-MatchingBraceIndex {
    param([string]$Text, [int]$OpenBraceIndex)

    $depth = 0
    $inString = $false
    $escaped = $false
    $inLineComment = $false
    $inBlockComment = $false
    for ($index = $OpenBraceIndex; $index -lt $Text.Length; $index++) {
        $character = $Text[$index]
        $nextCharacter = if ($index + 1 -lt $Text.Length) { $Text[$index + 1] } else { [char]0 }

        if ($inLineComment) {
            if ($character -eq "`n") { $inLineComment = $false }
            continue
        }
        if ($inBlockComment) {
            if ($character -eq '*' -and $nextCharacter -eq '/') {
                $inBlockComment = $false
                $index++
            }
            continue
        }
        if ($inString) {
            if ($escaped) { $escaped = $false; continue }
            if ($character -eq '\') { $escaped = $true; continue }
            if ($character -eq '"') { $inString = $false }
            continue
        }

        if ($character -eq '/' -and $nextCharacter -eq '/') { $inLineComment = $true; $index++; continue }
        if ($character -eq '/' -and $nextCharacter -eq '*') { $inBlockComment = $true; $index++; continue }
        if ($character -eq '"') { $inString = $true; continue }
        if ($character -eq '{') { $depth++ }
        elseif ($character -eq '}') {
            $depth--
            if ($depth -eq 0) { return $index }
        }
    }

    throw "Unmatched opening brace at character $OpenBraceIndex."
}

function Remove-CSharpComments {
    param([string]$Text)

    $withoutBlockComments = [regex]::Replace($Text, '/\*.*?\*/', '', [Text.RegularExpressions.RegexOptions]::Singleline)
    return [regex]::Replace($withoutBlockComments, '(?m)//.*$', '')
}

function New-ClassIndex {
    param([string]$SourceDirectory)

    $index = @{}
    Get-ChildItem -LiteralPath (Join-Path $SourceDirectory 'Src') -Filter '*.cs' -Recurse |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
        ForEach-Object {
            $file = $_
            if ($file.Length -eq 0) { return }
            foreach ($match in [regex]::Matches([IO.File]::ReadAllText($file.FullName), '(?m)^\s*(?:public|internal)\s+(?:sealed\s+|abstract\s+)?class\s+(?<name>\w+)\b')) {
                # Settings-model names are unique in the source tree. Keeping the first lets us
                # avoid parsing every file until that model is actually needed.
                if (-not $index.ContainsKey($match.Groups['name'].Value)) {
                    $index[$match.Groups['name'].Value] = $file.FullName
                }
            }
        }
    return $index
}

function Get-Class {
    param([string]$ClassName, [hashtable]$ClassIndex, [hashtable]$Classes)

    if ($Classes.ContainsKey($ClassName)) { return $Classes[$ClassName] }
    if (-not $ClassIndex.ContainsKey($ClassName)) { return $null }

    $filePath = $ClassIndex[$ClassName]
    $text = [IO.File]::ReadAllText($filePath)
    $match = [regex]::Match($text, "(?s)(?<attributes>(?:\s*\[[^\]]+\]\s*)*)\s*(?:public|internal)\s+(?:sealed\s+|abstract\s+)?class\s+$([regex]::Escape($ClassName))(?:\s*:\s*[^\{]+)?\s*\{")
    if (-not $match.Success) { return $null }

    $openBrace = $match.Index + $match.Length - 1
    try {
        $closeBrace = Get-MatchingBraceIndex -Text $text -OpenBraceIndex $openBrace
    }
    catch {
        throw "Could not parse class '$ClassName' in '$filePath': $($_.Exception.Message)"
    }
    $class = [pscustomobject]@{
        Name = $ClassName
        Attributes = $match.Groups['attributes'].Value
        Body = $text.Substring($openBrace + 1, $closeBrace - $openBrace - 1)
        File = $filePath
    }
    $Classes[$ClassName] = $class
    return $class
}

function Convert-CSharpDefaultToString {
    param([string]$Expression)

    $value = $Expression.Trim()
    if ($value -eq 'string.Empty') {
        return ''
    }
    if ($value -match '^"(?<value>(?:\\.|[^"\\])*)"$') {
        return [regex]::Unescape($matches['value'])
    }
    if ($value -match "^'(?<value>(?:\\.|[^'\\])*)'$") {
        return $matches['value']
    }
    if ($value -match '^(true|false)$') {
        return (Get-Culture).TextInfo.ToTitleCase($value)
    }
    if ($value -match '^[-+]?\d+(?:\.\d+)?(?:[dDfFmM])?$') {
        return $value.TrimEnd('d', 'D', 'f', 'F', 'm', 'M')
    }
    if ($value -match '^(?<type>\w+)\.(?<member>\w+)$') {
        return $matches['member']
    }
    return $null
}

function Get-Properties {
    param($Class)

    $pattern = '(?ms)(?<attributes>(?:\s*\[[^\]]+\]\s*)*)\s*public\s+(?<type>[\w<>?,.\[\]]+)\s+(?<name>\w+)\s*\{\s*get;\s*set;\s*\}(?:\s*=\s*(?<initializer>[^;]+);)?'
    foreach ($match in [regex]::Matches($Class.Body, $pattern)) {
        [pscustomobject]@{
            Type = $match.Groups['type'].Value
            Name = $match.Groups['name'].Value
            Attributes = $match.Groups['attributes'].Value
            Initializer = $match.Groups['initializer'].Value.Trim()
            File = $Class.File
        }
    }
}

function Get-ObjectInitializerDefaults {
    param([string]$Expression)

    $defaults = @{}
    if ($Expression -notmatch '(?s)^new\s*(?:\w+\s*)?(?:\([^)]*\))?\s*\{(?<body>.*)\}$') {
        return $defaults
    }

    foreach ($assignment in [regex]::Matches($matches['body'], '(?s)(?<name>\w+)\s*=\s*(?<value>[^,}]+)')) {
        $defaults[$assignment.Groups['name'].Value] = $assignment.Groups['value'].Value.Trim()
    }
    return $defaults
}

function Add-SettingsForClass {
    param(
        [string]$ClassName,
        [string]$Prefix,
        [hashtable]$Overrides,
        [hashtable]$ClassIndex,
        [hashtable]$Classes,
        [System.Collections.Generic.List[object]]$Settings,
        [System.Collections.Generic.List[string]]$Warnings,
        [System.Collections.Generic.HashSet[string]]$Visited
    )

    $class = Get-Class -ClassName $ClassName -ClassIndex $ClassIndex -Classes $Classes
    if ($null -eq $class) {
        $Warnings.Add("Could not find settings class '$ClassName' for '$Prefix'.")
        return
    }

    $visitKey = "$ClassName|$Prefix"
    if (-not $Visited.Add($visitKey)) { return }

    foreach ($property in Get-Properties $class) {
        if ($property.Attributes -match 'ExcludeFromImportExport|SettingsAction' -or $property.Type -eq 'SettingsAction') { continue }
        $key = if ([string]::IsNullOrEmpty($Prefix)) { $property.Name } else { "$Prefix`:$($property.Name)" }
        $typeName = ($property.Type -replace '^(?:IEnumerable|ICollection|List)<(.+)>$', '$1').TrimEnd('?', '[', ']')
        $initializer = $property.Initializer

        if ($property.Type -match '^(?:IEnumerable|ICollection|List)<') {
            # Page-list settings need database entries and are absent from a defaults-only export.
            continue
        }

        $initializer = if ($Overrides.ContainsKey($property.Name)) { $Overrides[$property.Name] } else { $property.Initializer }

        if ($ClassIndex.ContainsKey($typeName)) {
            $childOverrides = Get-ObjectInitializerDefaults $initializer
            Add-SettingsForClass -ClassName $typeName -Prefix $key -Overrides $childOverrides -ClassIndex $ClassIndex -Classes $Classes -Settings $Settings -Warnings $Warnings -Visited $Visited
            continue
        }

        if ([string]::IsNullOrWhiteSpace($initializer)) {
            $implicitDefaults = @{
                'bool' = 'False'; 'byte' = '0'; 'sbyte' = '0'; 'short' = '0'; 'ushort' = '0'
                'int' = '0'; 'uint' = '0'; 'long' = '0'; 'ulong' = '0'; 'float' = '0'
                'double' = '0'; 'decimal' = '0'; 'char' = [string][char]0
            }
            if ($implicitDefaults.ContainsKey($typeName)) {
                $value = $implicitDefaults[$typeName]
            }
            else {
                $Warnings.Add("No text default found for '$key' ($($property.File)).")
                continue
            }
        }
        else {
            $value = Convert-CSharpDefaultToString $initializer
        }
        if ($null -eq $value) {
            $Warnings.Add("Unsupported default expression '$initializer' for '$key' ($($property.File)).")
            continue
        }

        $Settings.Add([pscustomobject]@{ Key = $key; Value = $value })
    }
}

$resolvedSourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
$settingsDirectory = Join-Path $resolvedSourceRoot 'Src\Pharos.Server.Application.Settings'
if (-not (Test-Path -LiteralPath $settingsDirectory)) {
    throw "'$resolvedSourceRoot' is not a TetrisSoftware source root. Expected '$settingsDirectory'."
}

$classIndex = New-ClassIndex $resolvedSourceRoot
$classes = @{}
$rootSourceFiles = @(
    $classIndex.Values |
        Sort-Object -Unique |
        Where-Object { Select-String -LiteralPath $_ -Pattern 'RootConfigSection' -Quiet }
)
$rootClasses = @(
    $classIndex.Keys |
        Where-Object { $rootSourceFiles -contains $classIndex[$_] } |
        ForEach-Object { Get-Class -ClassName $_ -ClassIndex $classIndex -Classes $classes } |
        Where-Object { $null -ne $_ -and $_.Attributes -match 'RootConfigSection' }
)
if ($rootClasses.Count -eq 0) {
    throw "No classes with [RootConfigSection] were found below '$resolvedSourceRoot'."
}

$settings = [System.Collections.Generic.List[object]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()
$visited = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($rootClass in $rootClasses) {
    $rootPrefix = $rootClass.Name -replace '(Settings|Options)$', ''
    Add-SettingsForClass -ClassName $rootClass.Name -Prefix $rootPrefix -Overrides @{} -ClassIndex $classIndex -Classes $classes -Settings $settings -Warnings $warnings -Visited $visited
}

$outputPath = Join-Path (Get-Location).Path 'default-settings.csv'
$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('Pharos default settings (generated from source code)')
$lines.Add((Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
$lines.Add('***********************************')
foreach ($setting in $settings | Sort-Object Key) {
    $lines.Add("$($setting.Key),$($setting.Value)")
}
[IO.File]::WriteAllLines($outputPath, $lines)

Write-Host "Generated $($settings.Count) default settings in '$outputPath'."
if ($warnings.Count -gt 0) {
    Write-Warning "Skipped $($warnings.Count) setting(s) whose defaults cannot be determined from source text alone:"
    $warnings | Sort-Object -Unique | ForEach-Object { Write-Warning "  $_" }
}
