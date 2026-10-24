param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repoRoot ('artifacts\release-' + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$localDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }
foreach ($item in @(@('Aegis.Desktop.Wpf','app'), @('Aegis.SyncHost','host'), @('Aegis.Tools','tools'))) {
    $releaseLock = Join-Path $OutputDirectory ($item[0] + '.packages.lock.json')
    & $dotnet publish (Join-Path $repoRoot "src\$($item[0])") -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false "-p:NuGetLockFilePath=$releaseLock" -o (Join-Path $OutputDirectory $item[1])
    if ($LASTEXITCODE -ne 0) { throw "Publishing failed: $($item[0])" }
}
Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination $OutputDirectory
Copy-Item -LiteralPath (Join-Path $repoRoot 'THIRD-PARTY-NOTICES.md') -Destination $OutputDirectory
Copy-Item -LiteralPath (Join-Path $repoRoot 'third-party') -Destination $OutputDirectory -Recurse
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets') -Destination $OutputDirectory -Recurse
Copy-Item -Path (Join-Path $repoRoot 'packaging\*') -Destination $OutputDirectory
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs') -Destination $OutputDirectory -Recurse
& (Join-Path $OutputDirectory 'tools\Aegis.Tools.exe') --seed (Join-Path $OutputDirectory 'demo-data')
if ($LASTEXITCODE -ne 0) { throw 'Packaged seed tool failed.' }
$archive = $OutputDirectory + '.zip'
Compress-Archive -Path (Join-Path $OutputDirectory '*') -DestinationPath $archive
Get-FileHash -LiteralPath $archive -Algorithm SHA256 | Format-List
Write-Host "Windows package: $archive"
