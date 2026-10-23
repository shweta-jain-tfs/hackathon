# Verifies that a built MSI carries the expected ProductVersion, so that the production and
# test installers from the same run cannot drift apart in version.

param (
    [Parameter(Mandatory = $true)]
    [string]$installerDir,

    [Parameter(Mandatory = $true)]
    [string]$expectedVersion
)

$ErrorActionPreference = 'Stop'

$msi = Get-ChildItem -Path $installerDir -Recurse -Filter '*.msi' -File | Select-Object -First 1

if ($null -eq $msi) {
    throw "No MSI file was found in $installerDir."
}

$installer = New-Object -ComObject WindowsInstaller.Installer
$database = $installer.GetType().InvokeMember('OpenDatabase', 'InvokeMethod', $null, $installer, @($msi.FullName, 0))
$view = $database.GetType().InvokeMember('OpenView', 'InvokeMethod', $null, $database, @("SELECT Value FROM Property WHERE Property = 'ProductVersion'"))
$view.GetType().InvokeMember('Execute', 'InvokeMethod', $null, $view, $null)
$record = $view.GetType().InvokeMember('Fetch', 'InvokeMethod', $null, $view, $null)

if ($null -eq $record) {
    throw "ProductVersion was not found in $($msi.FullName)."
}

$actualVersion = $record.GetType().InvokeMember('StringData', 'GetProperty', $null, $record, 1)

if ($actualVersion -ne $expectedVersion) {
    throw "$($msi.Name) has ProductVersion '$actualVersion' but '$expectedVersion' was expected."
}

Write-Host "$($msi.Name) ProductVersion = $actualVersion"
