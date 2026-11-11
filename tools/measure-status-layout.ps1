Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName WindowsBase

foreach ($fontSize in @(10, 14, 20, 28)) {
    $border = New-Object System.Windows.Controls.Border
    $border.MinHeight = 24
    $border.Padding = [System.Windows.Thickness]::new(10, 4, 12, 4)
    $border.BorderThickness = [System.Windows.Thickness]::new(1)
    $border.CornerRadius = [System.Windows.CornerRadius]::new(12)

    $panel = New-Object System.Windows.Controls.StackPanel
    $panel.Orientation = [System.Windows.Controls.Orientation]::Horizontal
    $text = New-Object System.Windows.Controls.TextBlock
    $text.Text = "今日已下班"
    $text.FontSize = $fontSize
    $text.FontFamily = [System.Windows.Media.FontFamily]::new("Microsoft YaHei UI")
    $text.VerticalAlignment = [System.Windows.VerticalAlignment]::Center
    $text.Padding = [System.Windows.Thickness]::new(0, 0, 1, 0)
    $panel.Children.Add($text) | Out-Null
    $border.Child = $panel

    $border.Measure([System.Windows.Size]::new(400, 200))
    $border.Arrange(([System.Windows.Rect]::new(0, 0, 400, 200)))
    $border.UpdateLayout()
    Write-Output ("FontSize={0}: pill={1:N2} text={2:N2} desired={3:N2}" -f `
        $fontSize, $border.ActualHeight, $text.ActualHeight, $border.DesiredSize.Height)
}



