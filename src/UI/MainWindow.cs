using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using SalaryMonitor.Core;

namespace SalaryMonitor.UI
{
    internal sealed class MainWindow : Window
    {
        private readonly SettingsStore _store;
        private AppSettings _settings;
        private readonly DispatcherTimer _timer;
        private PaySnapshot _snapshot;
        private MonthSnapshot _monthSnapshot;
        private double _progressRatio;
        private double _monthProgressRatio;
        private bool _footerError;
        private bool _monthView;

        private Grid _todayPage;
        private Grid _monthPage;
        private SegmentButton _todayTab;
        private SegmentButton _monthTab;

        private TextBlock _amountText;
        private TextBlock _statusText;
        private Ellipse _statusDot;
        private Border _statusPill;
        private TextBlock _dateText;
        private TextBlock _clockText;
        private TextBlock _dailyText;
        private TextBlock _hourlyText;
        private TextBlock _minuteText;
        private TextBlock _countdownLabel;
        private TextBlock _offTimeText;
        private TextBlock _timerText;
        private TextBlock _remainingText;
        private TextBlock _progressPercentText;
        private TextBlock _progressMinutesText;
        private TextBlock _footerHint;
        private TextBlock _updateNoteText;
        private Border _progressTrack;
        private Border _progressFill;
        private IconButton _pinButton;
        private IconButton _eyeButton;

        private TextBlock _monthAmountText;
        private TextBlock _monthBasisTagText;
        private TextBlock _monthDailyText;
        private TextBlock _monthWorkdaysText;
        private TextBlock _monthElapsedText;
        private TextBlock _monthProgressPercentText;
        private TextBlock _monthProgressDaysText;
        private TextBlock _monthPaydayTitle;
        private TextBlock _monthPaydayDateText;
        private TextBlock _monthPaydayDaysText;
        private TextBlock _monthPaydayUnitText;
        private TextBlock _monthPaydayHintText;
        private TextBlock _monthPaydayNote;
        private Border _monthProgressTrack;
        private Border _monthProgressFill;

        private static readonly SolidColorBrush WorkBg = Theme.Frozen(32, 45, 212, 191);
        private static readonly SolidColorBrush WorkBorder = Theme.Frozen(55, 45, 212, 191);
        private static readonly SolidColorBrush WorkText = Theme.Frozen(255, 153, 246, 228);
        private static readonly SolidColorBrush LunchBg = Theme.Frozen(28, 251, 191, 36);
        private static readonly SolidColorBrush LunchBorder = Theme.Frozen(52, 251, 191, 36);
        private static readonly SolidColorBrush LunchText = Theme.Frozen(255, 253, 230, 138);
        private static readonly SolidColorBrush NeutralBg = Theme.Frozen(15, 148, 163, 184);
        private static readonly SolidColorBrush NeutralBorder = Theme.Frozen(30, 148, 163, 184);
        private static readonly SolidColorBrush NeutralText = Theme.Frozen(255, 203, 213, 225);

        public MainWindow(AppSettings settings, SettingsStore store)
        {
            _settings = settings == null ? AppSettings.CreateDefault() : settings;
            _store = store;

            Title = "薪资显示器";
            Width = 504;
            Height = 560;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = _settings.AlwaysOnTop;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            Icon = AppIcon.Load();
            Theme.SetFontSmoothing(this);

            Content = BuildRoot();
            Loaded += OnLoaded;
            Closed += OnClosed;

            _timer = new DispatcherTimer(DispatcherPriority.Background);
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerTick;

            UpdateUi();
        }

        private UIElement BuildRoot()
        {
            Grid root = new Grid();
            root.Background = Brushes.Transparent;

            Border card = Theme.Card(26);
            card.Margin = new Thickness(22);

            Grid content = new Grid();
            content.Margin = new Thickness(24, 20, 24, 18);
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            UIElement header = BuildHeader();
            content.Children.Add(header);
            Grid.SetRow(header, 0);

            UIElement status = BuildStatusRow();
            content.Children.Add(status);
            Grid.SetRow(status, 1);

            UIElement tabs = BuildViewTabs();
            content.Children.Add(tabs);
            Grid.SetRow(tabs, 2);

            Grid pageHost = new Grid();
            _todayPage = BuildTodayPage();
            _monthPage = BuildMonthPage();
            _monthPage.Visibility = Visibility.Collapsed;
            pageHost.Children.Add(_todayPage);
            pageHost.Children.Add(_monthPage);
            content.Children.Add(pageHost);
            Grid.SetRow(pageHost, 3);

            UIElement footer = BuildFooter();
            content.Children.Add(footer);
            Grid.SetRow(footer, 4);

            Theme.CardSurface(card, 26, content);
            root.Children.Add(card);
            return root;
        }

        private UIElement BuildHeader()
        {
            Grid header = new Grid();
            header.Background = Brushes.Transparent;
            header.MouseLeftButtonDown += OnHeaderMouseDown;
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel brand = new StackPanel();
            brand.Orientation = Orientation.Horizontal;
            brand.VerticalAlignment = VerticalAlignment.Center;
            brand.Children.Add(BuildBrandMark());
            StackPanel brandText = new StackPanel();
            brandText.Margin = new Thickness(10, 0, 0, 0);
            brandText.VerticalAlignment = VerticalAlignment.Center;
            TextBlock name = Theme.CnText("薪资显示器", 13, Theme.TextPrimary, FontWeights.SemiBold);
            TextBlock sub = Theme.UiText("SALARY MONITOR", 7.5, Theme.TextMuted);
            sub.FontWeight = FontWeights.SemiBold;
            sub.Margin = new Thickness(0, 2, 0, 0);
            brandText.Children.Add(name);
            brandText.Children.Add(sub);
            brand.Children.Add(brandText);
            header.Children.Add(brand);

            StackPanel actions = new StackPanel();
            actions.Orientation = Orientation.Horizontal;
            actions.HorizontalAlignment = HorizontalAlignment.Right;
            actions.VerticalAlignment = VerticalAlignment.Center;

            _pinButton = new IconButton(AppIcons.Pin, "保持置顶", false);
            _pinButton.Margin = new Thickness(0, 0, 4, 0);
            _pinButton.Click += OnPinClick;
            UpdatePinButton();

            IconButton settingsButton = new IconButton(AppIcons.Settings, "设置", false);
            settingsButton.Margin = new Thickness(0, 0, 4, 0);
            settingsButton.Click += OnSettingsClick;

            IconButton minimizeButton = new IconButton(AppIcons.Minimize, "最小化", false);
            minimizeButton.Margin = new Thickness(0, 0, 4, 0);
            minimizeButton.Click += OnMinimizeClick;

            IconButton closeButton = new IconButton(AppIcons.Close, "退出", true);
            closeButton.Click += OnCloseClick;

            actions.Children.Add(_pinButton);
            actions.Children.Add(settingsButton);
            actions.Children.Add(minimizeButton);
            actions.Children.Add(closeButton);
            header.Children.Add(actions);
            Grid.SetColumn(actions, 2);

            return header;
        }

