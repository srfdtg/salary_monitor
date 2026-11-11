using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SalaryMonitor.Core;

namespace SalaryMonitor.UI
{
    internal sealed class SettingsField
    {
        public string Key { get; private set; }
        public StackPanel Root { get; private set; }
        public Border Host { get; private set; }
        public TextBox Input { get; private set; }
        public TextBlock Error { get; private set; }
        public bool HasError { get; private set; }

        public SettingsField(string key, StackPanel root, Border host, TextBox input, TextBlock error)
        {
            this.Key = key;
            this.Root = root;
            this.Host = host;
            this.Input = input;
            this.Error = error;
        }

        public void ShowError(string message)
        {
            HasError = true;
            Error.Text = message ?? string.Empty;
            Error.Visibility = Visibility.Visible;
            Host.BorderBrush = Theme.ErrorBorder;
        }

        public void ClearError()
        {
            HasError = false;
            Error.Text = string.Empty;
            Error.Visibility = Visibility.Collapsed;
            Host.BorderBrush = Input.IsKeyboardFocusWithin ? Theme.InputFocusBorder : Theme.InputBorder;
        }

        public void FocusInput()
        {
            Input.Focus();
            Root.BringIntoView();
        }
    }

    internal sealed class SettingsWindow : Window
    {
        private readonly SettingsStore _store;
        private readonly AppSettings _original;
        private AppSettings _savedSettings;

        private readonly List<SettingsField> _fields = new List<SettingsField>();
        private SettingsField _salaryField;
        private SettingsField _daysField;
        private SettingsField _startField;
        private SettingsField _lunchStartField;
        private SettingsField _lunchEndField;
        private SettingsField _endField;

        private ToggleSwitch _topmostToggle;
        private ToggleSwitch _actualWorkdaysToggle;
        private TextBlock _actualWorkdaysHint;
        private ColumnDefinition _daysGapColumn;
        private ColumnDefinition _daysColumn;
        private TextBlock _incomePreview;
        private TextBlock _schedulePreview;
        private TextBlock _saveError;

        public AppSettings SavedSettings
        {
            get { return _savedSettings; }
        }

        public SettingsWindow(AppSettings original, SettingsStore store)
        {
            _original = original == null ? AppSettings.CreateDefault() : original.Clone();
            _store = store;

            Title = "设置 · 薪资显示器";
            Width = 604;
            Height = 700;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            Icon = AppIcon.Load();
            PreviewKeyDown += OnPreviewKeyDown;

            Content = BuildRoot();
            Loaded += OnLoaded;
            UpdatePreview();
            Program.Log("SettingsWindow ctor complete, title=" + Title);
        }

        private UIElement BuildRoot()
        {
            Grid root = new Grid();
            root.Background = Brushes.Transparent;

            Border card = Theme.Card(26);
            card.Margin = new Thickness(22);

            Grid content = new Grid();
            content.Margin = new Thickness(26, 20, 26, 18);
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            UIElement header = BuildHeader();
            content.Children.Add(header);
            Grid.SetRow(header, 0);

            ScrollViewer scroll = new ScrollViewer();
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            scroll.Focusable = false;
            scroll.Margin = new Thickness(0, 8, 0, 0);
            scroll.Content = BuildSections();
            content.Children.Add(scroll);
            Grid.SetRow(scroll, 1);

            UIElement footer = BuildFooter();
            content.Children.Add(footer);
            Grid.SetRow(footer, 2);

            Theme.CardSurface(card, 26, content);
            root.Children.Add(card);
            return root;
        }

        private UIElement BuildHeader()
        {
            Grid header = new Grid();
            header.Background = Brushes.Transparent;
            header.MouseLeftButtonDown += OnHeaderMouseDown;
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titleStack = new StackPanel();
            titleStack.VerticalAlignment = VerticalAlignment.Top;

            TextBlock kicker = Theme.UiText("P R E F E R E N C E S", 8.5, Theme.Accent);
            kicker.FontWeight = FontWeights.SemiBold;
            TextBlock title = Theme.CnText("设置", 27, Theme.TextPrimary, FontWeights.SemiBold);
            title.Margin = new Thickness(0, 5, 0, 2);
            TextBlock subtitle = Theme.CnText("参数保存后立即生效，今日工资将按新规则重新估算",
                9.5, Theme.TextSecondary, FontWeights.Normal);
            titleStack.Children.Add(kicker);
            titleStack.Children.Add(title);
            titleStack.Children.Add(subtitle);
            header.Children.Add(titleStack);

            IconButton closeButton = new IconButton(AppIcons.Close, "关闭设置", true);
            closeButton.HorizontalAlignment = HorizontalAlignment.Right;
            closeButton.VerticalAlignment = VerticalAlignment.Top;
            closeButton.Margin = new Thickness(8, 2, 0, 0);
            closeButton.Click += delegate { Close(); };
            header.Children.Add(closeButton);
            Grid.SetColumn(closeButton, 1);

            return header;
        }

