using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using SalaryMonitor.Core;
using SalaryMonitor.UI;

namespace SalaryMonitor
{
    internal static class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                Log("Program.Main start");
                SalaryApplication app = new SalaryApplication();
                app.Run();
                Log("Application.Run returned");
            }
            catch (Exception ex)
            {
                Log("Startup exception: " + ex);
                try
                {
                    Console.Error.WriteLine(ex.ToString());
                }
                catch
                {
                }
                MessageBox.Show(
                    "薪资显示器启动失败：\n" + ex.Message,
                    "薪资显示器",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        internal static void Log(string message)
        {
#if UI_TEST_BUILD
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ui-trace.log");
                File.AppendAllText(path,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + message + Environment.NewLine);
            }
            catch
            {
            }
#endif
        }
    }

    internal sealed class SalaryApplication : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Program.Log("App.OnStartup begin");
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            SettingsStore store = new SettingsStore(SettingsStore.GetDefaultSettingsPath());
            SettingsLoadResult loaded = store.Load();
            Program.Log("Settings loaded from " + store.SettingsPath + ", defaults=" + loaded.UsedDefaults);

            MainWindow window = new MainWindow(loaded.Settings, store);
            Program.Log("MainWindow constructed");
            MainWindow = window;
            window.Show();
            Program.Log("MainWindow shown");

#if UI_TEST_BUILD
            UiTestCapture.Schedule(window, store, loaded.Settings);
#endif
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Program.Log("Dispatcher exception: " + e.Exception);
            try
            {
                Console.Error.WriteLine(e.Exception.ToString());
            }
            catch
            {
            }
            MessageBox.Show(
                "发生了一个未处理的错误：\n" + e.Exception.Message,
                "薪资显示器",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