        private static UIElement BuildBrandMark()
        {
            Border mark = new Border();
            mark.Width = 31;
            mark.Height = 31;
            mark.CornerRadius = new CornerRadius(10);
            mark.BorderBrush = Theme.Frozen(52, 165, 243, 252);
            mark.BorderThickness = new Thickness(1);
            mark.Background = Theme.BuildLinear(
                0.0, 0.0, 1.0, 1.0,
                0.0, Color.FromArgb(255, 23, 59, 76),
                1.0, Color.FromArgb(255, 25, 41, 74));

            Grid inner = new Grid();
            TextBlock yen = Theme.MonoText("¥", 15, Theme.Frozen(255, 236, 254, 255));
            yen.FontWeight = FontWeights.Bold;
            yen.HorizontalAlignment = HorizontalAlignment.Center;
            yen.VerticalAlignment = VerticalAlignment.Center;
            inner.Children.Add(yen);
            Ellipse dot = new Ellipse();
            dot.Width = 5;
            dot.Height = 5;
            dot.Fill = Theme.Accent;
            dot.HorizontalAlignment = HorizontalAlignment.Right;
            dot.VerticalAlignment = VerticalAlignment.Top;
            dot.Margin = new Thickness(0, 5, 5, 0);
            inner.Children.Add(dot);
            mark.Child = inner;
            return mark;
        }

        private UIElement BuildStatusRow()
        {
            Grid row = new Grid();
            row.Margin = new Thickness(0, 20, 0, 0);
            // The status pill keeps its natural width; the date/time yields
            // space on very narrow windows instead of squeezing the label.
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _statusPill = new Border();
            _statusPill.HorizontalAlignment = HorizontalAlignment.Left;
            _statusPill.VerticalAlignment = VerticalAlignment.Center;
            _statusPill.MinHeight = 24;
            _statusPill.CornerRadius = new CornerRadius(12);
            _statusPill.BorderThickness = new Thickness(1);
            _statusPill.Padding = new Thickness(10, 4, 12, 4);
            _statusPill.UseLayoutRounding = true;

            StackPanel statusInner = new StackPanel();
            statusInner.Orientation = Orientation.Horizontal;
            statusInner.VerticalAlignment = VerticalAlignment.Center;

            _statusDot = new Ellipse();
            _statusDot.Width = 5;
            _statusDot.Height = 5;
            _statusDot.Margin = new Thickness(0, 0, 7, 0);
            _statusDot.VerticalAlignment = VerticalAlignment.Center;

            _statusText = Theme.CnText("未上班", 10.0, Theme.TextSoft, FontWeights.Normal);
            _statusText.VerticalAlignment = VerticalAlignment.Center;
            _statusText.Padding = new Thickness(0, 0, 1, 0);
            statusInner.Children.Add(_statusDot);
            statusInner.Children.Add(_statusText);
            _statusPill.Child = statusInner;
            row.Children.Add(_statusPill);

            StackPanel dateStack = new StackPanel();
            dateStack.Orientation = Orientation.Horizontal;
            dateStack.VerticalAlignment = VerticalAlignment.Center;
            dateStack.HorizontalAlignment = HorizontalAlignment.Right;
            _dateText = Theme.CnText("", 10, Theme.TextMuted, FontWeights.Normal);
            _clockText = Theme.MonoText("", 10.5, Theme.TextSoft);
            _clockText.Margin = new Thickness(8, 0, 0, 0);
            dateStack.Children.Add(_dateText);
            dateStack.Children.Add(_clockText);
            row.Children.Add(dateStack);
            Grid.SetColumn(dateStack, 1);

            return row;
        }

        private UIElement BuildViewTabs()
        {
            Grid row = new Grid();
            row.Margin = new Thickness(0, 12, 0, 0);
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Border switchBox = Theme.Panel(
                Theme.Frozen(11, 255, 255, 255),
                Theme.Frozen(17, 255, 255, 255), 10, 1);
            switchBox.Padding = new Thickness(2);
            switchBox.HorizontalAlignment = HorizontalAlignment.Left;
            switchBox.VerticalAlignment = VerticalAlignment.Center;

            StackPanel tabs = new StackPanel();
            tabs.Orientation = Orientation.Horizontal;
            _todayTab = new SegmentButton("今日");
            _todayTab.Margin = new Thickness(0, 0, 2, 0);
            System.Windows.Automation.AutomationProperties.SetName(_todayTab, "今日视图");
            _todayTab.Click += OnTodayTabClick;
            _monthTab = new SegmentButton("本月");
            System.Windows.Automation.AutomationProperties.SetName(_monthTab, "本月视图");
            _monthTab.Click += OnMonthTabClick;
            tabs.Children.Add(_todayTab);
            tabs.Children.Add(_monthTab);
            switchBox.Child = tabs;
            row.Children.Add(switchBox);

            _eyeButton = new IconButton(
                _settings.AmountsHidden ? AppIcons.EyeOff : AppIcons.Eye,
                _settings.AmountsHidden ? "显示金额" : "隐藏金额", false);
            _eyeButton.HorizontalAlignment = HorizontalAlignment.Right;
            _eyeButton.VerticalAlignment = VerticalAlignment.Center;
            _eyeButton.Click += OnEyeClick;
            row.Children.Add(_eyeButton);
            Grid.SetColumn(_eyeButton, 2);

            return row;
        }

