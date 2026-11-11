$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class LiveCaptureNative {
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
}
'@

$exe = "D:\AI\dist\薪资显示器.exe"
$portable = "D:\AI\dist\SalaryMonitor.settings.json"
$output = "D:\AI\build\live-after-work.png"
if (Test-Path -LiteralPath $portable) { Remove-Item -LiteralPath $portable -Force }

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $exe
$psi.WorkingDirectory = "D:\AI\dist"
$psi.UseShellExecute = $false
$process = [System.Diagnostics.Process]::Start($psi)
try {
    for ($i = 0; $i -lt 50; $i++) {
        Start-Sleep -Milliseconds 200
        $process.Refresh()
        if ($process.MainWindowHandle -ne 0) { break }
    }
    if ($process.MainWindowHandle -eq 0) { throw "No main window." }
    Start-Sleep -Milliseconds 1200
    $process.Refresh()

    $rect = New-Object LiveCaptureNative+RECT
    if (-not [LiveCaptureNative]::GetWindowRect($process.MainWindowHandle, [ref]$rect)) {
        throw "GetWindowRect failed."
    }
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    Write-Output "WINDOW_RECT=$($rect.Left),$($rect.Top) ${width}x${height}"

    $bitmap = New-Object System.Drawing.Bitmap($width, $height)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0,
                (New-Object System.Drawing.Size($width, $height)))
            $bitmap.Save($output, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $graphics.Dispose()
        }
    }
    finally {
        $bitmap.Dispose()
    }
    Write-Output "SAVED=$output"
}
finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 400
    }
    if (Test-Path -LiteralPath $portable) { Remove-Item -LiteralPath $portable -Force }
}

