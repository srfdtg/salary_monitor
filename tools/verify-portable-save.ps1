param(
    [string]$ExePath = "D:\AI\dist\薪资显示器.exe",
    [string]$TestDir = "D:\AI\build\portable-ui-test"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class PortableUiNative {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    public static IntPtr FindWindowForProcess(uint processId, string titlePart) {
        IntPtr found = IntPtr.Zero;
        EnumWindows(delegate(IntPtr hWnd, IntPtr lParam) {
            uint pid;
            GetWindowThreadProcessId(hWnd, out pid);
            if (pid == processId && IsWindowVisible(hWnd)) {
                StringBuilder text = new StringBuilder(512);
                GetWindowText(hWnd, text, text.Capacity);
                if (text.ToString().IndexOf(titlePart, StringComparison.Ordinal) >= 0) {
                    found = hWnd;
                    return false;
                }
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }
}
'@

$exe = [System.IO.Path]::GetFullPath($ExePath)
$testDirFull = [System.IO.Path]::GetFullPath($TestDir)
$testExe = Join-Path $testDirFull "薪资显示器.exe"
$portableSettings = Join-Path $testDirFull "SalaryMonitor.settings.json"
$roamingSettings = Join-Path $env:APPDATA "SalaryMonitor\settings.json"

function Log([string]$message) {
    Write-Output ("[PORTABLE] " + $message)
}

function Find-Control($parent, [string]$name, [string]$controlTypeName) {
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $elements = $parent.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condition)
    foreach ($element in $elements) {
        if ($element.Current.ControlType.ProgrammaticName -eq $controlTypeName) {
            return $element
        }
    }
    return $null
}

function Find-Button($parent, [string]$name) {
    return Find-Control $parent $name "ControlType.Button"
}

function Find-Edit($parent, [string]$name) {
    return Find-Control $parent $name "ControlType.Edit"
}

function Invoke-Button($button) {
    if (-not $button) { throw "Button not found." }
    $pattern = [System.Windows.Automation.InvokePattern]$button.GetCurrentPattern(
        [System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
}

function Set-EditValue($edit, [string]$value) {
    if (-not $edit) { throw "Edit field not found." }
    $pattern = [System.Windows.Automation.ValuePattern]$edit.GetCurrentPattern(
        [System.Windows.Automation.ValuePattern]::Pattern)
    $pattern.SetValue($value)
}

function Wait-Button($parent, [string]$name) {
    for ($i = 0; $i -lt 40; $i++) {
        $button = Find-Button $parent $name
        if ($button) { return $button }
        Start-Sleep -Milliseconds 200
    }
    return $null
}

function Wait-Edit($parent, [string]$name) {
    for ($i = 0; $i -lt 40; $i++) {
        $edit = Find-Edit $parent $name
        if ($edit) { return $edit }
        Start-Sleep -Milliseconds 200
    }
    return $null
}

function Wait-SettingsWindow([int]$appPid) {
    for ($i = 0; $i -lt 40; $i++) {
        $handle = [PortableUiNative]::FindWindowForProcess([uint32]$appPid, "设置 · 薪资显示器")
        if ($handle -ne [IntPtr]::Zero) {
            return [System.Windows.Automation.AutomationElement]::FromHandle($handle)
        }
        Start-Sleep -Milliseconds 250
    }
    throw "Settings window did not appear."
}

function Get-SettingsWindow([int]$appPid) {
    $handle = [PortableUiNative]::FindWindowForProcess([uint32]$appPid, "设置 · 薪资显示器")
    if ($handle -eq [IntPtr]::Zero) { return $null }
    return [System.Windows.Automation.AutomationElement]::FromHandle($handle)
}

function Find-TextContaining($parent, [string]$needle) {
    $all = $parent.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($element in $all) {
        if ($element.Current.ControlType.ProgrammaticName -eq "ControlType.Text" -and
            $element.Current.Name -like ("*" + $needle + "*")) {
            return $element.Current.Name
        }
    }
    return $null
}

function Wait-TextContaining($parent, [string]$needle) {
    for ($i = 0; $i -lt 40; $i++) {
        $text = Find-TextContaining $parent $needle
        if ($text) { return $text }
        Start-Sleep -Milliseconds 200
    }
    return $null
}

function Start-App {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $testExe
    $psi.WorkingDirectory = $testDirFull
    $psi.UseShellExecute = $false
    $process = [System.Diagnostics.Process]::Start($psi)
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Milliseconds 200
        $process.Refresh()
        if ($process.MainWindowHandle -ne 0) {
            Start-Sleep -Milliseconds 700
            $process.Refresh()
            return $process
        }
    }
    throw "Application window did not appear."
}

function Stop-App($process) {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 300
    }
}

$currentProcess = $null
$scriptFailed = $false
try {
    Log "Preparing isolated portable test folder..."
    if (Test-Path $testDirFull) {
        Remove-Item -LiteralPath $testDirFull -Recurse -Force
    }
    New-Item -ItemType Directory -Path $testDirFull -Force | Out-Null
    Copy-Item -LiteralPath $exe -Destination $testExe -Force
    if (Test-Path $portableSettings) { throw "Portable settings unexpectedly pre-exist." }
    $roamingBefore = if (Test-Path $roamingSettings) { (Get-Item $roamingSettings).LastWriteTimeUtc } else { $null }
    Log "Test exe: $testExe"
    Log "Roaming settings exists: $([bool](Test-Path $roamingSettings))"

    Log "Starting the same release binary from the isolated folder..."
    $currentProcess = Start-App
    $appPid = $currentProcess.Id
    Log "Started pid=$appPid"
    & "D:\AI\build\ProcessIntegrity.exe" $appPid

    $main = [System.Windows.Automation.AutomationElement]::FromHandle($currentProcess.MainWindowHandle)
    $defaultAmount = Wait-TextContaining $main "1,540.23"
    if (-not $defaultAmount) { throw "Default daily amount 1,540.23 was not shown." }
    Log "Default settings loaded from the existing AppData config."

    $settingsButton = Wait-Button $main "设置"
    Invoke-Button $settingsButton
    $settings = Wait-SettingsWindow $appPid
    $salaryEdit = Wait-Edit $settings "月薪"
    Set-EditValue $salaryEdit "40000"
    Start-Sleep -Milliseconds 300
    $saveButton = Find-Button $settings "保存设置"
    Invoke-Button $saveButton

    for ($i = 0; $i -lt 40; $i++) {
        Start-Sleep -Milliseconds 200
        if (-not (Get-SettingsWindow $appPid)) { break }
    }
    if (Get-SettingsWindow $appPid) {
        $visibleError = Find-TextContaining $settings "保存设置失败"
        throw ("Settings window stayed open. Error: " + $visibleError)
    }
    Log "Save completed without an admin prompt or error dialog."

    if (-not (Test-Path $portableSettings)) {
        throw "Portable settings file was not created: $portableSettings"
    }
    $saved = Get-Content -LiteralPath $portableSettings -Raw -Encoding UTF8 | ConvertFrom-Json
    if ([Math]::Abs([double]$saved.monthlySalary - 40000) -gt 0.01) {
        throw ("Portable settings salary was " + $saved.monthlySalary)
    }
    Log "Portable settings written: $portableSettings (salary=40000)."

    if (Test-Path $roamingSettings) {
        $roamingAfter = (Get-Item $roamingSettings).LastWriteTimeUtc
        if ($roamingBefore -ne $roamingAfter) {
            throw "Restricted launch unexpectedly modified the AppData settings."
        }
    }
    Log "AppData settings were left untouched."

    $updatedAmount = Wait-TextContaining $main "1,839.08"
    if (-not $updatedAmount) { throw "Main amount did not update to 1,839.08." }
    Log "Main amount recalculated to 1,839.08 after save."

    $closeButton = Wait-Button $main "退出"
    Invoke-Button $closeButton
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Milliseconds 200
        if ($currentProcess.HasExited) { break }
    }
    if (-not $currentProcess.HasExited) { throw "Application did not exit." }
    $currentProcess = $null

    Log "Restarting to verify persistence..."
    $currentProcess = Start-App
    $appPid = $currentProcess.Id
    $main = [System.Windows.Automation.AutomationElement]::FromHandle($currentProcess.MainWindowHandle)
    $restartedAmount = Wait-TextContaining $main "1,839.08"
    if (-not $restartedAmount) { throw "Restart did not load the portable settings." }
    Log "Restart loaded salary=40000 from the portable file."

    $settingsButton = Wait-Button $main "设置"
    Invoke-Button $settingsButton
    $settings = Wait-SettingsWindow $appPid
    $salaryEdit = Wait-Edit $settings "月薪"
    $valuePattern = [System.Windows.Automation.ValuePattern]$salaryEdit.GetCurrentPattern(
        [System.Windows.Automation.ValuePattern]::Pattern)
    Log "Salary field after restart: $($valuePattern.Current.Value)"
    $cancelButton = Find-Button $settings "取消"
    if ($cancelButton) { Invoke-Button $cancelButton }

    Write-Output "PORTABLE UI TEST: PASS"
}
catch {
    $scriptFailed = $true
    Write-Output ("[PORTABLE] FAILED: " + $_.Exception.Message)
    Write-Output $_.ScriptStackTrace
}
finally {
    Stop-App $currentProcess
}
if ($scriptFailed) { exit 1 }
exit 0


