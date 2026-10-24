param([string]$Destination = (Join-Path $env:LOCALAPPDATA 'Programs\AEGIS-SIM\2.0.0'), [switch]$SkipShortcut)
$ErrorActionPreference = 'Stop'
$destination = [IO.Path]::GetFullPath($Destination)
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Copy-Item -Path (Join-Path $PSScriptRoot '*') -Destination $destination -Recurse -Force
if (-not $SkipShortcut) {
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'AEGIS-SIM.lnk'))
$shortcut.TargetPath = Join-Path $destination 'Start-Local.cmd'
$shortcut.WorkingDirectory = $destination
$shortcut.Save()
}
Write-Host "Installed for the current user: $destination. Desktop shortcut created. User data stays in LOCALAPPDATA\AEGIS-SIM."
