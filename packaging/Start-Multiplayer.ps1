$ErrorActionPreference = 'Stop'
if (Get-NetTCPConnection -LocalPort 5099 -State Listen -ErrorAction SilentlyContinue) { throw 'Port 5099 is already in use. Stop the earlier demo host first.' }
$dataDirectory = Join-Path $env:LOCALAPPDATA 'AEGIS-SIM\host'
New-Item -ItemType Directory -Path $dataDirectory -Force | Out-Null
$server = Start-Process -FilePath (Join-Path $PSScriptRoot 'host\Aegis.SyncHost.exe') -ArgumentList @('--data-dir', ('"' + $dataDirectory + '"')) -WindowStyle Hidden -PassThru
try {
    for ($attempt = 0; $attempt -lt 50; $attempt++) {
        if ($server.HasExited) { throw 'Host failed to start. Close any earlier demo host using port 5099.' }
        try { Invoke-RestMethod http://127.0.0.1:5099/health | Out-Null; break } catch { Start-Sleep -Milliseconds 200 }
    }
    $secretPath = Join-Path $dataDirectory 'session-secrets.json'
    if (-not (Test-Path -LiteralPath $secretPath)) { throw 'Host did not start.' }
    foreach ($role in @('controller','observer')) {
        Start-Process -FilePath (Join-Path $PSScriptRoot 'app\Aegis.Desktop.Wpf.exe') -ArgumentList @('--server','http://127.0.0.1:5099','--role',$role,'--secrets',('"' + $secretPath + '"'))
    }
    Read-Host 'Controller and observer are open. Press Enter here to stop the host after the demo'
} finally { if (-not $server.HasExited) { Stop-Process -Id $server.Id } }