        private Grid BuildTodayPage()
        {
            Grid page = new Grid();
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            UIElement hero = BuildHeroLabel();
            page.Children.Add(hero);
            Grid.SetRow(hero, 0);

            UIElement amount = BuildAmount();
            page.Children.Add(amount);
            Grid.SetRow(amount, 1);

            UIElement rates = BuildRateStrip();
            page.Children.Add(rates);
            Grid.SetRow(rates, 2);

            UIElement countdown = BuildCountdownPanel();
            page.Children.Add(countdown);
            Grid.SetRow(countdown, 3);

            UIElement progress = BuildProgress();
            page.Children.Add(progress);
            Grid.SetRow(progress, 4);

            return page;
        }

        private Grid BuildMonthPage()
        {
            Grid page = new Grid();
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            page.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            UIElement hero = BuildMonthHeroLabel();
            page.Children.Add(hero);
            Grid.SetRow(hero, 0);

            UIElement amount = BuildMonthAmount();
            page.Children.Add(amount);
            Grid.SetRow(amount, 1);

            UIElement progress = BuildMonthProgress();
            page.Children.Add(progress);
            Grid.SetRow(progress, 2);

            UIElement payday = BuildMonthPaydayPanel();
            page.Children.Add(payday);
            Grid.SetRow(payday, 3);

            UIElement metrics = BuildMonthMetrics();
            page.Children.Add(metrics);
            Grid.SetRow(metrics, 4);

            UIElement note = BuildMonthPaydayNote();
            page.Children.Add(note);
            Grid.SetRow(note, 5);

            return page;
        }

        private UIElement BuildMonthHeroLabel()
        {
            Grid row = new Grid();
            row.Margin = new Thickness(0, 15, 0, 0);
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel labels = new StackPanel();
            labels.Orientation = Orientation.Horizontal;
            labels.VerticalAlignment = VerticalAlignment.Center;
            TextBlock label = Theme.CnText("本月已赚", 11, Theme.TextSecondary, FontWeights.Normal);

            Border tag = new Border();
            tag.Background = Theme.Frozen(20, 129, 140, 248);
            tag.BorderBrush = Theme.Frozen(35, 129, 140, 248);
            tag.BorderThickness = new Thickness(1);
            tag.CornerRadius = new CornerRadius(8);
            tag.Padding = new Thickness(7, 2, 7, 2);
            tag.Margin = new Thickness(8, 0, 0, 0);
            tag.VerticalAlignment = VerticalAlignment.Center;
            _monthBasisTagText = Theme.CnText("按日薪累计", 8.5,
                Theme.Frozen(255, 199, 210, 254), FontWeights.Normal);
            tag.Child = _monthBasisTagText;

            labels.Children.Add(label);
            labels.Children.Add(tag);
            row.Children.Add(labels);
            return row;
        }

        private UIElement BuildMonthAmount()
        {
            Grid row = new Grid();
            row.Margin = new Thickness(0, 2, 0, 0);
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock currency = Theme.UiText("¥", 22, Theme.Frozen(255, 185, 245, 248));
            currency.VerticalAlignment = VerticalAlignment.Bottom;
            currency.Margin = new Thickness(0, 0, 6, 9);
            row.Children.Add(currency);

            _monthAmountText = Theme.AnimatedDigits(Theme.Text(
                "0.00", 50, Theme.AmountForeground, Theme.NumberFont, FontWeights.Medium));
            _monthAmountText.VerticalAlignment = VerticalAlignment.Bottom;
            _monthAmountText.HorizontalAlignment = HorizontalAlignment.Left;
            row.Children.Add(_monthAmountText);
            Grid.SetColumn(_monthAmountText, 1);

            return row;
        }

        private UIElement BuildMonthProgress()
        {
            StackPanel root = new StackPanel();
            root.Margin = new Thickness(0, 18, 0, 0);

            Grid labels = new Grid();
            labels.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            labels.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel left = new StackPanel();
            left.Orientation = Orientation.Horizontal;
            TextBlock prefix = Theme.CnText("本月工作日进度", 9, Theme.TextMuted, FontWeights.Normal);
            _monthProgressPercentText = Theme.CnText("0%", 9, Theme.TextSoft, FontWeights.Normal);
            _monthProgressPercentText.Margin = new Thickness(4, 0, 0, 0);
            left.Children.Add(prefix);
            left.Children.Add(_monthProgressPercentText);
            labels.Children.Add(left);

            _monthProgressDaysText = Theme.CnText("0 / 0 天", 9, Theme.TextSoft, FontWeights.Normal);
            _monthProgressDaysText.HorizontalAlignment = HorizontalAlignment.Right;
            labels.Children.Add(_monthProgressDaysText);
            Grid.SetColumn(_monthProgressDaysText, 1);
            root.Children.Add(labels);

            _monthProgressTrack = new Border();
            _monthProgressTrack.Height = 6;
            _monthProgressTrack.CornerRadius = new CornerRadius(3);
            _monthProgressTrack.Background = Theme.Frozen(22, 255, 255, 255);
            _monthProgressTrack.Margin = new Thickness(0, 7, 0, 0);

            Grid trackGrid = new Grid();
            _monthProgressFill = new Border();
            _monthProgressFill.Height = 6;
            _monthProgressFill.CornerRadius = new CornerRadius(3);
            _monthProgressFill.HorizontalAlignment = HorizontalAlignment.Left;
            _monthProgressFill.Background = Theme.ProgressFill;
            _monthProgressFill.Width = 0;
            trackGrid.Children.Add(_monthProgressFill);
            _monthProgressTrack.Child = trackGrid;
            _monthProgressTrack.SizeChanged += delegate { UpdateMonthProgressBar(); };
            root.Children.Add(_monthProgressTrack);

            return root;
        }

