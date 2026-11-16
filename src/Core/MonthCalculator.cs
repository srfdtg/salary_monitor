using System;

namespace SalaryMonitor.Core
{
    /// <summary>
    /// An immutable month-to-date view. It is deliberately calculated from the
    /// current settings and the bundled calendar instead of a stored ledger:
    /// v1.3 does not introduce a database, and a new install can immediately
    /// show the current month's estimate.
    /// </summary>
    public sealed class MonthSnapshot
    {
        public DateTime Now { get; private set; }
        public int Year { get; private set; }
        public int Month { get; private set; }
        public bool ActualWorkdays { get; private set; }
        public bool CalendarDataAvailable { get; private set; }
        public int TotalWorkdays { get; private set; }
        public int CompletedFullWorkdays { get; private set; }
        public decimal CompletedWorkdays { get; private set; }
        public int TodayCompletedPaidMinutes { get; private set; }
        public int TodayTotalPaidMinutes { get; private set; }
        public decimal DailyWage { get; private set; }
        public decimal MonthEarned { get; private set; }
        public double Progress { get; private set; }
        public int Payday { get; private set; }
        public DateTime? NextPayday { get; private set; }
        public int DaysUntilPayday { get; private set; }
        public bool IsPaydayToday { get; private set; }

        public bool PaydayConfigured
        {
            get { return Payday >= 1 && Payday <= 31; }
        }

        public DateTime MonthStart
        {
            get { return new DateTime(Year, Month, 1); }
        }

        public MonthSnapshot(DateTime now, int year, int month,
            bool actualWorkdays, bool calendarDataAvailable,
            int totalWorkdays, int completedFullWorkdays,
            decimal completedWorkdays,
            int todayCompletedPaidMinutes, int todayTotalPaidMinutes,
            decimal dailyWage, decimal monthEarned, double progress,
            int payday, DateTime? nextPayday, int daysUntilPayday,
            bool isPaydayToday)
        {
            this.Now = now;
            this.Year = year;
            this.Month = month;
            this.ActualWorkdays = actualWorkdays;
            this.CalendarDataAvailable = calendarDataAvailable;
            this.TotalWorkdays = Math.Max(0, totalWorkdays);
            this.CompletedFullWorkdays = Math.Max(0, completedFullWorkdays);
            this.CompletedWorkdays = completedWorkdays < 0m ? 0m : completedWorkdays;
            this.TodayCompletedPaidMinutes = Math.Max(0, todayCompletedPaidMinutes);
            this.TodayTotalPaidMinutes = Math.Max(0, todayTotalPaidMinutes);
            this.DailyWage = dailyWage < 0m ? 0m : dailyWage;
            this.MonthEarned = monthEarned < 0m ? 0m : monthEarned;
            this.Progress = progress < 0.0 ? 0.0 : (progress > 1.0 ? 1.0 : progress);
            this.Payday = payday;
            this.NextPayday = nextPayday;
            this.DaysUntilPayday = Math.Max(0, daysUntilPayday);
            this.IsPaydayToday = isPaydayToday;
        }
    }

    public static class MonthCalculator
    {
        public static MonthSnapshot Calculate(AppSettings settings, DateTime now)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            PaySnapshot today = PayCalculator.Calculate(settings, now);
            bool actual = settings.ActualWorkdaysEnabled;
            int totalWorkdays = CountWorkdays(now.Year, now.Month, actual);

            int completedFull = 0;
            DateTime monthStart = new DateTime(now.Year, now.Month, 1);
            for (DateTime day = monthStart; day.Date < now.Date; day = day.AddDays(1))
            {
                if (IsWorkday(day, actual))
                {
                    completedFull++;
                }
            }

            decimal todayFraction = 0m;
            if (today.TotalPaidMinutes > 0 && today.Phase != DayPhase.RestDay)
            {
                todayFraction = (decimal)today.CompletedPaidMinutes /
                    (decimal)today.TotalPaidMinutes;
            }

            decimal completedWorkdays = (decimal)completedFull + todayFraction;
            if (completedWorkdays < 0m)
            {
                completedWorkdays = 0m;
            }
            if (completedWorkdays > (decimal)totalWorkdays)
            {
                completedWorkdays = (decimal)totalWorkdays;
            }

            decimal monthEarned = today.DailyWage * completedWorkdays;
            if (monthEarned < 0m)
            {
                monthEarned = 0m;
            }

            double progress = 0.0;
            if (totalWorkdays > 0)
            {
                progress = (double)(completedWorkdays / (decimal)totalWorkdays);
            }
            if (progress < 0.0)
            {
                progress = 0.0;
            }
            if (progress > 1.0)
            {
                progress = 1.0;
            }

            int payday = settings.Payday;
            DateTime? nextPayday = null;
            int daysUntil = 0;
            bool isToday = false;
            if (payday >= 1 && payday <= 31)
            {
                DateTime effectiveThisMonth = PaydayInMonth(now.Year, now.Month, payday);
                if (now.Date <= effectiveThisMonth.Date)
                {
                    nextPayday = effectiveThisMonth;
                }
                else
                {
                    DateTime nextMonth = monthStart.AddMonths(1);
                    nextPayday = PaydayInMonth(nextMonth.Year, nextMonth.Month, payday);
                }

                daysUntil = (nextPayday.Value.Date - now.Date).Days;
                if (daysUntil < 0)
                {
                    daysUntil = 0;
                }
                isToday = now.Date == effectiveThisMonth.Date;
            }

            return new MonthSnapshot(
                now,
                now.Year,
                now.Month,
                actual,
                actual && WorkdayCalendar.HasData(now.Year),
                totalWorkdays,
                completedFull,
                completedWorkdays,
                today.CompletedPaidMinutes,
                today.TotalPaidMinutes,
                today.DailyWage,
                monthEarned,
                progress,
                payday,
                nextPayday,
                daysUntil,
                isToday);
        }

        public static int CountWorkdays(int year, int month, bool actual)
        {
            int count = 0;
            int days = DateTime.DaysInMonth(year, month);
            for (int day = 1; day <= days; day++)
            {
                if (IsWorkday(new DateTime(year, month, day), actual))
                {
                    count++;
                }
            }
            return count;
        }

        public static bool IsWorkday(DateTime date, bool actual)
        {
            if (actual)
            {
                return WorkdayCalendar.IsWorkday(date);
            }
            return date.DayOfWeek != DayOfWeek.Saturday &&
                date.DayOfWeek != DayOfWeek.Sunday;
        }

        public static DateTime PaydayInMonth(int year, int month, int payday)
        {
            int days = DateTime.DaysInMonth(year, month);
            int effective = payday;
            if (effective < 1)
            {
                effective = 1;
            }
            if (effective > days)
            {
                effective = days;
            }
            return new DateTime(year, month, effective);
        }
    }
}
