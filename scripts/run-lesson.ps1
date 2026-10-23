param(
    [ValidateSet('01', '02', '03')]
    [string]$Lesson = '01',
    [switch]$Check
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

$projects = @{
    '01' = 'lessons\01-supply-ticks\Aegis.Lesson01.csproj'
    '02' = 'lessons\02-shelter-objects\Aegis.Lesson02.csproj'
    '03' = 'lessons\03-tick-engine\Aegis.Lesson03.csproj'
}
$lessonProject = Join-Path $repoRoot $projects[$Lesson]
if ($Check) {
    if ($Lesson -ne '03') {
        throw 'Executable checks are currently available only for Lesson 03.'
    }
    $lessonProject = Join-Path $repoRoot 'tests\Aegis.Lesson03.Checks\Aegis.Lesson03.Checks.csproj'
}
& $dotnetPath run --project $lessonProject
if ($LASTEXITCODE -ne 0) {
    throw "Lesson failed with exit code $LASTEXITCODE."
}