        private UIElement BuildSections()
        {
            StackPanel stack = new StackPanel();
            stack.Margin = new Thickness(0, 4, 0, 0);

            stack.Children.Add(CreateSectionLabel("收入参数", "INCOME", false));
            stack.Children.Add(BuildIncomeCard());

            stack.Children.Add(CreateSectionLabel("工作时间", "SCHEDULE", true));
            stack.Children.Add(BuildScheduleCard());

            stack.Children.Add(CreateSectionLabel("窗口", "WINDOW", true));
            stack.Children.Add(BuildWindowCard());

            _saveError = Theme.CnText(string.Empty, 9.0, Theme.Danger, FontWeights.Normal);
            _saveError.TextWrapping = TextWrapping.Wrap;
            _saveError.Margin = new Thickness(0, 10, 0, 4);
            _saveError.Visibility = Visibility.Collapsed;
            stack.Children.Add(_saveError);

            return stack;
        }

        private static UIElement CreateSectionLabel(string chinese, string english, bool withTopMargin)
        {
            StackPanel row = new StackPanel();
            row.Orientation = Orientation.Horizontal;
            row.VerticalAlignment = VerticalAlignment.Center;
            row.Margin = new Thickness(0, withTopMargin ? 16 : 0, 0, 7);

            TextBlock cn = Theme.CnText(chinese, 10.5, Theme.TextSoft, FontWeights.SemiBold);
            cn.Margin = new Thickness(0, 0, 8, 0);
            TextBlock en = Theme.UiText(english, 8, Theme.TextMuted);
            en.FontWeight = FontWeights.SemiBold;
            row.Children.Add(cn);
            row.Children.Add(en);
            return row;
        }

        private UIElement BuildIncomeCard()
        {
            Border card = Theme.Panel(Theme.PanelBackground, Theme.PanelBorder, 13, 1);
            StackPanel content = new StackPanel();

            Grid modeRow = new Grid();
            modeRow.Margin = new Thickness(14, 12, 14, 0);
            modeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            modeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel modeLabels = new StackPanel();
            TextBlock modeTitle = Theme.CnText("按当月实际工作日计薪", 9.5,
                Theme.TextSoft, FontWeights.Normal);
            TextBlock modeHint = Theme.CnText("自动应用 2026/2027 法定节假日与调休", 8.5,
                Theme.TextMuted, FontWeights.Normal);
            modeHint.Margin = new Thickness(0, 3, 0, 0);
            modeLabels.Children.Add(modeTitle);
            modeLabels.Children.Add(modeHint);
            modeRow.Children.Add(modeLabels);

            _actualWorkdaysToggle = new ToggleSwitch();
            _actualWorkdaysToggle.IsChecked = _original.ActualWorkdaysEnabled;
            _actualWorkdaysToggle.VerticalAlignment = VerticalAlignment.Center;
            _actualWorkdaysToggle.HorizontalAlignment = HorizontalAlignment.Right;
            AutomationProperties.SetName(_actualWorkdaysToggle, "按当月实际工作日计薪开关");
            _actualWorkdaysToggle.Checked += delegate
            {
                UpdateActualMode();
                UpdatePreview();
            };
            _actualWorkdaysToggle.Unchecked += delegate
            {
                UpdateActualMode();
                UpdatePreview();
            };
            modeRow.Children.Add(_actualWorkdaysToggle);
            Grid.SetColumn(_actualWorkdaysToggle, 1);
            content.Children.Add(modeRow);

            Border divider = new Border();
            divider.Height = 1;
            divider.Background = Theme.Divider;
            divider.Margin = new Thickness(14, 10, 14, 0);
            content.Children.Add(divider);

            Grid grid = new Grid();
            grid.Margin = new Thickness(14, 10, 14, 8);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _daysGapColumn = new ColumnDefinition { Width = new GridLength(12) };
            grid.ColumnDefinitions.Add(_daysGapColumn);
            _daysColumn = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
            grid.ColumnDefinitions.Add(_daysColumn);

            _salaryField = CreateField("MonthlySalary", "月薪",
                Formatters.MoneyValue(_original.MonthlySalary), "元 / 月", 16, false);
            _daysField = CreateField("MonthlyWorkDays", "月计薪天数",
                Formatters.DaysInput(_original.MonthlyWorkDays), "天", 8, false);

            grid.Children.Add(_salaryField.Root);
            Grid.SetColumn(_salaryField.Root, 0);
            grid.Children.Add(_daysField.Root);
            Grid.SetColumn(_daysField.Root, 2);
            content.Children.Add(grid);

            _actualWorkdaysHint = Theme.CnText(string.Empty, 8.5, Theme.TextMuted,
                FontWeights.Normal);
            _actualWorkdaysHint.TextWrapping = TextWrapping.Wrap;
            _actualWorkdaysHint.Margin = new Thickness(14, 0, 14, 8);
            content.Children.Add(_actualWorkdaysHint);

            _incomePreview = Theme.CnText("日薪 —　·　分钟薪 —", 9.0,
                Theme.Frozen(255, 181, 245, 244), FontWeights.Normal);
            content.Children.Add(CreateCalculationStrip(_incomePreview, "自动计算"));
            card.Child = content;
            UpdateActualMode();
            return card;
        }

