using System;

namespace SalaryMonitor.Core
{
    public enum DayPhase
    {
        RestDay,
        BeforeWork,
        MorningWork,
        LunchBreak,
        AfternoonWork,
        AfterWork
    }

    /// <summary>
    /// A single-day, lunch-break schedule. The default configuration is
    /// 09:30-12:30 plus 14:00-19:00 (480 paid minutes).
    /// </summary>
    public sealed class WorkSchedule
    {
        public int WorkStartMinutes { get; private set; }
        public int LunchStartMinutes { get; private set; }
        public int LunchEndMinutes { get; private set; }
        public int WorkEndMinutes { get; private set; }

        public int MorningMinutes
        {
            get { return Math.Max(0, LunchStartMinutes - WorkStartMinutes); }
        }

        public int AfternoonMinutes
        {
            get { return Math.Max(0, WorkEndMinutes - LunchEndMinutes); }
        }

        public int TotalPaidMinutes
        {
            get { return MorningMinutes + AfternoonMinutes; }
        }

        public WorkSchedule(int workStartMinutes, int lunchStartMinutes,
            int lunchEndMinutes, int workEndMinutes)
        {
            this.WorkStartMinutes = workStartMinutes;
            this.LunchStartMinutes = lunchStartMinutes;
            this.LunchEndMinutes = lunchEndMinutes;
            this.WorkEndMinutes = workEndMinutes;
        }

        public static WorkSchedule FromSettings(AppSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            return new WorkSchedule(
                settings.WorkStartMinutes,
                settings.LunchStartMinutes,
                settings.LunchEndMinutes,
                settings.WorkEndMinutes);
        }

        public static bool IsWeekend(DateTime now)
        {
            return now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;
        }

        public DateTime GetWorkStart(DateTime date)
        {
            return date.Date.AddMinutes(WorkStartMinutes);
        }

        public DateTime GetLunchStart(DateTime date)
        {
            return date.Date.AddMinutes(LunchStartMinutes);
        }

        public DateTime GetLunchEnd(DateTime date)
        {
            return date.Date.AddMinutes(LunchEndMinutes);
        }

        public DateTime GetWorkEnd(DateTime date)
        {
            return date.Date.AddMinutes(WorkEndMinutes);
        }

        public DayPhase GetPhase(DateTime now)
        {
            return GetPhase(now, !IsWeekend(now));
        }

        public DayPhase GetPhase(DateTime now, bool workday)
        {
            if (!workday)
            {
                return DayPhase.RestDay;
            }

            DateTime start = GetWorkStart(now);
            DateTime lunchStart = GetLunchStart(now);
            DateTime lunchEnd = GetLunchEnd(now);
            DateTime end = GetWorkEnd(now);

            if (now < start)
            {
                return DayPhase.BeforeWork;
            }

            if (now < lunchStart)
            {
                return DayPhase.MorningWork;
            }

            if (now < lunchEnd)
            {
                return DayPhase.LunchBreak;
            }

            if (now < end)
            {
                return DayPhase.AfternoonWork;
            }

            return DayPhase.AfterWork;
        }

        /// <summary>
        /// Number of fully completed paid minutes at <paramref name="now"/>.
        /// Compensation is intentionally minute-stepwise: at 09:30:59 this is
        /// still zero, and at 09:31:00 it becomes one.
        /// </summary>
        public int GetCompletedPaidMinutes(DateTime now)
        {
            return GetCompletedPaidMinutes(now, !IsWeekend(now));
        }

        public int GetCompletedPaidMinutes(DateTime now, bool workday)
        {
            if (!workday)
            {
                return 0;
            }

            DateTime start = GetWorkStart(now);
            DateTime lunchStart = GetLunchStart(now);
            DateTime lunchEnd = GetLunchEnd(now);
            DateTime end = GetWorkEnd(now);

            if (now < start)
            {
                return 0;
            }

            if (now < lunchStart)
            {
                return Math.Min(MorningMinutes, CompletedMinutes(start, now));
            }

            if (now < lunchEnd)
            {
                return MorningMinutes;
            }

            if (now < end)
            {
                return MorningMinutes + Math.Min(AfternoonMinutes, CompletedMinutes(lunchEnd, now));
            }

            return TotalPaidMinutes;
        }

        private static int CompletedMinutes(DateTime from, DateTime now)
        {
            if (now <= from)
            {
                return 0;
            }

            long ticks = now.Ticks - from.Ticks;
            return (int)(ticks / TimeSpan.TicksPerMinute);
        }
    }
}
