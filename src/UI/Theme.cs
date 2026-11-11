using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace SalaryMonitor.UI
{
    internal static class Theme
    {
        public static readonly FontFamily ChineseFont = new FontFamily("Microsoft YaHei UI");
        public static readonly FontFamily UiFont = new FontFamily("Segoe UI");
        public static readonly FontFamily MonoFont = new FontFamily("Consolas");
        public static readonly FontFamily NumberFont = new FontFamily("Segoe UI");

        public static readonly Color TextPrimaryColor = Hex("#F8FAFC");
        public static readonly Color TextSecondaryColor = Hex("#94A3B8");
        public static readonly Color TextMutedColor = Hex("#64748B");
        public static readonly Color AccentColor = Hex("#67E8F9");
        public static readonly Color TealColor = Hex("#2DD4BF");
        public static readonly Color IndigoColor = Hex("#818CF8");
        public static readonly Color AmberColor = Hex("#FBBF24");
        public static readonly Color DangerColor = Hex("#FCA5A5");

        public static readonly SolidColorBrush TextPrimary = Frozen(255, 248, 250, 252);
        public static readonly SolidColorBrush TextSecondary = Frozen(255, 148, 163, 184);
        public static readonly SolidColorBrush TextMuted = Frozen(255, 100, 116, 139);
        public static readonly SolidColorBrush TextSoft = Frozen(255, 168, 183, 201);
        public static readonly SolidColorBrush Accent = Frozen(AccentColor);
        public static readonly SolidColorBrush AccentBright = Frozen(255, 165, 243, 252);
        public static readonly SolidColorBrush Teal = Frozen(TealColor);
        public static readonly SolidColorBrush Amber = Frozen(AmberColor);
        public static readonly SolidColorBrush Danger = Frozen(DangerColor);
        public static readonly SolidColorBrush IconNormal = Frozen(255, 124, 142, 165);
        public static readonly SolidColorBrush IconDanger = Frozen(255, 196, 142, 150);
        public static readonly SolidColorBrush Transparent = Frozen(0, 0, 0, 0);
        public static readonly SolidColorBrush White = Frozen(255, 255, 255, 255);

        public static readonly SolidColorBrush CardBorder = Frozen(28, 255, 255, 255);
        public static readonly SolidColorBrush PanelBackground = Frozen(11, 255, 255, 255);
        public static readonly SolidColorBrush PanelBorder = Frozen(17, 255, 255, 255);
        public static readonly SolidColorBrush Divider = Frozen(18, 255, 255, 255);
        public static readonly SolidColorBrush IconHover = Frozen(18, 255, 255, 255);
        public static readonly SolidColorBrush IconActiveBackground = Frozen(26, 34, 211, 238);
        public static readonly SolidColorBrush IconActiveBorder = Frozen(48, 103, 232, 249);
        public static readonly SolidColorBrush GhostBackground = Frozen(7, 255, 255, 255);
        public static readonly SolidColorBrush GhostBorder = Frozen(18, 255, 255, 255);
        public static readonly SolidColorBrush InputBackground = Frozen(135, 3, 8, 17);
        public static readonly SolidColorBrush InputBorder = Frozen(22, 255, 255, 255);
        public static readonly SolidColorBrush InputFocusBorder = Frozen(110, 103, 232, 249);
        public static readonly SolidColorBrush ErrorBackground = Frozen(180, 52, 24, 32);
        public static readonly SolidColorBrush ErrorBorder = Frozen(70, 244, 63, 94);
        public static readonly SolidColorBrush ToggleOff = Frozen(255, 30, 47, 66);
        public static readonly SolidColorBrush ToggleOn = Frozen(255, 13, 148, 136);
        public static readonly SolidColorBrush ToggleBorder = Frozen(255, 55, 78, 104);
        public static readonly SolidColorBrush TooltipBackground = Frozen(255, 12, 22, 36);
        public static readonly SolidColorBrush TooltipBorder = Frozen(255, 49, 65, 84);

        public static readonly LinearGradientBrush CardBackground = BuildLinear(
            0.0, 0.0, 1.0, 1.0,
            0.0, Color.FromArgb(255, 16, 28, 47),
            0.5, Color.FromArgb(255, 11, 20, 34),
            1.0, Color.FromArgb(255, 10, 24, 34));

        public static readonly LinearGradientBrush AmountForeground = BuildLinear(
            0.0, 0.0, 1.0, 0.0,
            0.0, Color.FromArgb(255, 255, 255, 255),
            0.45, Color.FromArgb(255, 240, 253, 255),
            1.0, Color.FromArgb(255, 165, 243, 252));

        public static readonly LinearGradientBrush ProgressFill = BuildLinear(
            0.0, 0.0, 1.0, 0.0,
            0.0, Hex("#22D3EE"),
            0.52, Hex("#2DD4BF"),
            1.0, Hex("#818CF8"));

        public static readonly LinearGradientBrush PrimaryButton = BuildLinear(
            0.0, 0.0, 1.0, 0.0,
            0.0, Color.FromArgb(255, 83, 224, 243),
            1.0, Color.FromArgb(255, 45, 212, 191));

        public static readonly LinearGradientBrush TopAccentLine = BuildLinear(
            0.0, 0.0, 1.0, 0.0,
            0.0, Color.FromArgb(0, 103, 232, 249),
            0.28, Color.FromArgb(105, 103, 232, 249),
            0.68, Color.FromArgb(72, 129, 140, 248),
            1.0, Color.FromArgb(0, 129, 140, 248));

        public static readonly LinearGradientBrush CountdownBackground = BuildLinear(
            0.0, 0.0, 1.0, 0.0,
            0.0, Color.FromArgb(255, 16, 37, 48),
            1.0, Color.FromArgb(255, 11, 26, 37));

        public static readonly DropShadowEffect CardShadow = BuildShadow();

        public static SolidColorBrush Frozen(byte alpha, byte r, byte g, byte b)
        {
            SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(alpha, r, g, b));
            brush.Freeze();
            return brush;
        }

        public static SolidColorBrush Frozen(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public static Color Hex(string text)
        {
            return (Color)ColorConverter.ConvertFromString(text);
        }

        public static LinearGradientBrush BuildLinear(double x1, double y1, double x2, double y2,
            params object[] stops)
        {
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(x1, y1);
            brush.EndPoint = new Point(x2, y2);
            for (int i = 0; i + 1 < stops.Length; i += 2)
            {
                double offset = Convert.ToDouble(stops[i]);
                Color color = (Color)stops[i + 1];
                brush.GradientStops.Add(new GradientStop(color, offset));
            }
            brush.Freeze();
            return brush;
        }

        public static RadialGradientBrush BuildRadial(Point center, double radius, Color inner, Color outer)
        {
            RadialGradientBrush brush = new RadialGradientBrush();
            brush.Center = center;
            brush.GradientOrigin = center;
            brush.RadiusX = radius;
            brush.RadiusY = radius;
            brush.GradientStops.Add(new GradientStop(inner, 0.0));
            brush.GradientStops.Add(new GradientStop(outer, 1.0));
            brush.Freeze();
            return brush;
        }

        private static DropShadowEffect BuildShadow()
        {
            DropShadowEffect effect = new DropShadowEffect();
            effect.Color = Colors.Black;
            effect.BlurRadius = 32;
            effect.ShadowDepth = 10;
            effect.Direction = 270;
            effect.Opacity = 0.60;
            effect.Freeze();
            return effect;
        }

        public static TextBlock Text(string value, double fontSize, Brush foreground,
            FontFamily family, FontWeight weight)
        {
            TextBlock text = new TextBlock();
            text.Text = value;
            text.FontSize = fontSize;
            text.Foreground = foreground;
            text.FontFamily = family;
            text.FontWeight = weight;
            text.TextWrapping = TextWrapping.NoWrap;
            text.VerticalAlignment = VerticalAlignment.Center;
            TextOptions.SetTextFormattingMode(text, TextFormattingMode.Ideal);
            TextOptions.SetTextRenderingMode(text, TextRenderingMode.Grayscale);
            return text;
        }

        public static TextBlock UiText(string value, double fontSize, Brush foreground)
        {
            return Text(value, fontSize, foreground, UiFont, FontWeights.Normal);
        }

        public static TextBlock CnText(string value, double fontSize, Brush foreground, FontWeight weight)
        {
            return Text(value, fontSize, foreground, ChineseFont, weight);
        }

        public static TextBlock MonoText(string value, double fontSize, Brush foreground)
        {
            return Text(value, fontSize, foreground, MonoFont, FontWeights.Normal);
        }

        public static Border Panel(Brush background, Brush border, double radius, double thickness)
        {
            Border borderControl = new Border();
            borderControl.Background = background;
            borderControl.BorderBrush = border;
            borderControl.BorderThickness = new Thickness(thickness);
            borderControl.CornerRadius = new CornerRadius(radius);
            borderControl.SnapsToDevicePixels = true;
            return borderControl;
        }

        public static Border Card(double radius)
        {
            Border card = new Border();
            card.CornerRadius = new CornerRadius(radius);
            card.Background = CardBackground;
            card.BorderBrush = CardBorder;
            card.BorderThickness = new Thickness(1);
            card.Effect = (DropShadowEffect)CardShadow.Clone();
            card.SnapsToDevicePixels = true;
            card.UseLayoutRounding = true;
            return card;
        }

        public static Grid CardSurface(Border card, double radius, UIElement content)
        {
            Grid surface = new Grid();
            surface.ClipToBounds = true;

            Border topLeftGlow = new Border();
            topLeftGlow.CornerRadius = new CornerRadius(radius);
            topLeftGlow.Background = BuildRadial(new Point(0.04, 0.02), 0.9,
                Color.FromArgb(43, 34, 211, 238), Color.FromArgb(0, 34, 211, 238));
            topLeftGlow.IsHitTestVisible = false;
            surface.Children.Add(topLeftGlow);

            Border bottomGlow = new Border();
            bottomGlow.CornerRadius = new CornerRadius(radius);
            bottomGlow.Background = BuildRadial(new Point(0.95, 0.96), 0.9,
                Color.FromArgb(34, 99, 102, 241), Color.FromArgb(0, 99, 102, 241));
            bottomGlow.IsHitTestVisible = false;
            surface.Children.Add(bottomGlow);

            Border accent = new Border();
            accent.Height = 1;
            accent.Margin = new Thickness(30, 1, 30, 0);
            accent.VerticalAlignment = VerticalAlignment.Top;
            accent.Background = TopAccentLine;
            accent.IsHitTestVisible = false;
            surface.Children.Add(accent);

            if (content != null)
            {
                surface.Children.Add(content);
            }

            card.Child = surface;
            return surface;
        }

        public static Path Icon(Geometry data, Brush stroke, double size)
        {
            Path path = new Path();
            path.Data = data;
            path.Stroke = stroke;
            path.StrokeThickness = 1.6;
            path.StrokeStartLineCap = PenLineCap.Round;
            path.StrokeEndLineCap = PenLineCap.Round;
            path.StrokeLineJoin = PenLineJoin.Round;
            path.Fill = Transparent;
            path.Width = size;
            path.Height = size;
            path.Stretch = Stretch.Uniform;
            path.SnapsToDevicePixels = true;
            return path;
        }

        public static ToolTip Tooltip(string text)
        {
            ToolTip tip = new ToolTip();
            tip.Content = text;
            tip.Background = TooltipBackground;
            tip.Foreground = TextSoft;
            tip.BorderBrush = TooltipBorder;
            tip.BorderThickness = new Thickness(1);
            tip.Padding = new Thickness(9, 6, 9, 6);
            tip.FontFamily = ChineseFont;
            tip.FontSize = 10;
            tip.HasDropShadow = true;
            return tip;
        }

        public static void SetFontSmoothing(UIElement element)
        {
            TextOptions.SetTextFormattingMode(element, TextFormattingMode.Ideal);
            TextOptions.SetTextRenderingMode(element, TextRenderingMode.Grayscale);
        }

        public static bool IsInsideButton(object source)
        {
            DependencyObject current = source as DependencyObject;
            while (current != null)
            {
                if (current is Button)
                {
                    return true;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }
    }
}