        private UIElement BuildScheduleCard()
        {
            Border card = Theme.Panel(Theme.PanelBackground, Theme.PanelBorder, 13, 1);
            StackPanel content = new StackPanel();

            Grid grid = new Grid();
            grid.Margin = new Thickness(14, 12, 14, 8);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _startField = CreateField("WorkStartMinutes", "上班时间",
                Formatters.ClockHHmm(_original.WorkStartMinutes), "上午", 5, true);
            _endField = CreateField("WorkEndMinutes", "下班时间",
                Formatters.ClockHHmm(_original.WorkEndMinutes), "晚上", 5, true);
            _lunchStartField = CreateField("LunchStartMinutes", "午休开始",
                Formatters.ClockHHmm(_original.LunchStartMinutes), "中午", 5, true);
            _lunchEndField = CreateField("LunchEndMinutes", "午休结束",
                Formatters.ClockHHmm(_original.LunchEndMinutes), "下午", 5, true);

            grid.Children.Add(_startField.Root);
            Grid.SetColumn(_startField.Root, 0);
            Grid.SetRow(_startField.Root, 0);
            grid.Children.Add(_endField.Root);
            Grid.SetColumn(_endField.Root, 2);
            Grid.SetRow(_endField.Root, 0);

            grid.Children.Add(_lunchStartField.Root);
            Grid.SetColumn(_lunchStartField.Root, 0);
            Grid.SetRow(_lunchStartField.Root, 2);
            grid.Children.Add(_lunchEndField.Root);
            Grid.SetColumn(_lunchEndField.Root, 2);
            Grid.SetRow(_lunchEndField.Root, 2);
            content.Children.Add(grid);

            _schedulePreview = Theme.CnText("每日有效工时 —", 9.0,
                Theme.Frozen(255, 181, 245, 244), FontWeights.Normal);
            content.Children.Add(CreateCalculationStrip(_schedulePreview, "节假日/调休按日历识别"));
            card.Child = content;
            return card;
        }

        private UIElement BuildWindowCard()
        {
            Border card = Theme.Panel(Theme.PanelBackground, Theme.PanelBorder, 13, 1);
            Grid grid = new Grid();
            grid.Margin = new Thickness(14, 12, 14, 12);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel labels = new StackPanel();
            TextBlock title = Theme.CnText("窗口置顶", 9.5, Theme.TextSoft, FontWeights.Normal);
            TextBlock hint = Theme.CnText("让薪资卡片始终浮在其他窗口上方", 8.5,
                Theme.TextMuted, FontWeights.Normal);
            hint.Margin = new Thickness(0, 3, 0, 0);
            labels.Children.Add(title);
            labels.Children.Add(hint);
            grid.Children.Add(labels);

            _topmostToggle = new ToggleSwitch();
            _topmostToggle.IsChecked = _original.AlwaysOnTop;
            _topmostToggle.VerticalAlignment = VerticalAlignment.Center;
            _topmostToggle.HorizontalAlignment = HorizontalAlignment.Right;
            AutomationProperties.SetName(_topmostToggle, "窗口置顶开关");
            grid.Children.Add(_topmostToggle);
            Grid.SetColumn(_topmostToggle, 1);

            card.Child = grid;
            return card;
        }

