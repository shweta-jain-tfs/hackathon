# Script used in pipeline to set the ProductVersion of the installer to the pipeline build number.

param (
    [string]$newVersion
)

$filePath = "Installer/PharosInstaller/Package.wxs"

$content = Get-Content -Path $filePath -Raw

if($content -eq $null) {
    Write-Error "File not found."
    exit 1
}

# Find and update the product version

$versionPattern = 'Version="(\d+)\.(\d+)\.(\d+)\.(\d+)"'

$version = [regex]::Match($content, $versionPattern)

if ($version.Success -eq $false) {
    Write-Error "Version attribute not found in the file."
    exit 1
}

$content = [regex]::Replace($content, $versionPattern, "Version=`"$newVersion`"")

Set-Content -Path $filePath -Value $content

Write-Output "Updated ProductVersion of installer."
Write-Output "New installer version: $newVersion"

