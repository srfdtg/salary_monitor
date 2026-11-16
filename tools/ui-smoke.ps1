param(
    [string]$ExePath = (Join-Path $PSScriptRoot "..\build\SalaryMonitor.UITest.exe")
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class SalaryUiNative {
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

$root = $PSScriptRoot
$configRoot = Join-Path $root "..\build\ui-automation-config"
$tracePath = Join-Path $root "..\build\ui-trace.log"
$exe = [System.IO.Path]::GetFullPath($ExePath)
$configPath = Join-Path $configRoot "SalaryMonitor\settings.json"

function Log([string]$message) {
    Write-Output ("[UI] " + $message)
}

function Get-AppProcess {
    return Get-Process -Name "SalaryMonitor.UITest" -ErrorAction SilentlyContinue |
        Select-Object -First 1
}

function Start-App {
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

function Stop-App {
    $process = Get-AppProcess
    if ($process) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 300
    }
}

function Find-SettingsHandle([int]$appPid) {
    return [SalaryUiNative]::FindWindowForProcess([uint32]$appPid, "设置 · 薪资显示器")
}

function Wait-SettingsWindow([int]$appPid) {
    for ($i = 0; $i -lt 40; $i++) {
        $handle = Find-SettingsHandle $appPid
        if ($handle -ne [IntPtr]::Zero) {
            return [System.Windows.Automation.AutomationElement]::FromHandle($handle)
        }
        Start-Sleep -Milliseconds 250
    }
    throw "Settings window did not appear."
}

function Get-SettingsWindow([int]$appPid) {
    $handle = Find-SettingsHandle $appPid
    if ($handle -eq [IntPtr]::Zero) {
        return $null
    }
    return [System.Windows.Automation.AutomationElement]::FromHandle($handle)
}

function Find-Control([System.Windows.Automation.AutomationElement]$parent,
    [string]$name, [string]$controlTypeName) {
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

function Find-Button([System.Windows.Automation.AutomationElement]$parent, [string]$name) {
    return Find-Control $parent $name "ControlType.Button"
}

function Find-Edit([System.Windows.Automation.AutomationElement]$parent, [string]$name) {
    return Find-Control $parent $name "ControlType.Edit"
}

function Invoke-ButtonElement([System.Windows.Automation.AutomationElement]$button) {
    if (-not $button) { throw "Button not found." }
    $pattern = [System.Windows.Automation.InvokePattern]$button.GetCurrentPattern(
        [System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
}

function Set-EditValue([System.Windows.Automation.AutomationElement]$edit, [string]$value) {
    if (-not $edit) { throw "Edit field not found." }
    $pattern = [System.Windows.Automation.ValuePattern]$edit.GetCurrentPattern(
        [System.Windows.Automation.ValuePattern]::Pattern)
    $pattern.SetValue($value)
}

function Read-Settings {
    if (-not (Test-Path $configPath)) { return $null }
    return Get-Content -Raw $configPath | ConvertFrom-Json
}

function Wait-SettingsSaved([decimal]$salary) {
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Milliseconds 200
        $settings = Read-Settings
        if ($settings -and [Math]::Abs([double]$settings.monthlySalary - [double]$salary) -lt 0.01) {
            return $settings
        }
    }
    throw "Settings were not saved to disk."
}

function Find-TextContaining([System.Windows.Automation.AutomationElement]$parent, [string]$needle) {
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

function Wait-TextContaining([System.Windows.Automation.AutomationElement]$parent,
    [string]$needle, [int]$attempts = 30) {
    for ($i = 0; $i -lt $attempts; $i++) {
        $value = Find-TextContaining $parent $needle
        if ($value) { return $value }
        Start-Sleep -Milliseconds 200
    }
    return $null
}

function Wait-Button([System.Windows.Automation.AutomationElement]$parent, [string]$name) {
    for ($i = 0; $i -lt 30; $i++) {
        $button = Find-Button $parent $name
        if ($button) { return $button }
        Start-Sleep -Milliseconds 200
    }
    return $null
}

function Wait-Edit([System.Windows.Automation.AutomationElement]$parent, [string]$name) {
    for ($i = 0; $i -lt 30; $i++) {
        $edit = Find-Edit $parent $name
        if ($edit) { return $edit }
        Start-Sleep -Milliseconds 200
    }
    return $null
}

$scriptFailed = $false
$currentProcess = $null
try {
    Log "Stopping any stale test instance..."
    Stop-App
    if (Test-Path $configRoot) {
        Remove-Item $configRoot -Recurse -Force
    }
    if (Test-Path $tracePath) {
        Remove-Item $tracePath -Force
    }
    $env:SALARY_MONITOR_TEST_NOW = "2026-11-05 16:28:35"
    $env:SALARY_MONITOR_CONFIG_DIR = [System.IO.Path]::GetFullPath($configRoot)
    Remove-Item Env:\SALARY_MONITOR_SCREENSHOT_DIR -ErrorAction SilentlyContinue

    Log "Launching test build..."
    $currentProcess = Start-App
    $appPid = $currentProcess.Id
    $main = [System.Windows.Automation.AutomationElement]::FromHandle($currentProcess.MainWindowHandle)

    $amount = Wait-TextContaining $main "1,052.49"
    if (-not $amount) { throw "Expected main amount 1,052.49 was not found." }
    Log "Main view shows 1,052.49 at fixed 16:28:35."

    Log "Switching to the month view..."
    $monthTab = Wait-Button $main "本月视图"
    if (-not $monthTab) { throw "Month tab was not found." }
    Invoke-ButtonElement $monthTab
    if (-not (Wait-TextContaining $main "5,673.18")) {
        throw "Month-to-date amount 5,673.18 was not found."
    }
    if (-not (Wait-TextContaining $main "3.7 / 21 天")) {
        throw "Month workday progress 3.7 / 21 was not found."
    }
    if (-not (Wait-TextContaining $main "发薪日未设置")) {
        throw "Unset payday hint was not found."
    }
    Log "Month view shows 5,673.18, 3.7 / 21 workdays, and unset payday."

    Log "Testing amount hiding on the month view..."
    $hideMonthButton = Wait-Button $main "隐藏金额"
    Invoke-ButtonElement $hideMonthButton
    if (-not (Wait-TextContaining $main "••••••")) {
        throw "Month amount was not masked."
    }
    if (-not (Wait-TextContaining $main "¥••••")) {
        throw "Month daily wage was not masked."
    }
    $showMonthButton = Wait-Button $main "显示金额"
    Invoke-ButtonElement $showMonthButton
    if (-not (Wait-TextContaining $main "5,673.18")) {
        throw "Month amount was not restored."
    }
    $todayTab = Wait-Button $main "今日视图"
    Invoke-ButtonElement $todayTab
    if (-not (Wait-TextContaining $main "1,052.49")) {
        throw "Today amount was not restored after leaving the month view."
    }
    Log "Month privacy toggle and tab switching work."

    Log "Testing the eye button hide/show toggle..."
    $hideButton = Wait-Button $main "隐藏金额"
    if (-not $hideButton) { throw "Hide-amount eye button was not found." }
    Invoke-ButtonElement $hideButton
    if (-not (Wait-TextContaining $main "••••••")) {
        throw "Main amount was not masked after clicking the eye button."
    }
    if (-not (Wait-TextContaining $main "¥••••")) {
        throw "Rate strip was not masked after clicking the eye button."
    }
    $hiddenPersisted = $false
    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 200
        $hiddenSettings = Read-Settings
        if ($hiddenSettings -and $hiddenSettings.amountsHidden -eq $true) {
            $hiddenPersisted = $true
            break
        }
    }
    if (-not $hiddenPersisted) { throw "Hidden-amount state was not saved." }

    Log "Restarting to verify hidden amounts persist..."
    $closeHidden = Find-Button $main "退出"
    Invoke-ButtonElement $closeHidden
    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 200
        if (-not (Get-AppProcess)) { break }
    }
    if (Get-AppProcess) { throw "Application did not exit while amounts were hidden." }
    $currentProcess = Start-App
    $appPid = $currentProcess.Id
    $main = [System.Windows.Automation.AutomationElement]::FromHandle($currentProcess.MainWindowHandle)
    if (-not (Wait-TextContaining $main "••••••")) {
        throw "Hidden amounts were not restored after restart."
    }
    $showButton = Wait-Button $main "显示金额"
    if (-not $showButton) { throw "Show-amount eye button was not found after restart." }
    Invoke-ButtonElement $showButton
    if (-not (Wait-TextContaining $main "1,052.49")) {
        throw "Main amount was not restored after clicking the eye button again."
    }
    Log "Eye button masked, persisted across restart, and restored all salary values."

    $settingsButton = Wait-Button $main "设置"
    Invoke-ButtonElement $settingsButton
    $settingsWindow = Wait-SettingsWindow $appPid
    Log "Settings window opened."

    $salaryEdit = Wait-Edit $settingsWindow "月薪"
    Set-EditValue $salaryEdit "40000"
    $paydayEdit = Wait-Edit $settingsWindow "发薪日（可选）"
    Set-EditValue $paydayEdit "15"
    Start-Sleep -Milliseconds 300
    $previewText = Wait-TextContaining $settingsWindow "¥1,839.08"
    if (-not $previewText) { throw "Live income preview did not update to ¥1,839.08." }
    if (-not (Wait-TextContaining $settingsWindow "11月15日")) {
        throw "Payday preview did not update to 11月15日."
    }
    Log "Live preview updates for 40,000 salary and payday 15."

    $saveButton = Find-Button $settingsWindow "保存设置"
    Invoke-ButtonElement $saveButton
    $saved = Wait-SettingsSaved 40000
    Start-Sleep -Milliseconds 400
    if (Get-SettingsWindow $appPid) { throw "Settings window stayed open after valid save." }
    Log "Valid save wrote settings.json (salary=40000)."

    $updatedAmount = Wait-TextContaining $main "1,256.70"
    if (-not $updatedAmount) { throw "Main amount did not update to 1,256.70 after save." }
    Log "Main view recalculated to 1,256.70 after save."

    $savedWithPayday = Read-Settings
    if (-not $savedWithPayday -or $savedWithPayday.payday -ne 15) {
        throw "Payday 15 was not saved."
    }
    $monthTab = Wait-Button $main "本月视图"
    Invoke-ButtonElement $monthTab
    if (-not (Wait-TextContaining $main "6,773.95")) {
        throw "Month amount did not update to 6,773.95 after salary save."
    }
    if (-not (Wait-TextContaining $main "距下次发薪")) {
        throw "Payday countdown was not shown."
    }
    if (-not (Wait-TextContaining $main "11月15日")) {
        throw "Next payday date was not shown on the month page."
    }
    $todayTab = Wait-Button $main "今日视图"
    Invoke-ButtonElement $todayTab
    Log "Month view and payday countdown updated after settings save."

    Log "Testing invalid salary rejection..."
    $settingsButton = Wait-Button $main "设置"
    Invoke-ButtonElement $settingsButton
    $settingsWindow = Wait-SettingsWindow $appPid
    $salaryEdit = Wait-Edit $settingsWindow "月薪"
    Set-EditValue $salaryEdit "0"
    $saveButton = Find-Button $settingsWindow "保存设置"
    Invoke-ButtonElement $saveButton
    Start-Sleep -Milliseconds 600
    if (-not (Get-SettingsWindow $appPid)) { throw "Settings window closed despite invalid salary." }
    $errorText = Find-TextContaining $settingsWindow "月薪必须大于"
    if (-not $errorText) { throw "Inline salary validation message was not found." }
    $unchanged = Read-Settings
    if ([Math]::Abs([double]$unchanged.monthlySalary - 40000) -gt 0.01) {
        throw "Invalid save overwrote the persisted settings."
    }
    Log ("Invalid save blocked; message: " + $errorText)
    $cancelButton = Find-Button $settingsWindow "取消"
    Invoke-ButtonElement $cancelButton
    Start-Sleep -Milliseconds 400

    Log "Testing topmost toggle through settings..."
    $settingsButton = Wait-Button $main "设置"
    Invoke-ButtonElement $settingsButton
    $settingsWindow = Wait-SettingsWindow $appPid
    $topmostToggle = Find-Control $settingsWindow "窗口置顶开关" "ControlType.CheckBox"
    if (-not $topmostToggle) { throw "Topmost toggle was not found." }
    $togglePattern = [System.Windows.Automation.TogglePattern]$topmostToggle.GetCurrentPattern(
        [System.Windows.Automation.TogglePattern]::Pattern)
    Log ("Toggle state before click: " + $togglePattern.Current.ToggleState)
    $togglePattern.Toggle()
    Start-Sleep -Milliseconds 250
    Log ("Toggle state after click: " + $togglePattern.Current.ToggleState)
    $saveButton = Find-Button $settingsWindow "保存设置"
    Invoke-ButtonElement $saveButton
    $afterToggle = $null
    for ($i = 0; $i -lt 40; $i++) {
        Start-Sleep -Milliseconds 200
        $afterToggle = Read-Settings
        $settingsStillOpen = Get-SettingsWindow $appPid
        if (-not $settingsStillOpen -and $afterToggle -and $afterToggle.alwaysOnTop -eq $false) {
            break
        }
    }
    if ($afterToggle.alwaysOnTop -ne $false) { throw "Topmost=false was not persisted." }
    if ($afterToggle.payday -ne 15) { throw "Payday was lost while saving topmost." }
    if (Get-SettingsWindow $appPid) { throw "Settings window did not close after topmost save." }
    $pinOn = Find-Button $main "保持置顶"
    if (-not $pinOn) { throw "Main pin button was not updated after disabling topmost." }
    Log "Topmost disabled via settings and reflected by main pin."

    Log "Testing the main pin shortcut..."
    Invoke-ButtonElement $pinOn
    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 200
        $afterPin = Read-Settings
        if ($afterPin -and $afterPin.alwaysOnTop -eq $true) { break }
    }
    if (-not $afterPin -or $afterPin.alwaysOnTop -ne $true) { throw "Pin shortcut did not persist topmost=true." }
    $pinOff = Find-Button $main "取消置顶"
    if (-not $pinOff) { throw "Pin shortcut did not update its tooltip/state." }
    Log "Pin shortcut re-enabled topmost and persisted it."

    $closeButton = Find-Button $main "退出"
    Invoke-ButtonElement $closeButton
    for ($i = 0; $i -lt 20; $i++) {
        Start-Sleep -Milliseconds 200
        if (-not (Get-AppProcess)) { break }
    }
    if (Get-AppProcess) { throw "Application did not exit after clicking close." }
    $currentProcess = $null
    Log "Application closed cleanly."

    Log "Restarting to verify persisted salary, settings and pin..."
    $currentProcess = Start-App
    $appPid = $currentProcess.Id
    $main = [System.Windows.Automation.AutomationElement]::FromHandle($currentProcess.MainWindowHandle)
    if (-not (Wait-TextContaining $main "1,256.70")) {
        throw "Persisted salary was not applied on restart."
    }
    if (-not (Wait-Button $main "取消置顶")) {
        throw "Persisted topmost state was not applied on restart."
    }
    $persistedPayday = Read-Settings
    if (-not $persistedPayday -or $persistedPayday.payday -ne 15) {
        throw "Persisted payday was not applied on restart."
    }
    $monthTab = Wait-Button $main "本月视图"
    Invoke-ButtonElement $monthTab
    if (-not (Wait-TextContaining $main "6,773.95")) {
        throw "Persisted month amount was not shown after restart."
    }
    if (-not (Wait-TextContaining $main "11月15日")) {
        throw "Persisted payday was not shown after restart."
    }
    $todayTab = Wait-Button $main "今日视图"
    Invoke-ButtonElement $todayTab
    Log "Restart applied persisted salary, topmost state, month amount and payday."
    $closeButton = Find-Button $main "退出"
    Invoke-ButtonElement $closeButton
    Start-Sleep -Milliseconds 500
    Log "UI AUTOMATION SMOKE: PASS"
}
catch {
    $scriptFailed = $true
    Write-Output ("[UI] FAILED: " + $_.Exception.Message)
    Write-Output $_.ScriptStackTrace
}
finally {
    if (Test-Path $tracePath) {
        Write-Output "--- application trace ---"
        Get-Content $tracePath
    }
    Stop-App
}
if ($scriptFailed) { exit 1 }
exit 0