        private static UIElement CreateCalculationStrip(TextBlock left, string rightText)
        {
            Border strip = new Border();
            strip.Background = Theme.Frozen(20, 34, 211, 238);
            strip.BorderBrush = Theme.Divider;
            strip.BorderThickness = new Thickness(0, 1, 0, 0);
            strip.CornerRadius = new CornerRadius(0, 0, 13, 13);
            strip.Padding = new Thickness(14, 9, 14, 9);

            Grid row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(left);
            TextBlock right = Theme.CnText(rightText, 8.5, Theme.TextMuted, FontWeights.Normal);
            right.HorizontalAlignment = HorizontalAlignment.Right;
            row.Children.Add(right);
            Grid.SetColumn(right, 1);
            strip.Child = row;
            return strip;
        }

        private SettingsField CreateField(string key, string label, string value,
            string unit, int maxLength, bool isTime)
        {
            StackPanel root = new StackPanel();

            TextBlock labelText = Theme.CnText(label, 8.5, Theme.TextMuted, FontWeights.Normal);
            labelText.Margin = new Thickness(0, 0, 0, 5);
            root.Children.Add(labelText);

            Border host = Theme.Panel(Theme.InputBackground, Theme.InputBorder, 8, 1);
            host.Padding = new Thickness(10, 0, 10, 0);
            host.Height = 32;

            Grid inputGrid = new Grid();
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBox input = new TextBox();
            input.Text = value ?? string.Empty;
            input.MaxLength = maxLength;
            input.FontFamily = isTime ? Theme.MonoFont : Theme.MonoFont;
            input.FontSize = 12.5;
            input.Foreground = Theme.TextPrimary;
            input.CaretBrush = Theme.Accent;
            input.SelectionBrush = Theme.Frozen(90, 103, 232, 249);
            input.Background = Brushes.Transparent;
            input.BorderThickness = new Thickness(0);
            input.Padding = new Thickness(0);
            input.VerticalContentAlignment = VerticalAlignment.Center;
            input.HorizontalContentAlignment = HorizontalAlignment.Left;
            input.VerticalAlignment = VerticalAlignment.Center;
            input.SnapsToDevicePixels = true;
            inputGrid.Children.Add(input);
            AutomationProperties.SetName(input, label);

            TextBlock unitText = Theme.CnText(unit, 8.5, Theme.TextMuted, FontWeights.Normal);
            unitText.VerticalAlignment = VerticalAlignment.Center;
            unitText.Margin = new Thickness(8, 0, 0, 0);
            inputGrid.Children.Add(unitText);
            Grid.SetColumn(unitText, 1);

            host.Child = inputGrid;
            host.MouseLeftButtonDown += delegate { input.Focus(); };
            root.Children.Add(host);

            TextBlock error = Theme.CnText(string.Empty, 8.5, Theme.Danger, FontWeights.Normal);
            error.TextWrapping = TextWrapping.Wrap;
            error.Margin = new Thickness(0, 4, 0, 0);
            error.Visibility = Visibility.Collapsed;
            root.Children.Add(error);

            SettingsField field = new SettingsField(key, root, host, input, error);
            _fields.Add(field);

            input.GotKeyboardFocus += delegate
            {
                if (!field.HasError)
                {
                    host.BorderBrush = Theme.InputFocusBorder;
                }
            };
            input.LostKeyboardFocus += delegate
            {
                if (!field.HasError)
                {
                    host.BorderBrush = Theme.InputBorder;
                }
            };
            input.TextChanged += delegate
            {
                if (field.HasError)
                {
                    field.ClearError();
                }
                UpdatePreview();
            };

            return field;
        }