        private UIElement BuildMonthPaydayPanel()
        {
            Border panel = Theme.Panel(Theme.CountdownBackground,
                Theme.Frozen(25, 103, 232, 249), 15, 1);
            panel.Margin = new Thickness(0, 12, 0, 0);
            panel.Padding = new Thickness(14, 9, 14, 9);
            panel.Height = 86;

            Grid content = new Grid();
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titleRow = new StackPanel();
            titleRow.Orientation = Orientation.Horizontal;
            titleRow.VerticalAlignment = VerticalAlignment.Center;
            Path clock = Theme.Icon(AppIcons.Clock, Theme.Accent, 12);
            clock.Margin = new Thickness(0, 0, 6, 0);
            titleRow.Children.Add(clock);
            _monthPaydayTitle = Theme.CnText("距下次发薪", 10.5, Theme.TextSoft, FontWeights.Normal);
            titleRow.Children.Add(_monthPaydayTitle);
            header.Children.Add(titleRow);

            _monthPaydayDateText = Theme.CnText("", 9, Theme.TextMuted, FontWeights.Normal);
            _monthPaydayDateText.HorizontalAlignment = HorizontalAlignment.Right;
            _monthPaydayDateText.VerticalAlignment = VerticalAlignment.Center;
            header.Children.Add(_monthPaydayDateText);
            Grid.SetColumn(_monthPaydayDateText, 1);
            content.Children.Add(header);

            Grid body = new Grid();
            body.Margin = new Thickness(0, 1, 0, 0);
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel count = new StackPanel();
            count.Orientation = Orientation.Horizontal;
            count.VerticalAlignment = VerticalAlignment.Bottom;
            _monthPaydayDaysText = Theme.AnimatedDigits(Theme.Text(
                "—", 34, Theme.White, Theme.MonoFont, FontWeights.Normal));
            _monthPaydayDaysText.VerticalAlignment = VerticalAlignment.Bottom;
            _monthPaydayUnitText = Theme.CnText("天", 11, Theme.TextSoft, FontWeights.Normal);
            _monthPaydayUnitText.Margin = new Thickness(5, 0, 0, 7);
            _monthPaydayUnitText.VerticalAlignment = VerticalAlignment.Bottom;
            count.Children.Add(_monthPaydayDaysText);
            count.Children.Add(_monthPaydayUnitText);
            body.Children.Add(count);

            _monthPaydayHintText = Theme.CnText("按自然日估算", 8.5, Theme.TextMuted,
                FontWeights.Normal);
            _monthPaydayHintText.HorizontalAlignment = HorizontalAlignment.Right;
            _monthPaydayHintText.VerticalAlignment = VerticalAlignment.Bottom;
            _monthPaydayHintText.Margin = new Thickness(10, 0, 0, 7);
            body.Children.Add(_monthPaydayHintText);
            Grid.SetColumn(_monthPaydayHintText, 1);

            content.Children.Add(body);
            Grid.SetRow(body, 1);
            panel.Child = content;
            return panel;
        }

        private UIElement BuildMonthMetrics()
        {
            Border strip = Theme.Panel(Theme.PanelBackground, Theme.PanelBorder, 13, 1);
            strip.Height = 58;
            strip.Margin = new Thickness(0, 12, 0, 0);

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _monthDailyText = AddMonthMetricCell(grid, 0, "本月日薪");
            _monthWorkdaysText = AddMonthMetricCell(grid, 2, "本月工作日");
            _monthElapsedText = AddMonthMetricCell(grid, 4, "本月已过");

            AddVerticalSeparator(grid, 1);
            AddVerticalSeparator(grid, 3);
            strip.Child = grid;
            return strip;
        }

        private static TextBlock AddMonthMetricCell(Grid grid, int column, string label)
        {
            StackPanel cell = new StackPanel();
            cell.Margin = new Thickness(14, 8, 14, 8);
            cell.VerticalAlignment = VerticalAlignment.Center;
            TextBlock caption = Theme.CnText(label, 9.0, Theme.TextMuted, FontWeights.Normal);
            TextBlock value = Theme.MonoText("—", 13.0, Theme.TextSoft);
            value.Margin = new Thickness(0, 2, 0, 0);
            cell.Children.Add(caption);
            cell.Children.Add(value);
            grid.Children.Add(cell);
            Grid.SetColumn(cell, column);
            return value;
        }

        private UIElement BuildMonthPaydayNote()
        {
            _monthPaydayNote = Theme.CnText("按当前设置估算 · 不含请假/加班", 8.5,
                Theme.TextMuted, FontWeights.Normal);
            _monthPaydayNote.Margin = new Thickness(0, 8, 0, 0);
            _monthPaydayNote.TextWrapping = TextWrapping.Wrap;
            return _monthPaydayNote;
        }

        private UIElement BuildHeroLabel()
        {
            StackPanel labels = new StackPanel();
            labels.Margin = new Thickness(0, 15, 0, 0);
            labels.Orientation = Orientation.Horizontal;
            labels.VerticalAlignment = VerticalAlignment.Center;
            TextBlock label = Theme.CnText("今日已赚", 11, Theme.TextSecondary, FontWeights.Normal);

            Border tag = new Border();
            tag.Background = Theme.Frozen(20, 129, 140, 248);
            tag.BorderBrush = Theme.Frozen(35, 129, 140, 248);
            tag.BorderThickness = new Thickness(1);
            tag.CornerRadius = new CornerRadius(8);
            tag.Padding = new Thickness(7, 2, 7, 2);
            tag.Margin = new Thickness(8, 0, 0, 0);
            tag.VerticalAlignment = VerticalAlignment.Center;
            TextBlock tagText = Theme.CnText("按分钟累计", 8.5,
                Theme.Frozen(255, 199, 210, 254), FontWeights.Normal);
            tag.Child = tagText;

            labels.Children.Add(label);
            labels.Children.Add(tag);
            return labels;
        }

        private UIElement BuildAmount()
        {
            Grid row = new Grid();
            row.Margin = new Thickness(0, 2, 0, 0);
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock currency = Theme.UiText("¥", 22, Theme.Frozen(255, 185, 245, 248));
            currency.VerticalAlignment = VerticalAlignment.Bottom;
            currency.Margin = new Thickness(0, 0, 6, 9);
            row.Children.Add(currency);

            _amountText = Theme.AnimatedDigits(Theme.Text(
                "0.00", 50, Theme.AmountForeground, Theme.NumberFont, FontWeights.Medium));
            _amountText.VerticalAlignment = VerticalAlignment.Bottom;
            _amountText.HorizontalAlignment = HorizontalAlignment.Left;
            row.Children.Add(_amountText);
            Grid.SetColumn(_amountText, 1);

            return row;
        }

