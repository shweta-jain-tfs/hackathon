# This script renames installer and script files to include Pharos version in the file name.

param (
    [string]$installerDir,
    [string]$pharosVersion,
    [string]$fileNamePrefix = "Pharos_"
)

function Rename-File {
    param (
        [string]$extension,
        [string]$newName
    )

    $file = Get-ChildItem -Path $installerDir -Recurse -Filter "*.$extension" | Select-Object -First 1

    if ($file -eq $null) {
        Write-Error "File not found with extension $extension."
        exit 1
    }

    $newFilePath = Join-Path -Path $file.DirectoryName -ChildPath $newName
    Rename-Item -Path $file.FullName -NewName $newFilePath
    
    Write-Host "Renamed $($file.FullName) to $newFilePath"
}

Rename-File -extension "msi" -newName "$fileNamePrefix$pharosVersion.msi"

Rename-File -extension "exe" -newName "$fileNamePrefix$pharosVersion.exe"