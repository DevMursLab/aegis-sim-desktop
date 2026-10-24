param([string]$DataDirectory = (Join-Path $env:LOCALAPPDATA 'AEGIS-SIM\host'), [string]$Scenario)
$ErrorActionPreference = 'Stop'
if (Get-NetTCPConnection -LocalPort 5099 -State Listen -ErrorAction SilentlyContinue) { throw 'Port 5099 is already in use. Stop the earlier demo host first.' }
$repoRoot = Split-Path -Parent $PSScriptRoot
$localDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }
& $dotnet build (Join-Path $repoRoot 'Aegis.Sim.slnx') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$serverPath = Join-Path $repoRoot 'src\Aegis.SyncHost\bin\Release\net10.0\Aegis.SyncHost.exe'
$desktopPath = Join-Path $repoRoot 'src\Aegis.Desktop.Wpf\bin\Release\net10.0-windows\Aegis.Desktop.Wpf.exe'
$DataDirectory = [IO.Path]::GetFullPath($DataDirectory)
New-Item -ItemType Directory -Path $DataDirectory -Force | Out-Null
$serverArguments = @('--data-dir', ('"' + $DataDirectory + '"'))
if ($Scenario) { $serverArguments += @('--scenario', ('"' + [IO.Path]::GetFullPath($Scenario) + '"')) }
$server = Start-Process -FilePath $serverPath -ArgumentList $serverArguments -WindowStyle Hidden -PassThru
try {
    for ($attempt = 0; $attempt -lt 50; $attempt++) {
        if ($server.HasExited) { throw 'Host could not start. Port 5099 may already be in use.' }
        try { Invoke-RestMethod http://127.0.0.1:5099/health | Out-Null; break } catch { Start-Sleep -Milliseconds 200 }
    }
    $secret = Join-Path $DataDirectory 'session-secrets.json'
    if (-not (Test-Path -LiteralPath $secret)) { throw 'Host did not initialize its local session credentials.' }
    foreach ($role in @('controller', 'observer')) {
        Start-Process -FilePath $desktopPath -ArgumentList @('--server', 'http://127.0.0.1:5099', '--role', $role, '--secrets', ('"' + $secret + '"'))
    }
    Write-Host "Controller and observer opened. Host process ID: $($server.Id). Stop the host with Stop-Process -Id $($server.Id) after closing the windows."
} catch {
    if (-not $server.HasExited) { Stop-Process -Id $server.Id }
    throw
}
