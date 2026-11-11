Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName WindowsBase

$typeface = New-Object System.Windows.Media.Typeface('Microsoft YaHei UI')
$strings = @('今日已下班', '上午工作中', '下午工作中', '周末休息', '未上班')
foreach ($s in $strings) {
    $ft = New-Object System.Windows.Media.FormattedText(
        $s,
        [Globalization.CultureInfo]::GetCultureInfo('zh-CN'),
        [System.Windows.FlowDirection]::LeftToRight,
        $typeface,
        10,
        [System.Windows.Media.Brushes]::White,
        1.0)
    Write-Output ('{0}: Width={1:N3} Trailing={2:N3} Height={3:N3} Baseline={4:N3}' -f `
        $s, $ft.Width, $ft.OverhangTrailing, $ft.Height, $ft.Baseline)
}