        private UIElement BuildRateStrip()
        {
            Border strip = Theme.Panel(Theme.PanelBackground, Theme.PanelBorder, 13, 1);
            strip.Height = 50;
            strip.Margin = new Thickness(0, 14, 0, 0);

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _dailyText = AddRateCell(grid, 0, "日薪");
            _hourlyText = AddRateCell(grid, 2, "时薪");
            _minuteText = AddRateCell(grid, 4, "分钟薪");

            AddVerticalSeparator(grid, 1);
            AddVerticalSeparator(grid, 3);
            strip.Child = grid;
            return strip;
        }

        private static TextBlock AddRateCell(Grid grid, int column, string label)
        {
            StackPanel cell = new StackPanel();
            cell.Margin = new Thickness(14, 8, 14, 8);
            cell.VerticalAlignment = VerticalAlignment.Center;
            TextBlock caption = Theme.CnText(label, 9.0, Theme.TextMuted, FontWeights.Normal);
            TextBlock value = Theme.MonoText("—", 13.0, Theme.TextSoft);
            value.Margin = new Thickness(0, 2, 0, 0);
            cell.Children.Add(caption);
            cell.Children.Add(value);
            grid.Children.Add(cell);
            Grid.SetColumn(cell, column);
            return value;
        }

        private static void AddVerticalSeparator(Grid grid, int column)
        {
            Border line = new Border();
            line.Width = 1;
            line.Background = Theme.Divider;
            line.Margin = new Thickness(0, 10, 0, 10);
            grid.Children.Add(line);
            Grid.SetColumn(line, column);
        }

        private UIElement BuildCountdownPanel()
        {
            Border panel = Theme.Panel(Theme.CountdownBackground,
                Theme.Frozen(25, 103, 232, 249), 15, 1);
            panel.Margin = new Thickness(0, 14, 0, 0);
            panel.Padding = new Thickness(14, 11, 14, 10);

            Grid content = new Grid();
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel labelRow = new StackPanel();
            labelRow.Orientation = Orientation.Horizontal;
            labelRow.VerticalAlignment = VerticalAlignment.Center;
            Path clockIcon = Theme.Icon(AppIcons.Clock, Theme.Accent, 12);
            clockIcon.Margin = new Thickness(0, 0, 6, 0);
            labelRow.Children.Add(clockIcon);
            _countdownLabel = Theme.CnText("距离下班", 10.5, Theme.TextSoft, FontWeights.Normal);
            labelRow.Children.Add(_countdownLabel);
            header.Children.Add(labelRow);

            _offTimeText = Theme.CnText("下班时间 19:00", 9, Theme.TextMuted, FontWeights.Normal);
            _offTimeText.HorizontalAlignment = HorizontalAlignment.Right;
            _offTimeText.VerticalAlignment = VerticalAlignment.Center;
            header.Children.Add(_offTimeText);
            Grid.SetColumn(_offTimeText, 1);
            content.Children.Add(header);

            Grid timerRow = new Grid();
            timerRow.Margin = new Thickness(0, 3, 0, 0);
            timerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            timerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _timerText = Theme.AnimatedDigits(Theme.MonoText("00:00:00", 34, Theme.White));
            _timerText.VerticalAlignment = VerticalAlignment.Bottom;
            timerRow.Children.Add(_timerText);

            StackPanel remainingStack = new StackPanel();
            remainingStack.HorizontalAlignment = HorizontalAlignment.Right;
            remainingStack.VerticalAlignment = VerticalAlignment.Bottom;
            remainingStack.Margin = new Thickness(12, 0, 0, 3);
            TextBlock remainingLabel = Theme.CnText("剩余有效工时", 9, Theme.TextMuted, FontWeights.Normal);
            remainingLabel.HorizontalAlignment = HorizontalAlignment.Right;
            _remainingText = Theme.CnText("—", 12, Theme.Frozen(255, 201, 247, 247), FontWeights.Normal);
            _remainingText.HorizontalAlignment = HorizontalAlignment.Right;
            _remainingText.Margin = new Thickness(0, 2, 0, 0);
            remainingStack.Children.Add(remainingLabel);
            remainingStack.Children.Add(_remainingText);
            timerRow.Children.Add(remainingStack);
            Grid.SetColumn(remainingStack, 1);

            content.Children.Add(timerRow);
            Grid.SetRow(timerRow, 1);
            panel.Child = content;
            return panel;
        }

        private UIElement BuildProgress()
        {
            StackPanel root = new StackPanel();
            root.Margin = new Thickness(0, 12, 0, 0);

            Grid labels = new Grid();
            labels.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            labels.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel left = new StackPanel();
            left.Orientation = Orientation.Horizontal;
            TextBlock prefix = Theme.CnText("今日进度", 9, Theme.TextMuted, FontWeights.Normal);
            _progressPercentText = Theme.CnText("0%", 9, Theme.TextSoft, FontWeights.Normal);
            _progressPercentText.Margin = new Thickness(4, 0, 0, 0);
            left.Children.Add(prefix);
            left.Children.Add(_progressPercentText);
            labels.Children.Add(left);

            _progressMinutesText = Theme.CnText("0 / 480 分钟", 9, Theme.TextSoft, FontWeights.Normal);
            _progressMinutesText.HorizontalAlignment = HorizontalAlignment.Right;
            labels.Children.Add(_progressMinutesText);
            Grid.SetColumn(_progressMinutesText, 1);
            root.Children.Add(labels);

            _progressTrack = new Border();
            _progressTrack.Height = 6;
            _progressTrack.CornerRadius = new CornerRadius(3);
            _progressTrack.Background = Theme.Frozen(22, 255, 255, 255);
            _progressTrack.Margin = new Thickness(0, 7, 0, 0);

            Grid trackGrid = new Grid();
            _progressFill = new Border();
            _progressFill.Height = 6;
            _progressFill.CornerRadius = new CornerRadius(3);
            _progressFill.HorizontalAlignment = HorizontalAlignment.Left;
            _progressFill.Background = Theme.ProgressFill;
            _progressFill.Width = 0;
            trackGrid.Children.Add(_progressFill);
            _progressTrack.Child = trackGrid;
            _progressTrack.SizeChanged += delegate { UpdateProgressBar(); };
            root.Children.Add(_progressTrack);

            return root;
        }

