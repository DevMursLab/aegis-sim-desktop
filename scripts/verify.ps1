param([switch]$SkipUi)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$localDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }
Push-Location $repoRoot
try {
    & $dotnet build Aegis.Sim.slnx -c Release -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
    foreach ($project in @('tests/Aegis.Lesson03.Checks', 'tests/Aegis.Desktop.Checks')) {
        & $dotnet run --project $project -c Release --no-build
        if ($LASTEXITCODE -ne 0) { throw "Checks failed: $project" }
    }
    foreach ($lesson in @('01-supply-ticks/Aegis.Lesson01', '02-shelter-objects/Aegis.Lesson02', '03-tick-engine/Aegis.Lesson03')) {
        & $dotnet run --project "lessons/$lesson.csproj" -c Release --no-build
        if ($LASTEXITCODE -ne 0) { throw "Lesson failed: $lesson" }
    }
    if (-not $SkipUi) {
        & $dotnet run --project tests/Aegis.Wpf.Smoke -c Release --no-build
        if ($LASTEXITCODE -ne 0) { throw 'WPF smoke check failed.' }
    }
    Write-Host 'Verification passed.'
} finally {
    Pop-Location
}
