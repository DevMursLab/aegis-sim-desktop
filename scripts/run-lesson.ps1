param(
    [ValidateSet('01')]
    [string]$Lesson = '01'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$localDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue

if (Test-Path -LiteralPath $localDotnet) {
    $dotnetPath = $localDotnet
} elseif ($dotnetCommand) {
    $dotnetPath = $dotnetCommand.Source
} else {
    throw 'Install the .NET 10 SDK, then run this script again.'
}

$installedSdks = & $dotnetPath --list-sdks
if (-not ($installedSdks | Where-Object { $_ -match '^10\.' })) {
    throw 'The .NET 10 SDK is missing. A runtime alone cannot build lessons.'
}

$lessonProject = Join-Path $repoRoot 'lessons\01-supply-ticks\Aegis.Lesson01.csproj'
& $dotnetPath run --project $lessonProject
if ($LASTEXITCODE -ne 0) {
    throw "Lesson failed with exit code $LASTEXITCODE."
}
