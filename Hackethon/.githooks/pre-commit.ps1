#!/usr/bin/env pwsh
param()

$ErrorActionPreference = "Stop"

# Determine repository root based on the script location (.githooks is at repo root)
$scriptDir = Split-Path -Parent $PSCommandPath
$repoRoot  = Split-Path -Parent $scriptDir   # one level above .githooks

Write-Host "Repository root: $repoRoot"

# Path to the XSLT file (in tools\ResxNormalizer)
$xsltPath = Join-Path $repoRoot "tools\ResxNormalizer\ResxNormalizer.xslt"
Write-Host "Using ResxNormalizer.xslt at: $xsltPath"

if (-not (Test-Path $xsltPath)) {
    Write-Error "ResxNormalizer.xslt not found at $xsltPath"
    exit 1
}

# Load the XSLT
$xslt = New-Object System.Xml.Xsl.XslCompiledTransform
$xslt.Load($xsltPath)

# Get all staged .resx files (Added, Copied, Modified)
$changedFiles = git -C $repoRoot diff --cached --name-only --diff-filter=ACM |
    Where-Object { $_ -like '*.resx' }

Write-Host "Staged .resx files:"
if (-not $changedFiles) {
    Write-Host "  (none)"
    exit 0
}

foreach ($f in $changedFiles) {
    Write-Host "  $f"
}

Write-Host "Normalizing staged .resx files..."

foreach ($f in $changedFiles) {
    # Build absolute path to work with
    $fullPath = Join-Path $repoRoot $f

    if (-not (Test-Path $fullPath)) {
        Write-Host "WARNING: File not found on disk: $fullPath"
        continue
    }

    try {
        Write-Host "Running normalization on: $fullPath"
        
        # Create a temporary file for output
        $tempFile = [System.IO.Path]::GetTempFileName()
        
        # Configure XML writer settings for clean output
        $writerSettings = New-Object System.Xml.XmlWriterSettings
        $writerSettings.Indent = $true
        $writerSettings.IndentChars = "  "
        $writerSettings.NewLineChars = "`r`n"
        $writerSettings.NewLineHandling = [System.Xml.NewLineHandling]::Replace
        $writerSettings.OmitXmlDeclaration = $false
        $writerSettings.Encoding = [System.Text.UTF8Encoding]::new($false) # UTF-8 without BOM
        
        # Create XML writer
        $writer = [System.Xml.XmlWriter]::Create($tempFile, $writerSettings)
        
        try {
            # Transform the file
            $xslt.Transform($fullPath, $writer)
        }
        finally {
            $writer.Close()
        }
        
        # Read the transformed content and clean up extra blank lines
        $content = Get-Content -Path $tempFile -Raw -Encoding UTF8
        
        # Remove blank lines at the start of the file (after XML declaration)
        $content = $content -replace "(<\?xml[^>]+\?>)\s+", "`$1`r`n"
        
        # Remove excessive blank lines between </resheader> and first <data> element
        # This regex finds the last </resheader> tag followed by whitespace and the first <data> tag
        # and replaces all the whitespace in between with just 2 newlines
        $content = $content -replace "(</resheader>)\s+(<data)", "`$1`r`n`$2"
        
        # Ensure each <data> element starts on its own line (not on same line as </data>)
        $content = $content -replace "(</data>)(<data)", "`$1`r`n`$2"
        
        # Remove multiple consecutive blank lines (more than 2 newlines in a row)
        $content = $content -replace "(\r?\n){3,}", "`r`n`r`n"
        
        # Ensure <data> tags are properly indented at the root level (2 spaces)
        $content = $content -replace "(?m)^\s*(<data[^>]*>)", "  `$1"
        
        # Ensure </data> tags are properly indented at the root level (2 spaces)
        $content = $content -replace "(?m)^\s*(</data>)", "  `$1"
        
        # Ensure </root> is on its own line with no indentation
        $content = $content -replace "(</data>)\s*(</root>)", "`$1`r`n`$2"
        
        # Write cleaned content back to original file
        [System.IO.File]::WriteAllText($fullPath, $content, [System.Text.UTF8Encoding]::new($false))
        
        # Clean up temp file
        Remove-Item $tempFile -ErrorAction SilentlyContinue
        
        Write-Host "Normalized: $fullPath"
        
        # Re-stage the updated file so the commit contains the cleaned version
        git -C $repoRoot add $f | Out-Null
    }
    catch {
        Write-Error "Failed to normalize ${fullPath}: $_"
        if (Test-Path $tempFile) {
            Remove-Item $tempFile -ErrorAction SilentlyContinue
        }
        exit 1
    }
}

Write-Host "Resx normalization completed."
exit 0