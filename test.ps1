$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$framework = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319"
$csc = Join-Path $framework "csc.exe"
if (-not (Test-Path $csc)) {
    throw "C# compiler not found: $csc"
}

$outDir = Join-Path $root "build\tests"
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$outExe = Join-Path $outDir "SalaryMonitor.Tests.exe"
$logPath = Join-Path $outDir "test-output.txt"

$references = @(
    (Join-Path $framework "mscorlib.dll"),
    (Join-Path $framework "System.dll"),
    (Join-Path $framework "System.Core.dll"),
    (Join-Path $framework "System.Xml.dll"),
    (Join-Path $framework "System.Runtime.Serialization.dll")
)

$sources = @()
$sources += Get-ChildItem (Join-Path $root "src\Core") -Filter "*.cs" | ForEach-Object { $_.FullName }
$sources += (Join-Path $root "tests\SalaryMonitor.Tests.cs")

$compilerArgs = @(
    "/nologo",
    "/target:exe",
    "/platform:anycpu",
    "/optimize+",
    "/codepage:65001",
    "/utf8output",
    "/out:$outExe"
)
foreach ($reference in $references) {
    $compilerArgs += "/reference:$reference"
}
$compilerArgs += $sources

Write-Host "Compiling automated tests..."
& $csc $compilerArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "Test compilation failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}

Write-Host "Running automated tests..."
& $outExe 2>&1 | Tee-Object -FilePath $logPath
$testExit = $LASTEXITCODE
Write-Host "Test log: $logPath"
exit $testExit