        private UIElement BuildFooter()
        {
            Grid row = new Grid();
            row.Margin = new Thickness(0, 8, 0, 0);
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _footerHint = Theme.CnText("按设置估算 · 以实际薪资为准", 8.5, Theme.TextMuted,
                FontWeights.Normal);
            _footerHint.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(_footerHint);

            StackPanel update = new StackPanel();
            update.Orientation = Orientation.Horizontal;
            update.VerticalAlignment = VerticalAlignment.Center;
            Ellipse dot = new Ellipse();
            dot.Width = 4;
            dot.Height = 4;
            dot.Fill = Theme.Teal;
            dot.Margin = new Thickness(0, 0, 6, 0);
            dot.VerticalAlignment = VerticalAlignment.Center;
            _updateNoteText = Theme.CnText("每分钟更新工资", 8.5, Theme.TextMuted,
                FontWeights.Normal);
            update.Children.Add(dot);
            update.Children.Add(_updateNoteText);
            row.Children.Add(update);
            Grid.SetColumn(update, 1);

            return row;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Rect workArea = SystemParameters.WorkArea;
            Width = Math.Min(504, Math.Max(460, workArea.Width - 8));
            Height = Math.Min(560, Math.Max(480, workArea.Height - 8));
            Left = workArea.Left + Math.Max(0, (workArea.Width - Width) / 2);
            Top = workArea.Top + Math.Max(0, (workArea.Height - Height) / 2);

            UpdateUi();
            _timer.Start();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _timer.Stop();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            UpdateUi();
        }

        private void UpdateUi()
        {
            DateTime now = AppClock.Now;
            _snapshot = PayCalculator.Calculate(_settings, now);
            _monthSnapshot = MonthCalculator.Calculate(_settings, now);

            if (_settings.AmountsHidden)
            {
                _amountText.Text = Formatters.HiddenAmount;
                _amountText.FontSize = 50;
                _dailyText.Text = Formatters.HiddenMoney;
                _hourlyText.Text = Formatters.HiddenMoney;
                _minuteText.Text = Formatters.HiddenMoney;
            }
            else
            {
                string amount = Formatters.MoneyValue(_snapshot.TodayEarned);
                _amountText.Text = amount;
                if (amount.Length > 12)
                {
                    _amountText.FontSize = 32;
                }
                else if (amount.Length > 9)
                {
                    _amountText.FontSize = 42;
                }
                else
                {
                    _amountText.FontSize = 50;
                }
                _dailyText.Text = Formatters.Money(_snapshot.DailyWage);
                _hourlyText.Text = Formatters.Money(_snapshot.HourlyWage);
                _minuteText.Text = Formatters.Money(_snapshot.MinuteWage);
            }

            _dateText.Text = Formatters.DateHeader(now);
            _clockText.Text = Formatters.TimeOfDay(now);

            ApplyPhase();

            if (_snapshot.Phase == DayPhase.RestDay)
            {
                _progressRatio = 0.0;
                _progressPercentText.Text = "—";
                _progressMinutesText.Text = RestPayNote();
            }
            else
            {
                _progressRatio = _snapshot.Progress;
                _progressPercentText.Text = Formatters.Progress(_snapshot.Progress);
                _progressMinutesText.Text = _snapshot.CompletedPaidMinutes.ToString(CultureInfo.InvariantCulture) +
                    " / " + _snapshot.TotalPaidMinutes.ToString(CultureInfo.InvariantCulture) + " 分钟";
            }
            UpdateProgressBar();
            UpdateMonthUi();
            UpdateFooterHint();
        }

        private void UpdateMonthUi()
        {
            if (_monthSnapshot == null || _monthAmountText == null)
            {
                return;
            }

            if (_settings.AmountsHidden)
            {
                _monthAmountText.Text = Formatters.HiddenAmount;
                _monthAmountText.FontSize = 50;
                _monthDailyText.Text = Formatters.HiddenMoney;
            }
            else
            {
                string amount = Formatters.MoneyValue(_monthSnapshot.MonthEarned);
                _monthAmountText.Text = amount;
                if (amount.Length > 12)
                {
                    _monthAmountText.FontSize = 32;
                }
                else if (amount.Length > 9)
                {
                    _monthAmountText.FontSize = 42;
                }
                else
                {
                    _monthAmountText.FontSize = 50;
                }
                _monthDailyText.Text = Formatters.Money(_monthSnapshot.DailyWage);
            }

            _monthBasisTagText.Text = _monthSnapshot.ActualWorkdays
                ? "按实际工作日"
                : "按日薪累计";

            _monthWorkdaysText.Text = _monthSnapshot.TotalWorkdays.ToString(
                CultureInfo.InvariantCulture) + " 天";
            _monthElapsedText.Text = Formatters.WorkdaysValue(
                _monthSnapshot.CompletedWorkdays) + " 天";
            _monthProgressDaysText.Text = Formatters.WorkdaysValue(
                _monthSnapshot.CompletedWorkdays) + " / " +
                _monthSnapshot.TotalWorkdays.ToString(CultureInfo.InvariantCulture) + " 天";
            _monthProgressPercentText.Text = _monthSnapshot.TotalWorkdays > 0
                ? Formatters.Progress(_monthSnapshot.Progress)
                : "—";
            _monthProgressRatio = _monthSnapshot.Progress;
            UpdateMonthProgressBar();

            if (!_monthSnapshot.PaydayConfigured)
            {
                _monthPaydayTitle.Text = "发薪日未设置";
                _monthPaydayDateText.Text = "";
                _monthPaydayDaysText.Text = "—";
                _monthPaydayDaysText.FontFamily = Theme.ChineseFont;
                _monthPaydayDaysText.FontSize = 24;
                _monthPaydayDaysText.FontWeight = FontWeights.Normal;
                _monthPaydayUnitText.Text = "";
                _monthPaydayHintText.Text = "可在设置中填写";
            }
            else if (_monthSnapshot.IsPaydayToday)
            {
                _monthPaydayTitle.Text = "今天发薪";
                _monthPaydayDateText.Text = Formatters.MonthDay(
                    _monthSnapshot.NextPayday.Value);
                _monthPaydayDaysText.Text = "今天";
                _monthPaydayDaysText.FontFamily = Theme.ChineseFont;
                _monthPaydayDaysText.FontSize = 26;
                _monthPaydayDaysText.FontWeight = FontWeights.SemiBold;
                _monthPaydayUnitText.Text = "";
                _monthPaydayHintText.Text = "按自然日估算";
            }
            else
            {
                _monthPaydayTitle.Text = "距下次发薪";
                _monthPaydayDateText.Text = Formatters.MonthDay(
                    _monthSnapshot.NextPayday.Value);
                _monthPaydayDaysText.Text = _monthSnapshot.DaysUntilPayday.ToString(
                    CultureInfo.InvariantCulture);
                _monthPaydayDaysText.FontFamily = Theme.MonoFont;
                _monthPaydayDaysText.FontSize = 34;
                _monthPaydayDaysText.FontWeight = FontWeights.Normal;
                _monthPaydayUnitText.Text = "天";
                _monthPaydayHintText.Text = "按自然日估算";
            }
        }

