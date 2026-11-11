using System;
using System.Globalization;

namespace SalaryMonitor
{
    internal static class AppClock
    {
#if UI_TEST_BUILD
        private static readonly DateTime TestWallStarted = DateTime.Now;
#endif

        public static DateTime Now
        {
            get
            {
#if UI_TEST_BUILD
                string testNow = Environment.GetEnvironmentVariable("SALARY_MONITOR_TEST_NOW");
                DateTime parsed;
                if (!string.IsNullOrEmpty(testNow) &&
                    DateTime.TryParseExact(testNow, "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                {
                    string running = Environment.GetEnvironmentVariable("SALARY_MONITOR_TEST_RUNNING");
                    if (running == "1")
                    {
                        return parsed + (DateTime.Now - TestWallStarted);
                    }
                    return parsed;
                }
#endif
                return DateTime.Now;
            }
        }
    }
}
