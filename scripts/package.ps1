param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repoRoot ('artifacts\release-' + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
if (Get-ChildItem -LiteralPath $OutputDirectory -Force | Select-Object -First 1) { throw 'Use a new empty output directory so stale package files cannot be shipped.' }
$archive = $OutputDirectory + '.zip'
if (Test-Path -LiteralPath $archive) { throw 'Archive already exists. Choose a new output directory.' }
$sourceCommit = (& git -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot determine package source commit.' }
$sourceDirty = [bool](& git -C $repoRoot status --porcelain)
[xml]$buildProperties = Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Build.props')
$localDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }
foreach ($item in @(@('Aegis.Desktop.Wpf','app'), @('Aegis.SyncHost','host'), @('Aegis.Tools','tools'))) {
    $releaseLocks = Join-Path $OutputDirectory 'dependency-locks'
    & $dotnet publish (Join-Path $repoRoot "src\$($item[0])") -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false "-p:ReleaseLockRoot=$releaseLocks" -o (Join-Path $OutputDirectory $item[1])
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
$manifest = [ordered]@{
    format = 1
    desktopVersion = [string]$buildProperties.Project.PropertyGroup.Version
    sourceCommit = $sourceCommit
    sourceDirty = $sourceDirty
    runtime = 'win-x64'
    selfContained = $true
    builtAtUtc = [DateTime]::UtcNow.ToString('o')
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'package-manifest.json') -Encoding UTF8
Compress-Archive -Path (Join-Path $OutputDirectory '*') -DestinationPath $archive
$digest = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
($digest + '  ' + [IO.Path]::GetFileName($archive)) | Set-Content -LiteralPath ($archive + '.sha256') -Encoding ASCII
Write-Host "SHA-256: $digest"
Write-Host "Windows package: $archive"
