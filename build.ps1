param(
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$framework = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319"
$csc = Join-Path $framework "csc.exe"
$icon = Join-Path $root "assets\app.ico"
$manifest = Join-Path $root "assets\app.manifest"
$buildDir = Join-Path $root "build"
$distDir = Join-Path $root "dist"
$testScript = Join-Path $root "test.ps1"

if (-not (Test-Path $csc)) {
    throw "C# compiler not found: $csc"
}
if (-not (Test-Path $icon)) {
    throw "Application icon not found: $icon"
}
if (-not (Test-Path $manifest)) {
    throw "Application manifest not found: $manifest"
}

New-Item -ItemType Directory -Path $buildDir -Force | Out-Null
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

if (-not $SkipTests) {
    Write-Host "Running automated tests before packaging..."
    & powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File $testScript
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Automated tests failed; packaging aborted."
        exit $LASTEXITCODE
    }
}

$references = @(
    (Join-Path $framework "mscorlib.dll"),
    (Join-Path $framework "System.dll"),
    (Join-Path $framework "System.Core.dll"),
    (Join-Path $framework "System.Xml.dll"),
    (Join-Path $framework "System.Drawing.dll"),
    (Join-Path $framework "System.Xaml.dll"),
    (Join-Path $framework "System.Runtime.Serialization.dll"),
    (Join-Path $framework "WPF\WindowsBase.dll"),
    (Join-Path $framework "WPF\PresentationCore.dll"),
    (Join-Path $framework "WPF\PresentationFramework.dll")
)

$sources = @()
$sources += Get-ChildItem (Join-Path $root "src") -Recurse -Filter "*.cs" |
    Sort-Object FullName | ForEach-Object { $_.FullName }

function Invoke-AppBuild([string]$outputPath, [bool]$uiTest) {
    $compilerArgs = @(
        "/nologo",
        "/target:winexe",
        "/platform:anycpu",
        "/optimize+",
        "/codepage:65001",
        "/utf8output",
        "/win32icon:$icon",
        "/win32manifest:$manifest",
        "/resource:$icon,app.ico",
        "/out:$outputPath"
    )
    if ($uiTest) {
        $compilerArgs += "/define:UI_TEST_BUILD"
    }
    foreach ($reference in $references) {
        $compilerArgs += "/reference:$reference"
    }
    $compilerArgs += $sources

    Write-Host "Compiling $outputPath ..."
    & $csc $compilerArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Compilation failed with exit code $LASTEXITCODE."
    }
}

$releaseExe = Join-Path $buildDir "SalaryMonitor.exe"
$uiTestExe = Join-Path $buildDir "SalaryMonitor.UITest.exe"

Invoke-AppBuild $releaseExe $false
Invoke-AppBuild $uiTestExe $true

# Keep the user-facing filename in Chinese. Building it from code points keeps
# this script independent of the console's source encoding.
$chineseName = [string]::Concat(
    [char]0x85AA, [char]0x8D44, [char]0x663E, [char]0x793A, [char]0x5668) + ".exe"
$finalExe = Join-Path $distDir $chineseName
Copy-Item -Path $releaseExe -Destination $finalExe -Force

$hash = (Get-FileHash -Path $finalExe -Algorithm SHA256).Hash
$size = (Get-Item $finalExe).Length
Write-Host ""
Write-Host "Build complete."
Write-Host "Release: $finalExe"
Write-Host "Size: $size bytes"
Write-Host "SHA256: $hash"
Write-Host "UI test build: $uiTestExe"