        private void UpdateMonthProgressBar()
        {
            if (_monthProgressTrack == null || _monthProgressFill == null)
            {
                return;
            }

            double width = _monthProgressTrack.ActualWidth > 0
                ? _monthProgressTrack.ActualWidth * _monthProgressRatio
                : 0.0;
            if (width < 0.0)
            {
                width = 0.0;
            }
            if (width > _monthProgressTrack.ActualWidth)
            {
                width = _monthProgressTrack.ActualWidth;
            }
            _monthProgressFill.Width = width;
        }

        private void ApplyPhase()
        {
            SetStatusPalette(NeutralBg, NeutralBorder, NeutralText);
            _timerText.FontFamily = Theme.MonoFont;
            _timerText.FontSize = 34;
            _timerText.FontWeight = FontWeights.Normal;
            _timerText.Foreground = Theme.White;

            switch (_snapshot.Phase)
            {
                case DayPhase.RestDay:
                    _statusText.Text = RestStatus();
                    SetStatusPalette(NeutralBg, NeutralBorder, NeutralText);
                    _countdownLabel.Text = "今日状态";
                    _offTimeText.Text = RestPayNote();
                    _timerText.FontFamily = Theme.ChineseFont;
                    _timerText.FontSize = 29;
                    _timerText.FontWeight = FontWeights.SemiBold;
                    _timerText.Foreground = Theme.TextSecondary;
                    _timerText.Text = "休息日";
                    _remainingText.Text = "今日不计薪";
                    _updateNoteText.Text = RestPayNote();
                    return;

                case DayPhase.BeforeWork:
                    _statusText.Text = "未上班";
                    SetStatusPalette(NeutralBg, NeutralBorder, NeutralText);
                    _countdownLabel.Text = "距上班";
                    _offTimeText.Text = "上班时间 " + Formatters.ClockHHmm(_settings.WorkStartMinutes);
                    _timerText.Text = Formatters.Countdown(_snapshot.MainCountdown);
                    _remainingText.Text = Formatters.WorkDuration(_snapshot.RemainingPaidMinutes);
                    _updateNoteText.Text = "每满 1 分钟更新工资";
                    break;

                case DayPhase.MorningWork:
                    _statusText.Text = "上午工作中";
                    SetStatusPalette(WorkBg, WorkBorder, WorkText);
                    _countdownLabel.Text = "距离下班";
                    _offTimeText.Text = "下班时间 " + Formatters.ClockHHmm(_settings.WorkEndMinutes);
                    _timerText.Text = Formatters.Countdown(_snapshot.MainCountdown);
                    _remainingText.Text = Formatters.WorkDuration(_snapshot.RemainingPaidMinutes);
                    _updateNoteText.Text = "每分钟更新工资";
                    break;

                case DayPhase.LunchBreak:
                    _statusText.Text = "午休中";
                    SetStatusPalette(LunchBg, LunchBorder, LunchText);
                    _countdownLabel.Text = "距离下班";
                    _offTimeText.Text = "午休至 " + Formatters.ClockHHmm(_settings.LunchEndMinutes);
                    _timerText.Text = Formatters.Countdown(_snapshot.MainCountdown);
                    _remainingText.Text = Formatters.WorkDuration(_snapshot.RemainingPaidMinutes);
                    _updateNoteText.Text = "午休中 · 金额暂停";
                    break;

                case DayPhase.AfternoonWork:
                    _statusText.Text = "下午工作中";
                    SetStatusPalette(WorkBg, WorkBorder, WorkText);
                    _countdownLabel.Text = "距离下班";
                    _offTimeText.Text = "下班时间 " + Formatters.ClockHHmm(_settings.WorkEndMinutes);
                    _timerText.Text = Formatters.Countdown(_snapshot.MainCountdown);
                    _remainingText.Text = Formatters.WorkDuration(_snapshot.RemainingPaidMinutes);
                    _updateNoteText.Text = "每分钟更新工资";
                    break;

                case DayPhase.AfterWork:
                    _statusText.Text = "今日已下班";
                    SetStatusPalette(NeutralBg, NeutralBorder, NeutralText);
                    _countdownLabel.Text = "距离下班";
                    _offTimeText.Text = "下班时间 " + Formatters.ClockHHmm(_settings.WorkEndMinutes);
                    _timerText.Text = "00:00:00";
                    _remainingText.Text = "0 分钟";
                    _updateNoteText.Text = "今日工资已封顶";
                    break;
            }
        }

        private string RestStatus()
        {
            return _snapshot.DayKind == DayKind.Holiday ? "节假日休息" : "周末休息";
        }

        private string RestPayNote()
        {
            return _snapshot.DayKind == DayKind.Holiday ? "节假日不计薪" : "周末不计薪";
        }