        private UIElement BuildFooter()
        {
            Border footer = new Border();
            footer.BorderBrush = Theme.Divider;
            footer.BorderThickness = new Thickness(0, 1, 0, 0);
            footer.Padding = new Thickness(0, 13, 0, 0);
            footer.Margin = new Thickness(0, 10, 0, 0);

            Grid row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextButton reset = new TextButton("恢复默认值", TextButtonKind.Link);
            reset.HorizontalAlignment = HorizontalAlignment.Left;
            reset.Click += delegate { ResetToDefaults(); };
            row.Children.Add(reset);

            StackPanel actions = new StackPanel();
            actions.Orientation = Orientation.Horizontal;
            actions.HorizontalAlignment = HorizontalAlignment.Right;

            TextButton cancel = new TextButton("取消", TextButtonKind.Ghost);
            cancel.Margin = new Thickness(0, 0, 8, 0);
            cancel.Click += delegate { Close(); };
            TextButton save = new TextButton("保存设置", TextButtonKind.Primary);
            save.Click += delegate { SaveSettings(); };

            actions.Children.Add(cancel);
            actions.Children.Add(save);
            row.Children.Add(actions);
            Grid.SetColumn(actions, 1);

            footer.Child = row;
            return footer;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Program.Log("SettingsWindow Loaded");
            Rect workArea = SystemParameters.WorkArea;
            Width = Math.Min(604, Math.Max(520, workArea.Width - 12));
            Height = Math.Min(700, Math.Max(430, workArea.Height - 12));
            if (Owner != null)
            {
                Left = Owner.Left + Math.Max(0, (Owner.Width - Width) / 2);
                Top = Owner.Top + Math.Max(0, (Owner.Height - Height) / 2);
                if (Top + Height > workArea.Bottom)
                {
                    Top = Math.Max(workArea.Top, workArea.Bottom - Height);
                }
                if (Left + Width > workArea.Right)
                {
                    Left = Math.Max(workArea.Left, workArea.Right - Width);
                }
            }
        }

