param([string]$Output = (Join-Path $PSScriptRoot '..\artifacts\performance.json'))
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$localDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }
& $dotnet run --project (Join-Path $repoRoot 'src\Aegis.Tools') -c Release -- --profile ([IO.Path]::GetFullPath($Output))
if ($LASTEXITCODE -ne 0) { throw 'Performance profiling failed.' }