        private void UpdateFooterHint()
        {
            if (_footerError || _footerHint == null || _snapshot == null)
            {
                return;
            }

            if (_monthView && _monthSnapshot != null)
            {
                if (_monthSnapshot.ActualWorkdays)
                {
                    if (!_monthSnapshot.CalendarDataAvailable)
                    {
                        _footerHint.Text = "未收录 " + _monthSnapshot.Year.ToString(
                            CultureInfo.InvariantCulture) + " 年节假日 · 按工作日估算";
                    }
                    else
                    {
                        _footerHint.Text = "按本月 " + _monthSnapshot.TotalWorkdays.ToString(
                            CultureInfo.InvariantCulture) + " 个工作日估算";
                    }
                }
                else
                {
                    _footerHint.Text = "日薪累计估算 · 月末可能不等于月薪";
                }
                _footerHint.Foreground = Theme.TextMuted;
                _updateNoteText.Text = "本月实时累计";
                return;
            }

            string basis;
            if (_snapshot.ActualWorkdays)
            {
                if (!_snapshot.CalendarDataAvailable)
                {
                    basis = "未收录 " + _snapshot.Now.Year.ToString(CultureInfo.InvariantCulture) + " 年节假日";
                }
                else if (_snapshot.DayKind == DayKind.MakeupWorkday)
                {
                    basis = "今日调休上班 · 本月 " +
                        _snapshot.MonthlyWorkdays.ToString(CultureInfo.InvariantCulture) + " 个工作日";
                }
                else
                {
                    basis = "按本月 " +
                        _snapshot.MonthlyWorkdays.ToString(CultureInfo.InvariantCulture) + " 个工作日";
                }
            }
            else
            {
                basis = "按 " + Formatters.DaysInput(_settings.MonthlyWorkDays) + " 天平均";
            }

            _footerHint.Text = basis + "估算 · 以实际薪资为准";
            _footerHint.Foreground = Theme.TextMuted;
        }

        private void SetStatusPalette(Brush background, Brush border, Brush foreground)
        {
            _statusPill.Background = background;
            _statusPill.BorderBrush = border;
            _statusDot.Fill = foreground;
            _statusText.Foreground = foreground;
        }

        private void UpdateProgressBar()
        {
            if (_progressTrack == null || _progressFill == null)
            {
                return;
            }

            double width = _progressTrack.ActualWidth > 0
                ? _progressTrack.ActualWidth * _progressRatio
                : 0.0;
            if (width < 0.0)
            {
                width = 0.0;
            }
            if (width > _progressTrack.ActualWidth)
            {
                width = _progressTrack.ActualWidth;
            }
            _progressFill.Width = width;
        }

        private void OnTodayTabClick(object sender, RoutedEventArgs e)
        {
            SetMonthView(false);
        }

        private void OnMonthTabClick(object sender, RoutedEventArgs e)
        {
            SetMonthView(true);
        }

        private void SetMonthView(bool month)
        {
            _monthView = month;
            if (_todayPage != null)
            {
                _todayPage.Visibility = month ? Visibility.Collapsed : Visibility.Visible;
            }
            if (_monthPage != null)
            {
                _monthPage.Visibility = month ? Visibility.Visible : Visibility.Collapsed;
            }
            if (_todayTab != null)
            {
                _todayTab.SetActive(!month);
            }
            if (_monthTab != null)
            {
                _monthTab.SetActive(month);
            }
            UpdateUi();
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

        private void OnPinClick(object sender, RoutedEventArgs e)
        {
            _settings = _settings.Clone();
            _settings.AlwaysOnTop = !_settings.AlwaysOnTop;
            Topmost = _settings.AlwaysOnTop;
            UpdatePinButton();

            SettingsSaveResult save = _store.Save(_settings);
            if (!save.Success)
            {
                _footerError = true;
                _footerHint.Text = "置顶状态未能保存";
                _footerHint.Foreground = Theme.Danger;
            }
            else
            {
                _footerError = false;
                UpdateFooterHint();
            }
            UpdateUi();
        }

        private void OnEyeClick(object sender, RoutedEventArgs e)
        {
            _settings = _settings.Clone();
            _settings.AmountsHidden = !_settings.AmountsHidden;
            UpdateUi();
            UpdateEyeButton();

            SettingsSaveResult save = _store.Save(_settings);
            if (!save.Success)
            {
                _footerError = true;
                _footerHint.Text = "金额隐藏状态未能保存";
                _footerHint.Foreground = Theme.Danger;
            }
            else
            {
                _footerError = false;
                UpdateFooterHint();
            }
        }

        private void UpdateEyeButton()
        {
            if (_eyeButton == null)
            {
                return;
            }
            bool hidden = _settings.AmountsHidden;
            _eyeButton.SetIcon(hidden ? AppIcons.EyeOff : AppIcons.Eye);
            _eyeButton.SetTooltip(hidden ? "显示金额" : "隐藏金额");
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            Program.Log("OnSettingsClick begin");
            SettingsWindow settingsWindow = new SettingsWindow(_settings, _store);
            Program.Log("SettingsWindow constructed");
            settingsWindow.Owner = this;
            settingsWindow.Topmost = this.Topmost;
            settingsWindow.ShowDialog();
            Program.Log("SettingsWindow dialog returned");

            if (settingsWindow.SavedSettings == null)
            {
                return;
            }

            _settings = settingsWindow.SavedSettings;
            Topmost = _settings.AlwaysOnTop;
            UpdatePinButton();
            UpdateEyeButton();
            _footerError = false;
            UpdateUi();
        }

        private void UpdatePinButton()
        {
            _pinButton.SetActive(_settings.AlwaysOnTop);
            _pinButton.SetTooltip(_settings.AlwaysOnTop ? "取消置顶" : "保持置顶");
        }

        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

#if UI_TEST_BUILD
        internal void SetAmountsHiddenForTest(bool hidden)
        {
            _settings = _settings.Clone();
            _settings.AmountsHidden = hidden;
            UpdateUi();
            UpdateEyeButton();
        }

        internal void SetMonthViewForTest(bool month)
        {
            SetMonthView(month);
        }

        internal void SetPaydayForTest(int payday)
        {
            _settings = _settings.Clone();
            _settings.Payday = payday;
            UpdateUi();
        }
#endif
    }
}
