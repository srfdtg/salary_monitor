using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;

namespace SalaryMonitor.UI
{
    internal static class AppIcons
    {
        public static readonly Geometry Pin = Freeze(BuildPin());
        public static readonly Geometry Settings = Freeze(BuildSettings());
        public static readonly Geometry Minimize = Freeze(new LineGeometry(
            new Point(5, 12), new Point(19, 12)));
        public static readonly Geometry Close = Freeze(BuildClose());
        public static readonly Geometry Clock = Freeze(BuildClock());

        private static Geometry BuildPin()
        {
            GeometryGroup group = new GeometryGroup();
            PathGeometry body = new PathGeometry();
            PathFigure figure = new PathFigure();
            figure.StartPoint = new Point(9, 3);
            figure.IsClosed = true;
            figure.Segments.Add(new LineSegment(new Point(15, 3), true));
            figure.Segments.Add(new LineSegment(new Point(15, 9), true));
            figure.Segments.Add(new LineSegment(new Point(18, 12), true));
            figure.Segments.Add(new LineSegment(new Point(18, 14), true));
            figure.Segments.Add(new LineSegment(new Point(6, 14), true));
            figure.Segments.Add(new LineSegment(new Point(6, 12), true));
            figure.Segments.Add(new LineSegment(new Point(9, 9), true));
            body.Figures.Add(figure);
            group.Children.Add(body);
            group.Children.Add(new LineGeometry(new Point(12, 14), new Point(12, 21)));
            return group;
        }

        private static Geometry BuildSettings()
        {
            GeometryGroup group = new GeometryGroup();
            // Two slider rows, matching the compact controls in the concept.
            group.Children.Add(new LineGeometry(new Point(3, 8), new Point(11.5, 8)));
            group.Children.Add(new LineGeometry(new Point(16.5, 8), new Point(21, 8)));
            group.Children.Add(new EllipseGeometry(new Point(14, 8), 2.4, 2.4));
            group.Children.Add(new LineGeometry(new Point(3, 16), new Point(6.5, 16)));
            group.Children.Add(new LineGeometry(new Point(11.5, 16), new Point(21, 16)));
            group.Children.Add(new EllipseGeometry(new Point(9, 16), 2.4, 2.4));
            return group;
        }

        private static Geometry BuildClose()
        {
            GeometryGroup group = new GeometryGroup();
            group.Children.Add(new LineGeometry(new Point(6, 6), new Point(18, 18)));
            group.Children.Add(new LineGeometry(new Point(18, 6), new Point(6, 18)));
            return group;
        }

        private static Geometry BuildClock()
        {
            GeometryGroup group = new GeometryGroup();
            group.Children.Add(new EllipseGeometry(new Point(12, 12), 8.4, 8.4));
            group.Children.Add(new LineGeometry(new Point(12, 7.2), new Point(12, 12)));
            group.Children.Add(new LineGeometry(new Point(12, 12), new Point(15.2, 14)));
            return group;
        }

        private static Geometry Freeze(Geometry geometry)
        {
            geometry.Freeze();
            return geometry;
        }
    }

    internal sealed class IconButton : Button
    {
        private readonly Path _icon;
        private readonly bool _danger;
        private bool _active;

        public IconButton(Geometry icon, string tooltip, bool danger)
        {
            _danger = danger;
            Width = 28;
            Height = 28;
            Padding = new Thickness(0);
            Background = Theme.Transparent;
            BorderBrush = Theme.Transparent;
            BorderThickness = new Thickness(1);
            Cursor = Cursors.Hand;
            Focusable = false;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            ToolTip = Theme.Tooltip(tooltip);
            AutomationProperties.SetName(this, tooltip);

            _icon = Theme.Icon(icon, danger ? Theme.IconDanger : Theme.IconNormal, 14);
            Content = _icon;
            Template = BuildIconTemplate();
            SetActive(false);
        }

        public void SetActive(bool active)
        {
            _active = active;
            Background = active ? Theme.IconActiveBackground : Theme.Transparent;
            BorderBrush = active ? Theme.IconActiveBorder : Theme.Transparent;
            _icon.Stroke = active ? Theme.AccentBright : (_danger ? Theme.IconDanger : Theme.IconNormal);
        }

        public void SetTooltip(string text)
        {
            ToolTip = Theme.Tooltip(text);
            AutomationProperties.SetName(this, text);
            AutomationProperties.SetHelpText(this, text);
        }

        private static ControlTemplate BuildIconTemplate()
        {
            FrameworkElementFactory chrome = new FrameworkElementFactory(typeof(Border), "chrome");
            chrome.SetValue(Border.BackgroundProperty,
                new TemplateBindingExtension(Control.BackgroundProperty));
            chrome.SetValue(Border.BorderBrushProperty,
                new TemplateBindingExtension(Control.BorderBrushProperty));
            chrome.SetValue(Border.BorderThicknessProperty,
                new TemplateBindingExtension(Control.BorderThicknessProperty));
            chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            chrome.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            chrome.AppendChild(presenter);

            ControlTemplate template = new ControlTemplate(typeof(Button));
            template.VisualTree = chrome;

            Trigger hover = new Trigger();
            hover.Property = UIElement.IsMouseOverProperty;
            hover.Value = true;
            Setter hoverSetter = new Setter(Border.BackgroundProperty, Theme.IconHover);
            hoverSetter.TargetName = "chrome";
            hover.Setters.Add(hoverSetter);
            template.Triggers.Add(hover);

            Trigger pressed = new Trigger();
            pressed.Property = ButtonBase.IsPressedProperty;
            pressed.Value = true;
            pressed.Setters.Add(new Setter(UIElement.OpacityProperty, 0.70));
            template.Triggers.Add(pressed);

            return template;
        }
    }

