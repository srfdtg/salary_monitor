using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace SalaryMonitor.UI
{
    internal static class AppIcon
    {
        private static BitmapSource _cached;

        public static BitmapSource Load()
        {
            if (_cached != null)
            {
                return _cached;
            }

            try
            {
                StreamResourceInfo info = Application.GetResourceStream(new Uri("app.ico", UriKind.Relative));
                if (info == null || info.Stream == null)
                {
                    return null;
                }

                using (System.Drawing.Icon icon = new System.Drawing.Icon(info.Stream))
                {
                    _cached = Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    _cached.Freeze();
                    return _cached;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
