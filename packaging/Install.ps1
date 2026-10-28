param([string]$Destination, [switch]$SkipShortcut)
$ErrorActionPreference = 'Stop'
$packageSource = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
$desktopExecutable = Join-Path $packageSource 'app\Aegis.Desktop.Wpf.exe'
if (-not (Test-Path -LiteralPath $desktopExecutable -PathType Leaf)) { throw 'Extract the complete Windows package before installing.' }
$packageVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($desktopExecutable).ProductVersion.Split('+')[0]
$parsedPackageVersion = $null
if (-not [Version]::TryParse($packageVersion, [ref]$parsedPackageVersion)) { throw 'The packaged desktop version is invalid.' }
if (-not $Destination) { $Destination = Join-Path $env:LOCALAPPDATA ('Programs\AEGIS-SIM\' + $packageVersion) }
$destination = [IO.Path]::GetFullPath($Destination)
$separator = [IO.Path]::DirectorySeparatorChar
if ($destination.TrimEnd($separator).Equals($packageSource, [StringComparison]::OrdinalIgnoreCase) -or
    $destination.StartsWith($packageSource + $separator, [StringComparison]::OrdinalIgnoreCase) -or
    $packageSource.StartsWith($destination.TrimEnd($separator) + $separator, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Choose an installation folder outside the extracted package and its parent folders.'
}
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Copy-Item -Path (Join-Path $PSScriptRoot '*') -Destination $destination -Recurse -Force
if (-not $SkipShortcut) {
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'AEGIS-SIM.lnk'))
$shortcut.TargetPath = Join-Path $destination 'Start-Local.cmd'
$shortcut.WorkingDirectory = $destination
$shortcut.Save()
}
Write-Host "Installed for the current user: $destination. User data stays in LOCALAPPDATA\AEGIS-SIM."
if (-not $SkipShortcut) { Write-Host 'Desktop shortcut created.' }
