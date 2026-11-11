param()

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class ExtraUiNative {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    public static IntPtr Find(uint processId, string titlePart) {
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

$exe = Join-Path $PSScriptRoot "..\build\SalaryMonitor.UITest.exe"
$configRoot = Join-Path $PSScriptRoot "..\build\ui-extra-config"
$configPath = Join-Path $configRoot "SalaryMonitor\settings.json"

function Log([string]$message) { Write-Output ("[EXTRA] " + $message) }

function Get-AppProcess {
    return Get-Process -Name "SalaryMonitor.UITest" -ErrorAction SilentlyContinue |
        Select-Object -First 1
}

function Stop-App {
    $process = Get-AppProcess
    if ($process) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 300
    }
}

function Start-App {
    Remove-Item Env:\SALARY_MONITOR_SCREENSHOT_DIR -ErrorAction SilentlyContinue
    & $exe
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Milliseconds 200
        $process = Get-AppProcess
        if ($process) {
            $process.Refresh()
            if ($process.MainWindowHandle -ne 0) {
                Start-Sleep -Milliseconds 800
                $process.Refresh()
                return $process
            }
        }
    }
    throw "Application window did not appear."
}

function Find-Control($parent, [string]$name, [string]$type) {
    if (-not $parent) { return $null }
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $elements = $parent.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condition)
    foreach ($element in $elements) {
        if ($element.Current.ControlType.ProgrammaticName -eq $type) {
            return $element
        }
    }
    return $null
}

function Wait-Control($parent, [string]$name, [string]$type, [int]$attempts = 30) {
    for ($i = 0; $i -lt $attempts; $i++) {
        $element = Find-Control $parent $name $type
        if ($element) { return $element }
        Start-Sleep -Milliseconds 200
    }
    return $null
}

function Wait-Text([System.Windows.Automation.AutomationElement]$parent,
    [string]$needle, [int]$attempts = 30) {
    for ($i = 0; $i -lt $attempts; $i++) {
        $all = $parent.FindAll([System.Windows.Automation.TreeScope]::Descendants,
            [System.Windows.Automation.Condition]::TrueCondition)
        foreach ($element in $all) {
            if ($element.Current.ControlType.ProgrammaticName -eq "ControlType.Text" -and
                $element.Current.Name -like ("*" + $needle + "*")) {
                return $element.Current.Name
            }
        }
        Start-Sleep -Milliseconds 200
    }
    return $null
}

function Wait-Settings([int]$appPid) {
    for ($i = 0; $i -lt 40; $i++) {
        $handle = [ExtraUiNative]::Find([uint32]$appPid, "设置 · 薪资显示器")
        if ($handle -ne [IntPtr]::Zero) {
            return [System.Windows.Automation.AutomationElement]::FromHandle($handle)
        }
        Start-Sleep -Milliseconds 200
    }
    return $null
}

function Invoke-Control($element) {
    if (-not $element) { throw "A required UI control was not found." }
    $pattern = [System.Windows.Automation.InvokePattern]$element.GetCurrentPattern(
        [System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
}

function Set-Edit([System.Windows.Automation.AutomationElement]$edit, [string]$value) {
    if (-not $edit) { throw "A required text input was not found." }
    $pattern = [System.Windows.Automation.ValuePattern]$edit.GetCurrentPattern(
        [System.Windows.Automation.ValuePattern]::Pattern)
    $pattern.SetValue($value)
}

function Get-Edit([System.Windows.Automation.AutomationElement]$edit) {
    $pattern = [System.Windows.Automation.ValuePattern]$edit.GetCurrentPattern(
        [System.Windows.Automation.ValuePattern]::Pattern)
    return $pattern.Current.Value
}

$failed = $false
try {
    Log "Cleaning test state."
    Stop-App
    Remove-Item $configRoot -Recurse -Force -ErrorAction SilentlyContinue
    $env:SALARY_MONITOR_TEST_NOW = "2026-11-05 16:28:35"
    $env:SALARY_MONITOR_TEST_RUNNING = "0"
    $env:SALARY_MONITOR_CONFIG_DIR = [System.IO.Path]::GetFullPath($configRoot)

    Log "Launching for time-validation and reset tests."
    $process = Start-App
    $appPid = $process.Id
    Start-Sleep -Milliseconds 700
    $process.Refresh()
    $main = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    if (-not (Wait-Text $main "1,052.49")) { throw "Main view not ready." }
    $settingsButton = Wait-Control $main "设置" "ControlType.Button"
    Invoke-Control $settingsButton
    $settings = Wait-Settings $appPid
    if (-not $settings) { throw "Settings window not found." }

    $startEdit = Wait-Control $settings "上班时间" "ControlType.Edit"
    Set-Edit $startEdit "25:00"
    $saveButton = Wait-Control $settings "保存设置" "ControlType.Button"
    Invoke-Control $saveButton
    $errorText = Wait-Text $settings "请输入有效时间"
    if (-not $errorText) { throw "Invalid time did not show an inline error." }
    $handle = [ExtraUiNative]::Find([uint32]$appPid, "设置 · 薪资显示器")
    if ($handle -eq [IntPtr]::Zero) { throw "Settings window closed after invalid time." }
    Log ("Invalid time blocked: " + $errorText)

    $resetButton = Wait-Control $settings "恢复默认值" "ControlType.Button"
    Invoke-Control $resetButton
    Start-Sleep -Milliseconds 400
    $salaryEdit = Wait-Control $settings "月薪" "ControlType.Edit"
    $startValue = Get-Edit $startEdit
    $salaryValue = Get-Edit $salaryEdit
    if ($startValue -ne "09:30") { throw "Reset did not restore 09:30 (was $startValue)." }
    if ($salaryValue -ne "33,500.00") { throw "Reset did not restore salary (was $salaryValue)." }
    Log "Reset restored 09:30 and 33,500.00 (not yet persisted)."

    $cancelButton = Wait-Control $settings "取消" "ControlType.Button"
    Invoke-Control $cancelButton
    Start-Sleep -Milliseconds 300
    $closeButton = Wait-Control $main "退出" "ControlType.Button"
    Invoke-Control $closeButton
    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 200
        if (-not (Get-AppProcess)) { break }
    }
    if (Get-AppProcess) { throw "Application did not close." }

    Log "Launching with a corrupt settings file."
    $settingsDir = Join-Path $configRoot "SalaryMonitor"
    New-Item -ItemType Directory -Path $settingsDir -Force | Out-Null
    [System.IO.File]::WriteAllText($configPath, "{ this is not valid json",
        (New-Object System.Text.UTF8Encoding($false)))
    $process = Start-App
    try {
        Start-Sleep -Milliseconds 700
        $process.Refresh()
        $main = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
        if (-not (Wait-Text $main "1,052.49")) {
            throw "Corrupt configuration did not fall back to defaults."
        }
        Log "Corrupt config fallback displayed default rates and amount."
        $closeButton = Wait-Control $main "退出" "ControlType.Button"
        Invoke-Control $closeButton
        Start-Sleep -Milliseconds 400
    }
    finally {
        Stop-App
    }

    Log "EXTRA UI CHECKS: PASS"
}
catch {
    $failed = $true
    Write-Output ("[EXTRA] FAILED: " + $_.Exception.Message)
    Write-Output $_.ScriptStackTrace
}
finally {
    Stop-App
}
if ($failed) { exit 1 }
exit 0
