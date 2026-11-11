using System;

namespace SalaryMonitor.Core
{
    /// <summary>
    /// An immutable rendering snapshot for a single point in time.
    /// </summary>
    public sealed class PaySnapshot
    {
        public DateTime Now { get; private set; }
        public DayPhase Phase { get; private set; }
        public DayKind DayKind { get; private set; }
        public bool ActualWorkdays { get; private set; }
        public int MonthlyWorkdays { get; private set; }
        public bool CalendarDataAvailable { get; private set; }
        public DateTime WorkStart { get; private set; }
        public DateTime WorkEnd { get; private set; }
        public DateTime LunchStart { get; private set; }
        public DateTime LunchEnd { get; private set; }
        public int CompletedPaidMinutes { get; private set; }
        public int TotalPaidMinutes { get; private set; }
        public int RemainingPaidMinutes { get; private set; }
        public decimal DailyWage { get; private set; }
        public decimal HourlyWage { get; private set; }
        public decimal MinuteWage { get; private set; }
        public decimal TodayEarned { get; private set; }
        public double Progress { get; private set; }
        public TimeSpan MainCountdown { get; private set; }
        public DateTime CountdownTarget { get; private set; }

        public bool IsRestDay
        {
            get { return Phase == DayPhase.RestDay; }
        }

        public PaySnapshot(DateTime now, DayPhase phase,
            DayKind dayKind, bool actualWorkdays, int monthlyWorkdays,
            bool calendarDataAvailable,
            DateTime workStart, DateTime workEnd, DateTime lunchStart, DateTime lunchEnd,
            int completedPaidMinutes, int totalPaidMinutes,
            decimal dailyWage, decimal hourlyWage, decimal minuteWage,
            decimal todayEarned, double progress,
            TimeSpan mainCountdown, DateTime countdownTarget)
        {
            this.Now = now;
            this.Phase = phase;
            this.DayKind = dayKind;
            this.ActualWorkdays = actualWorkdays;
            this.MonthlyWorkdays = Math.Max(0, monthlyWorkdays);
            this.CalendarDataAvailable = calendarDataAvailable;
            this.WorkStart = workStart;
            this.WorkEnd = workEnd;
            this.LunchStart = lunchStart;
            this.LunchEnd = lunchEnd;
            this.CompletedPaidMinutes = completedPaidMinutes;
            this.TotalPaidMinutes = totalPaidMinutes;
            this.RemainingPaidMinutes = Math.Max(0, totalPaidMinutes - completedPaidMinutes);
            this.DailyWage = dailyWage;
            this.HourlyWage = hourlyWage;
            this.MinuteWage = minuteWage;
            this.TodayEarned = todayEarned;
            this.Progress = progress;
            this.MainCountdown = mainCountdown < TimeSpan.Zero ? TimeSpan.Zero : mainCountdown;
            this.CountdownTarget = countdownTarget;
        }
    }

    public static class PayCalculator
    {
        public static PaySnapshot Calculate(AppSettings settings, DateTime now)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            WorkSchedule schedule = WorkSchedule.FromSettings(settings);
            bool actualWorkdays = settings.ActualWorkdaysEnabled;
            DayKind dayKind = actualWorkdays
                ? WorkdayCalendar.GetDayKind(now.Date)
                : (WorkSchedule.IsWeekend(now) ? DayKind.Weekend : DayKind.Workday);
            bool workday = dayKind.IsWorkday();
            int monthlyWorkdays = actualWorkdays
                ? WorkdayCalendar.WorkdaysInMonth(now.Year, now.Month)
                : 0;

            DayPhase phase = schedule.GetPhase(now, workday);
            int total = schedule.TotalPaidMinutes;

            decimal wageDays = actualWorkdays
                ? (decimal)monthlyWorkdays
                : settings.MonthlyWorkDays;
            decimal dailyWage = 0m;
            if (wageDays > 0m)
            {
                dailyWage = settings.MonthlySalary / wageDays;
            }

            decimal minuteWage = total > 0 ? dailyWage / total : 0m;
            decimal hourlyWage = minuteWage * 60m;

            int completed = schedule.GetCompletedPaidMinutes(now, workday);
            if (completed < 0)
            {
                completed = 0;
            }
            if (completed > total)
            {
                completed = total;
            }

            decimal earned = 0m;
            if (total > 0 && workday)
            {
                earned = dailyWage * completed / total;
                if (earned < 0m)
                {
                    earned = 0m;
                }
                if (earned > dailyWage)
                {
                    earned = dailyWage;
                }
            }

            double progress = 0.0;
            if (total > 0)
            {
                progress = (double)completed / (double)total;
                if (progress < 0.0) { progress = 0.0; }
                if (progress > 1.0) { progress = 1.0; }
            }

            DateTime target;
            TimeSpan countdown;
            if (phase == DayPhase.BeforeWork)
            {
                target = schedule.GetWorkStart(now);
                countdown = target - now;
            }
            else if (phase == DayPhase.RestDay)
            {
                target = schedule.GetWorkEnd(now);
                countdown = TimeSpan.Zero;
            }
            else
            {
                target = schedule.GetWorkEnd(now);
                countdown = target - now;
                if (countdown < TimeSpan.Zero)
                {
                    countdown = TimeSpan.Zero;
                }
            }

            return new PaySnapshot(
                now,
                phase,
                dayKind,
                actualWorkdays,
                monthlyWorkdays,
                WorkdayCalendar.HasData(now.Year),
                schedule.GetWorkStart(now),
                schedule.GetWorkEnd(now),
                schedule.GetLunchStart(now),
                schedule.GetLunchEnd(now),
                completed,
                total,
                dailyWage,
                hourlyWage,
                minuteWage,
                earned,
                progress,
                countdown,
                target);
        }
    }
}
