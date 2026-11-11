[CmdletBinding()]
param(
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$workspace = Split-Path $root -Parent
$toolchain = Join-Path $workspace '.android-toolchain'
$sdk = Join-Path $toolchain 'sdk'
$jdk = Join-Path $toolchain 'jdk'
$buildTools = Join-Path $sdk 'build-tools\34.0.0'
$androidJar = Join-Path $sdk 'platforms\android-34\android.jar'
$app = Join-Path $root 'app'
$build = Join-Path $root 'build'
$dist = Join-Path $workspace 'dist'
$finalName = '薪资显示器-Android.apk'
$signedAscii = Join-Path $build 'SalaryMonitor-Android.apk'

$required = @(
    (Join-Path $jdk 'bin\javac.exe'),
    (Join-Path $jdk 'bin\java.exe'),
    (Join-Path $jdk 'bin\jar.exe'),
    (Join-Path $jdk 'bin\keytool.exe'),
    (Join-Path $buildTools 'aapt2.exe'),
    (Join-Path $buildTools 'd8.bat'),
    (Join-Path $buildTools 'zipalign.exe'),
    (Join-Path $buildTools 'apksigner.bat'),
    $androidJar
)
foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing Android toolchain file: $path`nRun the toolchain setup first."
    }
}

$env:JAVA_HOME = $jdk
$env:Path = (Join-Path $jdk 'bin') + ';' + $buildTools + ';' + $env:Path
$javac = Join-Path $jdk 'bin\javac.exe'
$java = Join-Path $jdk 'bin\java.exe'
$jar = Join-Path $jdk 'bin\jar.exe'
$keytool = Join-Path $jdk 'bin\keytool.exe'
$aapt2 = Join-Path $buildTools 'aapt2.exe'
$d8 = Join-Path $buildTools 'd8.bat'
$zipalign = Join-Path $buildTools 'zipalign.exe'
$apksigner = Join-Path $buildTools 'apksigner.bat'

function Invoke-Native {
    param(
        [string]$Exe,
        [string[]]$Arguments,
        [string]$Label
    )
    Write-Output "==> $Label"
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & $Exe @Arguments
    $code = $LASTEXITCODE
    $ErrorActionPreference = $previous
    if ($code -ne 0) {
        throw "$Label failed with exit code $code"
    }
}

if (Test-Path -LiteralPath $build) {
    Remove-Item -LiteralPath $build -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $build | Out-Null
$gen = Join-Path $build 'gen'
$classesDir = Join-Path $build 'classes'
$dexDir = Join-Path $build 'dex'
$testClasses = Join-Path $build 'test-classes'
New-Item -ItemType Directory -Force -Path $gen,$classesDir,$dexDir,$testClasses | Out-Null

$coreSources = @(Get-ChildItem -LiteralPath (Join-Path $app 'src\com\salarymonitor\app\core') -Filter '*.java' | ForEach-Object FullName)
$testSource = Join-Path $root 'tests\CoreTests.java'
if (-not $SkipTests) {
    $testArgs = @('-encoding', 'UTF-8', '-d', $testClasses) + $coreSources + @($testSource)
    Invoke-Native $javac $testArgs 'compile core tests'
    Invoke-Native $java @('-cp', $testClasses, 'CoreTests') 'run core tests'
}

$res = Join-Path $app 'res'
$manifest = Join-Path $app 'AndroidManifest.xml'
$resZip = Join-Path $build 'resources.zip'
$baseApk = Join-Path $build 'base.apk'
Invoke-Native $aapt2 @('compile', '--dir', $res, '-o', $resZip) 'compile resources'
Invoke-Native $aapt2 @(
    'link',
    '-o', $baseApk,
    '-I', $androidJar,
    '--manifest', $manifest,
    '-R', $resZip,
    '--auto-add-overlay',
    '--java', $gen,
    '--min-sdk-version', '26',
    '--target-sdk-version', '34',
    '--version-code', '3',
    '--version-name', '1.1.1'
) 'link resources and manifest'

$sources = @(Get-ChildItem -LiteralPath (Join-Path $app 'src') -Recurse -Filter '*.java' | ForEach-Object FullName)
$sources += @(Get-ChildItem -LiteralPath $gen -Recurse -Filter '*.java' | ForEach-Object FullName)
$compileArgs = @('--release', '8', '-encoding', 'UTF-8', '-classpath', $androidJar, '-d', $classesDir) + $sources
Invoke-Native $javac $compileArgs 'compile app sources'

$classesJar = Join-Path $build 'classes.jar'
Invoke-Native $jar @('cf', $classesJar, '-C', $classesDir, '.') 'package classes'
Invoke-Native $d8 @('--release', '--min-api', '26', '--lib', $androidJar, '--output', $dexDir, $classesJar) 'dex classes'
$dexFile = Join-Path $dexDir 'classes.dex'
if (-not (Test-Path -LiteralPath $dexFile)) {
    throw 'd8 did not produce classes.dex'
}

$unsigned = Join-Path $build 'unsigned.apk'
Copy-Item -LiteralPath $baseApk -Destination $unsigned -Force
Push-Location $dexDir
try {
    Invoke-Native $jar @('uf', $unsigned, 'classes.dex') 'add classes.dex'
} finally {
    Pop-Location
}

$aligned = Join-Path $build 'aligned.apk'
Invoke-Native $zipalign @('-f', '4', $unsigned, $aligned) 'zipalign APK'

$signingDir = Join-Path $root 'signing'
New-Item -ItemType Directory -Force -Path $signingDir | Out-Null
$keystore = Join-Path $signingDir 'salarymonitor.jks'
$passFile = Join-Path $signingDir 'keystore.properties'
$keyAlias = 'salarymonitor'
$storePass = $env:SALARYMONITOR_KEYSTORE_PASS
if ([string]::IsNullOrWhiteSpace($storePass) -and (Test-Path -LiteralPath $passFile)) {
    foreach ($line in Get-Content -LiteralPath $passFile) {
        if ($line -match '^\s*storePassword\s*=\s*(.+?)\s*$') {
            $storePass = $matches[1]
            break
        }
    }
}
if ([string]::IsNullOrWhiteSpace($storePass) -and -not (Test-Path -LiteralPath $keystore)) {
    $storePass = [Guid]::NewGuid().ToString('N')
    Set-Content -LiteralPath $passFile -Value "storePassword=$storePass" -Encoding ASCII
    Write-Output "Generated a local signing password in $passFile"
}
if ([string]::IsNullOrWhiteSpace($storePass)) {
    throw "Signing key exists but no password was found. Set SALARYMONITOR_KEYSTORE_PASS or create $passFile."
}
if (-not (Test-Path -LiteralPath $keystore)) {
    Invoke-Native $keytool @(
        '-genkeypair', '-v',
        '-keystore', $keystore,
        '-storepass', $storePass,
        '-keypass', $storePass,
        '-alias', $keyAlias,
        '-keyalg', 'RSA',
        '-keysize', '2048',
        '-validity', '10000',
        '-dname', 'CN=Salary Monitor, O=Personal, C=CN',
        '-noprompt'
    ) 'create signing key'
}

Invoke-Native $apksigner @(
    'sign',
    '--ks', $keystore,
    '--ks-key-alias', $keyAlias,
    '--ks-pass', "pass:$storePass",
    '--key-pass', "pass:$storePass",
    '--min-sdk-version', '26',
    '--out', $signedAscii,
    $aligned
) 'sign APK'
Invoke-Native $apksigner @('verify', '--verbose', '--print-certs', $signedAscii) 'verify signature'

New-Item -ItemType Directory -Force -Path $dist | Out-Null
$final = Join-Path $dist $finalName
Copy-Item -LiteralPath $signedAscii -Destination $final -Force

$hash = (Get-FileHash -LiteralPath $final -Algorithm SHA256).Hash
$size = (Get-Item -LiteralPath $final).Length
Write-Output ''
Write-Output '=== APK built ==='
Write-Output "File:   $final"
Write-Output "Size:   $size bytes"
Write-Output "SHA256: $hash"
Write-Output ''
Invoke-Native $aapt2 @('dump', 'badging', $signedAscii) 'APK badging'
Write-Output ''
Write-Output 'APK entries:'
& $jar tf $signedAscii | Select-Object -First 30
