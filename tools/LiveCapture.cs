using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

internal static class LiveCapture
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter,
        int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr window);

    public static int Main(string[] args)
    {
        if (args.Length != 2 && args.Length != 4)
        {
            Console.WriteLine("usage: LiveCapture.exe <exe> <output.png> [widthDips heightDips]");
            return 2;
        }

        try
        {
            SetProcessDpiAwarenessContext(new IntPtr(-4));
        }
        catch
        {
        }

        Process process = new Process();
        process.StartInfo.FileName = args[0];
        process.StartInfo.WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(args[0]));
        process.StartInfo.UseShellExecute = false;
        process.Start();

        try
        {
            for (int i = 0; i < 60; i++)
            {
                Thread.Sleep(200);
                process.Refresh();
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    break;
                }
            }

            if (process.MainWindowHandle == IntPtr.Zero)
            {
                Console.WriteLine("No main window.");
                return 1;
            }

            Thread.Sleep(1200);
            process.Refresh();

            if (args.Length == 4)
            {
                double widthDips;
                double heightDips;
                if (double.TryParse(args[2], out widthDips) &&
                    double.TryParse(args[3], out heightDips))
                {
                    uint dpi = GetDpiForWindow(process.MainWindowHandle);
                    double scale = dpi > 0 ? dpi / 96.0 : 1.0;
                    int widthPixels = (int)Math.Round(widthDips * scale);
                    int heightPixels = (int)Math.Round(heightDips * scale);
                    SetWindowPos(process.MainWindowHandle, IntPtr.Zero, 80, 80,
                        widthPixels, heightPixels, 0x0004 | 0x0010);
                    Thread.Sleep(700);
                    process.Refresh();
                }
            }

            RECT rect;
            if (!GetWindowRect(process.MainWindowHandle, out rect))
            {
                Console.WriteLine("GetWindowRect failed: " + Marshal.GetLastWin32Error());
                return 1;
            }

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            Console.WriteLine("RECT=" + rect.Left + "," + rect.Top + " " + width + "x" + height);

            using (Bitmap bitmap = new Bitmap(width, height))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0,
                        new Size(width, height));
                }
                bitmap.Save(args[1], ImageFormat.Png);
            }
            Console.WriteLine("SAVED=" + args[1]);
            return 0;
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit(2000);
                }
            }
            catch
            {
            }
            process.Dispose();
        }
    }
}