    internal enum TextButtonKind
    {
        Primary,
        Ghost,
        Link
    }

    internal sealed class TextButton : Button
    {
        public TextButton(string text, TextButtonKind kind)
        {
            Cursor = Cursors.Hand;
            Focusable = true;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            Template = BuildTextButtonTemplate();
            AutomationProperties.SetName(this, text);

            FontFamily = Theme.ChineseFont;
            FontSize = 9.5;
            Height = kind == TextButtonKind.Link ? 30 : 34;
            Padding = kind == TextButtonKind.Link ? new Thickness(0) : new Thickness(16, 0, 16, 0);

            TextBlock label = Theme.CnText(text, 9.5, Theme.TextPrimary, FontWeights.Normal);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;

            if (kind == TextButtonKind.Primary)
            {
                Background = Theme.PrimaryButton;
                BorderBrush = Theme.Frozen(Color.FromArgb(70, 165, 243, 252));
                Foreground = Theme.Frozen(255, 4, 42, 49);
                label.Foreground = Foreground;
                label.FontWeight = FontWeights.SemiBold;
                MinWidth = 88;
                BorderThickness = new Thickness(1);
            }
            else if (kind == TextButtonKind.Ghost)
            {
                Background = Theme.GhostBackground;
                BorderBrush = Theme.GhostBorder;
                Foreground = Theme.TextSecondary;
                label.Foreground = Foreground;
                BorderThickness = new Thickness(1);
                MinWidth = 70;
            }
            else
            {
                Background = Theme.Transparent;
                BorderBrush = Theme.Transparent;
                Foreground = Theme.TextMuted;
                label.Foreground = Foreground;
                BorderThickness = new Thickness(0);
                HorizontalContentAlignment = HorizontalAlignment.Left;
            }

            Content = label;
        }

        private static ControlTemplate BuildTextButtonTemplate()
        {
            FrameworkElementFactory chrome = new FrameworkElementFactory(typeof(Border), "chrome");
            chrome.SetValue(Border.BackgroundProperty,
                new TemplateBindingExtension(Control.BackgroundProperty));
            chrome.SetValue(Border.BorderBrushProperty,
                new TemplateBindingExtension(Control.BorderBrushProperty));
            chrome.SetValue(Border.BorderThicknessProperty,
                new TemplateBindingExtension(Control.BorderThicknessProperty));
            chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
            chrome.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty,
                new TemplateBindingExtension(Control.HorizontalContentAlignmentProperty));
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            chrome.AppendChild(presenter);

            ControlTemplate template = new ControlTemplate(typeof(Button));
            template.VisualTree = chrome;

            Trigger hover = new Trigger();
            hover.Property = UIElement.IsMouseOverProperty;
            hover.Value = true;
            hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.84));
            template.Triggers.Add(hover);

            Trigger pressed = new Trigger();
            pressed.Property = ButtonBase.IsPressedProperty;
            pressed.Value = true;
            pressed.Setters.Add(new Setter(UIElement.OpacityProperty, 0.70));
            template.Triggers.Add(pressed);

            Trigger disabled = new Trigger();
            disabled.Property = UIElement.IsEnabledProperty;
            disabled.Value = false;
            disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45));
            template.Triggers.Add(disabled);

            return template;
        }
    }

    internal sealed class ToggleSwitch : CheckBox
    {
        public ToggleSwitch()
        {
            Width = 38;
            Height = 20;
            Cursor = Cursors.Hand;
            Focusable = false;
            Template = BuildToggleTemplate();
        }

        private static ControlTemplate BuildToggleTemplate()
        {
            FrameworkElementFactory root = new FrameworkElementFactory(typeof(Grid));
            root.SetValue(FrameworkElement.WidthProperty, 38.0);
            root.SetValue(FrameworkElement.HeightProperty, 20.0);

            FrameworkElementFactory track = new FrameworkElementFactory(typeof(Border), "track");
            track.SetValue(Border.BackgroundProperty, Theme.ToggleOff);
            track.SetValue(Border.BorderBrushProperty, Theme.ToggleBorder);
            track.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            track.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            root.AppendChild(track);

            FrameworkElementFactory knob = new FrameworkElementFactory(typeof(Ellipse), "knob");
            knob.SetValue(FrameworkElement.WidthProperty, 14.0);
            knob.SetValue(FrameworkElement.HeightProperty, 14.0);
            knob.SetValue(FrameworkElement.MarginProperty, new Thickness(2));
            knob.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            knob.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            knob.SetValue(Shape.FillProperty, Theme.White);
            root.AppendChild(knob);

            ControlTemplate template = new ControlTemplate(typeof(CheckBox));
            template.VisualTree = root;

            Trigger on = new Trigger();
            on.Property = ToggleButton.IsCheckedProperty;
            on.Value = true;
            Setter trackOn = new Setter(Border.BackgroundProperty, Theme.ToggleOn);
            trackOn.TargetName = "track";
            on.Setters.Add(trackOn);
            Setter knobOn = new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            knobOn.TargetName = "knob";
            on.Setters.Add(knobOn);
            template.Triggers.Add(on);

            Trigger hover = new Trigger();
            hover.Property = UIElement.IsMouseOverProperty;
            hover.Value = true;
            Setter borderHover = new Setter(Border.BorderBrushProperty, Theme.Accent);
            borderHover.TargetName = "track";
            hover.Setters.Add(borderHover);
            template.Triggers.Add(hover);

            return template;
        }
    }
}
