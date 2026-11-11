using System;

namespace SalaryMonitor.Core
{
    /// <summary>
    /// Calendar classification used when the actual-workday mode is enabled.
    /// </summary>
    public enum DayKind
    {
        Workday,
        MakeupWorkday,
        Weekend,
        Holiday
    }

    public static class DayKindExtensions
    {
        public static bool IsWorkday(this DayKind kind)
        {
            return kind == DayKind.Workday || kind == DayKind.MakeupWorkday;
        }
    }
}
