#if UI_TEST_BUILD
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SalaryMonitor.Core;

namespace SalaryMonitor.UI
{
    /// <summary>
    /// Test-build-only utility that renders deterministic screenshots without
    /// relying on the capture process's DPI awareness or desktop composition.
    /// </summary>
    internal static class UiTestCapture
    {
        public static void Schedule(MainWindow main, SettingsStore store, AppSettings settings)
        {
            string directory = Environment.GetEnvironmentVariable("SALARY_MONITOR_SCREENSHOT_DIR");
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(directory);
            }
            catch
            {
                return;
            }

            main.Dispatcher.BeginInvoke(
                new Action(delegate { CaptureAll(main, store, settings, directory); }),
                DispatcherPriority.ApplicationIdle,
                null);
        }

        private static void CaptureAll(MainWindow main, SettingsStore store,
            AppSettings settings, string directory)
        {
            try
            {
                Program.Log("UiTestCapture main begin");
                CaptureElement((FrameworkElement)main.Content,
                    Path.Combine(directory, "main.png"));

                main.SetAmountsHiddenForTest(true);
                CaptureElement((FrameworkElement)main.Content,
                    Path.Combine(directory, "main-hidden.png"));
                main.SetAmountsHiddenForTest(false);

                SettingsWindow dialog = new SettingsWindow(settings, store);
                dialog.Owner = main;
                dialog.Topmost = main.Topmost;
                dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                dialog.Show();
                dialog.Dispatcher.BeginInvoke(
                    new Action(delegate
                    {
                        try
                        {
                            Program.Log("UiTestCapture settings begin");
                            CaptureElement((FrameworkElement)dialog.Content,
                                Path.Combine(directory, "settings.png"));
                            Program.Log("UiTestCapture complete");
                        }
                        finally
                        {
                            dialog.Close();
                            main.Close();
                            Application.Current.Shutdown();
                        }
                    }),
                    DispatcherPriority.ApplicationIdle,
                    null);
            }
            catch (Exception ex)
            {
                Program.Log("UiTestCapture failed: " + ex);
                main.Close();
                Application.Current.Shutdown();
            }
        }

        private static void CaptureElement(FrameworkElement element, string path)
        {
            if (element == null)
            {
                return;
            }

            element.UpdateLayout();
            double width = element.ActualWidth;
            double height = element.ActualHeight;
            if (width <= 0.0 || height <= 0.0)
            {
                return;
            }

            const double scale = 2.0;
            Rect bounds = new Rect(0, 0, width, height);
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                LinearGradientBrush backdrop = new LinearGradientBrush(
                    Color.FromRgb(6, 12, 22), Color.FromRgb(5, 10, 19),
                    new Point(0, 0), new Point(1, 1));
                dc.DrawRectangle(backdrop, null, bounds);

                VisualBrush content = new VisualBrush(element);
                content.ViewboxUnits = BrushMappingMode.Absolute;
                content.Viewbox = bounds;
                content.Stretch = Stretch.Fill;
                dc.DrawRectangle(content, null, bounds);
            }

            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                (int)Math.Ceiling(width * scale),
                (int)Math.Ceiling(height * scale),
                96.0 * scale,
                96.0 * scale,
                PixelFormats.Pbgra32);
            bitmap.Render(visual);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (FileStream stream = new FileStream(path, FileMode.Create,
                FileAccess.Write, FileShare.None))
            {
                encoder.Save(stream);
            }
        }
    }
}
#endif
