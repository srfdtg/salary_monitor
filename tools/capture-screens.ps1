$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "build\SalaryMonitor.UITest.exe"
$configRoot = Join-Path $root "build\screenshot-config"
$outputRoot = Join-Path $root "build\screenshot-output"
$docsShots = Join-Path $root "docs\screenshots"

$states = @(
    @{ Time = "2026-11-09 09:00:00"; Name = "main-before-work.png" },
    @{ Time = "2026-11-09 16:28:35"; Name = "main-working.png" },
    @{ Time = "2026-11-09 12:45:00"; Name = "main-lunch.png" },
    @{ Time = "2026-11-09 19:30:00"; Name = "main-after-work.png" },
    @{ Time = "2026-11-07 12:00:00"; Name = "main-weekend.png" }
)

if (-not (Test-Path -LiteralPath $exe)) {
    throw "UI test build not found: $exe"
}

foreach ($state in $states) {
    if (Test-Path -LiteralPath $configRoot) {
        Remove-Item -LiteralPath $configRoot -Recurse -Force
    }
    if (Test-Path -LiteralPath $outputRoot) {
        Remove-Item -LiteralPath $outputRoot -Recurse -Force
    }

    $env:SALARY_MONITOR_TEST_NOW = $state.Time
    $env:SALARY_MONITOR_CONFIG_DIR = $configRoot
    $env:SALARY_MONITOR_SCREENSHOT_DIR = $outputRoot
    $process = Start-Process -FilePath $exe -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "Screenshot build failed for $($state.Time) with exit code $($process.ExitCode)."
    }

    $main = Join-Path $outputRoot "main.png"
    if (-not (Test-Path -LiteralPath $main)) {
        throw "Screenshot was not produced for $($state.Time)."
    }
    Copy-Item -LiteralPath $main -Destination (Join-Path $docsShots $state.Name) -Force
    Write-Output ("Captured {0} -> {1}" -f $state.Time, $state.Name)

    if ($state.Name -eq "main-working.png") {
        $hidden = Join-Path $outputRoot "main-hidden.png"
        if (-not (Test-Path -LiteralPath $hidden)) {
            throw "Hidden-amount screenshot was not produced for $($state.Time)."
        }
        Copy-Item -LiteralPath $hidden -Destination (Join-Path $docsShots "main-hidden.png") -Force
        Write-Output ("Captured {0} -> main-hidden.png" -f $state.Time)
    }
}

Remove-Item Env:\SALARY_MONITOR_TEST_NOW -ErrorAction SilentlyContinue
Remove-Item Env:\SALARY_MONITOR_CONFIG_DIR -ErrorAction SilentlyContinue
Remove-Item Env:\SALARY_MONITOR_SCREENSHOT_DIR -ErrorAction SilentlyContinue
if (Test-Path -LiteralPath $configRoot) {
    Remove-Item -LiteralPath $configRoot -Recurse -Force
}
if (Test-Path -LiteralPath $outputRoot) {
    Remove-Item -LiteralPath $outputRoot -Recurse -Force
}
Write-Output "SCREENSHOTS: PASS"