        private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || Theme.IsInsideButton(e.OriginalSource))
            {
                return;
            }
            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
            }
        }

        private void SaveSettings()
        {
            Program.Log("SaveSettings begin, topmost=" + (_topmostToggle.IsChecked == true));
            _saveError.Visibility = Visibility.Collapsed;
            _saveError.Text = string.Empty;

            AppSettings candidate;
            if (!TryReadForm(out candidate))
            {
                Program.Log("SaveSettings validation failed");
                return;
            }

            Program.Log("SaveSettings validation ok");
            SettingsSaveResult result = _store.Save(candidate);
            Program.Log("SaveSettings store result=" + result.Success + " " + result.ErrorMessage);
            if (!result.Success)
            {
                _saveError.Text = result.ErrorMessage;
                _saveError.Visibility = Visibility.Visible;
                _saveError.BringIntoView();
                return;
            }

            _savedSettings = candidate;
            Program.Log("SaveSettings closing dialog");
            Close();
        }

        private bool TryReadForm(out AppSettings candidate)
        {
            foreach (SettingsField field in _fields)
            {
                field.ClearError();
            }

            bool parseOk = true;
            decimal salary = 0m;
            decimal days = 0m;
            int start = 0;
            int lunchStart = 0;
            int lunchEnd = 0;
            int end = 0;
            bool actual = _actualWorkdaysToggle != null && _actualWorkdaysToggle.IsChecked == true;

            if (!Formatters.TryParseNumber(_salaryField.Input.Text, out salary))
            {
                _salaryField.ShowError("请输入有效的月薪金额");
                parseOk = false;
            }

            if (!Formatters.TryParseNumber(_daysField.Input.Text, out days))
            {
                if (!actual)
                {
                    _daysField.ShowError("请输入有效的月计薪天数");
                    parseOk = false;
                }
                else
                {
                    days = _original.MonthlyWorkDays;
                }
            }
            if (actual && (days <= 0m || days > SettingsValidator.MaxMonthlyWorkDays))
            {
                days = _original.MonthlyWorkDays;
            }

            if (!Formatters.TryParseTime(_startField.Input.Text, out start))
            {
                _startField.ShowError("请输入有效时间，如 09:30");
                parseOk = false;
            }

            if (!Formatters.TryParseTime(_lunchStartField.Input.Text, out lunchStart))
            {
                _lunchStartField.ShowError("请输入有效时间，如 12:30");
                parseOk = false;
            }

            if (!Formatters.TryParseTime(_lunchEndField.Input.Text, out lunchEnd))
            {
                _lunchEndField.ShowError("请输入有效时间，如 14:00");
                parseOk = false;
            }

            if (!Formatters.TryParseTime(_endField.Input.Text, out end))
            {
                _endField.ShowError("请输入有效时间，如 19:00");
                parseOk = false;
            }

            candidate = new AppSettings();
            candidate.Version = AppSettings.CurrentVersion;
            candidate.MonthlySalary = salary;
            candidate.MonthlyWorkDays = days;
            candidate.WorkStartMinutes = start;
            candidate.LunchStartMinutes = lunchStart;
            candidate.LunchEndMinutes = lunchEnd;
            candidate.WorkEndMinutes = end;
            candidate.AlwaysOnTop = _topmostToggle.IsChecked == true;
            candidate.ActualWorkdaysEnabled = actual;

            if (parseOk)
            {
                SalaryMonitor.Core.ValidationResult validation = SettingsValidator.Validate(candidate);
                if (!validation.IsValid)
                {
                    parseOk = false;
                    foreach (ValidationIssue issue in validation.Issues)
                    {
                        SettingsField field = FindField(issue.Field);
                        if (field != null)
                        {
                            if (!field.HasError)
                            {
                                field.ShowError(issue.Message);
                            }
                        }
                        else
                        {
                            _saveError.Text = issue.Message;
                            _saveError.Visibility = Visibility.Visible;
                        }
                    }
                }
            }

            if (!parseOk)
            {
                for (int i = 0; i < _fields.Count; i++)
                {
                    if (_fields[i].HasError)
                    {
                        _fields[i].FocusInput();
                        break;
                    }
                }
                return false;
            }

            return true;
        }

        private SettingsField FindField(string key)
        {
            for (int i = 0; i < _fields.Count; i++)
            {
                if (string.Equals(_fields[i].Key, key, StringComparison.Ordinal))
                {
                    return _fields[i];
                }
            }
            return null;
        }

        private void ResetToDefaults()
        {
            AppSettings defaults = AppSettings.CreateDefault();
            _salaryField.Input.Text = Formatters.MoneyValue(defaults.MonthlySalary);
            _daysField.Input.Text = Formatters.DaysInput(defaults.MonthlyWorkDays);
            _startField.Input.Text = Formatters.ClockHHmm(defaults.WorkStartMinutes);
            _lunchStartField.Input.Text = Formatters.ClockHHmm(defaults.LunchStartMinutes);
            _lunchEndField.Input.Text = Formatters.ClockHHmm(defaults.LunchEndMinutes);
            _endField.Input.Text = Formatters.ClockHHmm(defaults.WorkEndMinutes);
            _topmostToggle.IsChecked = defaults.AlwaysOnTop;
            _actualWorkdaysToggle.IsChecked = defaults.ActualWorkdaysEnabled;
            UpdateActualMode();
            foreach (SettingsField field in _fields)
            {
                field.ClearError();
            }
            _saveError.Visibility = Visibility.Collapsed;
            _saveError.Text = string.Empty;
            UpdatePreview();
        }

        private void UpdateActualMode()
        {
            if (_actualWorkdaysToggle == null || _daysField == null || _daysColumn == null)
            {
                return;
            }

            bool actual = _actualWorkdaysToggle.IsChecked == true;
            _daysField.Root.Visibility = actual ? Visibility.Collapsed : Visibility.Visible;
            _daysColumn.Width = actual
                ? new GridLength(0)
                : new GridLength(1, GridUnitType.Star);
            if (_daysGapColumn != null)
            {
                _daysGapColumn.Width = actual ? new GridLength(0) : new GridLength(12);
            }

            if (_actualWorkdaysHint == null)
            {
                return;
            }

            DateTime now = AppClock.Now;
            if (actual)
            {
                int workdays = WorkdayCalendar.WorkdaysInMonth(now.Year, now.Month);
                string suffix = WorkdayCalendar.HasData(now.Year)
                    ? "（已按法定节假日与调休计算）"
                    : "（未收录该年节假日，按周一至周五）";
                _actualWorkdaysHint.Text = Formatters.MonthLabel(now) + " · " +
                    workdays.ToString(CultureInfo.InvariantCulture) + " 个工作日" + suffix;
            }
            else
            {
                _actualWorkdaysHint.Text = "关闭后按上方月计薪天数平均计算；开启后按当月实际工作日计算。";
            }
        }

        private void UpdatePreview()
        {
            if (_incomePreview == null || _schedulePreview == null)
            {
                return;
            }

            AppSettings preview;
            if (!TryBuildPreviewSettings(out preview))
            {
                _incomePreview.Text = "日薪 —　·　分钟薪 —";
                _schedulePreview.Text = "每日有效工时 —";
                return;
            }

            _incomePreview.Text = BuildIncomePreviewText();
            _schedulePreview.Text = BuildSchedulePreviewText();
        }

        private bool TryBuildPreviewSettings(out AppSettings preview)
        {
            preview = null;
            if (_salaryField == null || _daysField == null || _startField == null ||
                _lunchStartField == null || _lunchEndField == null || _endField == null)
            {
                return false;
            }

            decimal salary;
            decimal days;
            int start;
            int lunchStart;
            int lunchEnd;
            int end;
            bool actual = _actualWorkdaysToggle != null && _actualWorkdaysToggle.IsChecked == true;
            if (!Formatters.TryParseNumber(_salaryField.Input.Text, out salary) ||
                !Formatters.TryParseNumber(_daysField.Input.Text, out days) ||
                !Formatters.TryParseTime(_startField.Input.Text, out start) ||
                !Formatters.TryParseTime(_lunchStartField.Input.Text, out lunchStart) ||
                !Formatters.TryParseTime(_lunchEndField.Input.Text, out lunchEnd) ||
                !Formatters.TryParseTime(_endField.Input.Text, out end))
            {
                if (!actual ||
                    !Formatters.TryParseNumber(_salaryField.Input.Text, out salary) ||
                    !Formatters.TryParseTime(_startField.Input.Text, out start) ||
                    !Formatters.TryParseTime(_lunchStartField.Input.Text, out lunchStart) ||
                    !Formatters.TryParseTime(_lunchEndField.Input.Text, out lunchEnd) ||
                    !Formatters.TryParseTime(_endField.Input.Text, out end))
                {
                    return false;
                }
                days = _original.MonthlyWorkDays;
            }

            if (actual && (days <= 0m || days > SettingsValidator.MaxMonthlyWorkDays))
            {
                days = _original.MonthlyWorkDays;
            }

            AppSettings candidate = new AppSettings();
            candidate.Version = AppSettings.CurrentVersion;
            candidate.MonthlySalary = salary;
            candidate.MonthlyWorkDays = days;
            candidate.WorkStartMinutes = start;
            candidate.LunchStartMinutes = lunchStart;
            candidate.LunchEndMinutes = lunchEnd;
            candidate.WorkEndMinutes = end;
            candidate.AlwaysOnTop = _topmostToggle != null && _topmostToggle.IsChecked == true;
            candidate.ActualWorkdaysEnabled = actual;

            if (!SettingsValidator.Validate(candidate).IsValid)
            {
                return false;
            }

            preview = candidate;
            return true;
        }

        private string BuildIncomePreviewText()
        {
            AppSettings preview;
            if (!TryBuildPreviewSettings(out preview))
            {
                return "日薪 —　·　分钟薪 —";
            }

            PaySnapshot snapshot = PayCalculator.Calculate(preview, AppClock.Now);
            string prefix = preview.ActualWorkdaysEnabled
                ? Formatters.MonthLabel(AppClock.Now) + " " +
                    snapshot.MonthlyWorkdays.ToString(CultureInfo.InvariantCulture) + " 个工作日　·　"
                : string.Empty;
            return prefix + "日薪 " + Formatters.Money(snapshot.DailyWage) +
                "　·　分钟薪 " + Formatters.Money(snapshot.MinuteWage);
        }

        private string BuildSchedulePreviewText()
        {
            AppSettings preview;
            if (!TryBuildPreviewSettings(out preview))
            {
                return "每日有效工时 —";
            }
            return "每日有效工时 " + Formatters.WorkDuration(
                WorkSchedule.FromSettings(preview).TotalPaidMinutes);
        }
    }
}
