using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SalaryMonitor.Core;

namespace SalaryMonitor.Tests
{
    internal sealed class TestRunner
    {
        private int _passed;
        private int _failed;
        private readonly List<string> _failures = new List<string>();

        public void Run(string name, Action test)
        {
            try
            {
                test();
                _passed++;
                Console.WriteLine("[PASS] " + name);
            }
            catch (Exception ex)
            {
                _failed++;
                string message = name + ": " + ex.Message;
                _failures.Add(message);
                Console.WriteLine("[FAIL] " + message);
                if (!(ex is AssertionException))
                {
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        public int Finish()
        {
            Console.WriteLine();
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine("Automated tests: " + _passed + " passed, " + _failed + " failed.");
            if (_failed > 0)
            {
                Console.WriteLine("Failures:");
                foreach (string failure in _failures)
                {
                    Console.WriteLine(" - " + failure);
                }
                Console.WriteLine("RESULT: FAIL");
                return 1;
            }
            Console.WriteLine("RESULT: PASS");
            return 0;
        }

        public static void Equal<T>(T expected, T actual, string context)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new AssertionException(
                    (context ?? "value") + " expected <" + expected + "> but was <" + actual + ">");
            }
        }

        public static void True(bool condition, string context)
        {
            if (!condition)
            {
                throw new AssertionException((context ?? "condition") + " was false");
            }
        }

        public static void False(bool condition, string context)
        {
            if (condition)
            {
                throw new AssertionException((context ?? "condition") + " was true");
            }
        }

        public static void Near(double expected, double actual, double tolerance, string context)
        {
            if (Math.Abs(expected - actual) > tolerance)
            {
                throw new AssertionException(
                    (context ?? "value") + " expected near <" + expected + "> but was <" + actual + ">");
            }
        }
    }

    internal sealed class AssertionException : Exception
    {
        public AssertionException(string message) : base(message)
        {
        }
    }

    internal static class CalculationTests
    {
        private static readonly DateTime Monday = new DateTime(2026, 11, 9);
        private static readonly DateTime Friday = new DateTime(2026, 11, 6);
        private static readonly DateTime Saturday = new DateTime(2026, 11, 7);
        private static readonly DateTime Sunday = new DateTime(2026, 11, 8);

        private static DateTime At(DateTime day, int hour, int minute, int second)
        {
            return new DateTime(day.Year, day.Month, day.Day, hour, minute, second);
        }

        private static PaySnapshot Snapshot(DateTime now)
        {
            return PayCalculator.Calculate(AppSettings.CreateDefault(), now);
        }

        private static PaySnapshot ActualSnapshot(DateTime now)
        {
            AppSettings settings = AppSettings.CreateDefault();
            settings.ActualWorkdaysEnabled = true;
            return PayCalculator.Calculate(settings, now);
        }

        public static void Run(TestRunner runner)
        {
            RunCalculations(runner);
            RunValidation(runner);
        }

        private static void RunCalculations(TestRunner runner)
        {
            runner.Run("CALC-001 default daily/minute/hourly rates and 480 minutes", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 9, 30, 0));
                TestRunner.Equal(480, s.TotalPaidMinutes, "total paid minutes");
                TestRunner.Equal("¥1,540.23", Formatters.Money(s.DailyWage), "daily wage");
                TestRunner.Equal("¥3.21", Formatters.Money(s.MinuteWage), "minute wage");
                TestRunner.Equal("¥192.53", Formatters.Money(s.HourlyWage), "hourly wage");
            });

            runner.Run("CALC-002 09:00 before work shows countdown to start", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 9, 0, 0));
                TestRunner.Equal(DayPhase.BeforeWork, s.Phase, "phase");
                TestRunner.Equal(0, s.CompletedPaidMinutes, "completed minutes");
                TestRunner.Equal("¥0.00", Formatters.Money(s.TodayEarned), "earned");
                TestRunner.Equal(480, s.RemainingPaidMinutes, "remaining minutes");
                TestRunner.Equal("00:30:00", Formatters.Countdown(s.MainCountdown), "countdown");
                TestRunner.Equal("0%", Formatters.Progress(s.Progress), "progress");
            });

            runner.Run("CALC-003 09:29:59 before work countdown is one second", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 9, 29, 59));
                TestRunner.Equal(DayPhase.BeforeWork, s.Phase, "phase");
                TestRunner.Equal("00:00:01", Formatters.Countdown(s.MainCountdown), "countdown");
                TestRunner.Equal("¥0.00", Formatters.Money(s.TodayEarned), "earned");
            });

            runner.Run("CALC-004 09:30 starts work at zero pay", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 9, 30, 0));
                TestRunner.Equal(DayPhase.MorningWork, s.Phase, "phase");
                TestRunner.Equal(0, s.CompletedPaidMinutes, "completed minutes");
                TestRunner.Equal("¥0.00", Formatters.Money(s.TodayEarned), "earned");
                TestRunner.Equal("09:30:00", Formatters.Countdown(s.MainCountdown), "countdown");
                TestRunner.Equal(480, s.RemainingPaidMinutes, "remaining minutes");
            });

            runner.Run("CALC-005 09:30:59 still has not completed a paid minute", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 9, 30, 59));
                TestRunner.Equal(DayPhase.MorningWork, s.Phase, "phase");
                TestRunner.Equal(0, s.CompletedPaidMinutes, "completed minutes");
                TestRunner.Equal("¥0.00", Formatters.Money(s.TodayEarned), "earned");
            });

            runner.Run("CALC-006 09:31 first paid minute", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 9, 31, 0));
                TestRunner.Equal(1, s.CompletedPaidMinutes, "completed minutes");
                TestRunner.Equal("¥3.21", Formatters.Money(s.TodayEarned), "earned");
                TestRunner.Equal(479, s.RemainingPaidMinutes, "remaining minutes");
                TestRunner.Equal("09:29:00", Formatters.Countdown(s.MainCountdown), "countdown");
                TestRunner.Equal("0.2%", Formatters.Progress(s.Progress), "progress");
            });

            runner.Run("CALC-007 09:32 second paid minute", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 9, 32, 0));
                TestRunner.Equal(2, s.CompletedPaidMinutes, "completed minutes");
                TestRunner.Equal("¥6.42", Formatters.Money(s.TodayEarned), "earned");
            });

            runner.Run("CALC-008 12:30 lunch starts and freezes at three hours", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 12, 30, 0));
                TestRunner.Equal(DayPhase.LunchBreak, s.Phase, "phase");
                TestRunner.Equal(180, s.CompletedPaidMinutes, "completed minutes");
                TestRunner.Equal("¥577.59", Formatters.Money(s.TodayEarned), "earned");
                TestRunner.Equal(300, s.RemainingPaidMinutes, "remaining minutes");
                TestRunner.Equal("37.5%", Formatters.Progress(s.Progress), "progress");
                TestRunner.Equal("06:30:00", Formatters.Countdown(s.MainCountdown), "countdown");
            });

            runner.Run("CALC-009 13:59:59 lunch keeps money frozen, countdown moves", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 13, 59, 59));
                TestRunner.Equal(DayPhase.LunchBreak, s.Phase, "phase");
                TestRunner.Equal(180, s.CompletedPaidMinutes, "completed minutes");
                TestRunner.Equal("¥577.59", Formatters.Money(s.TodayEarned), "earned");
                TestRunner.Equal(300, s.RemainingPaidMinutes, "remaining minutes");
                TestRunner.Equal("05:00:01", Formatters.Countdown(s.MainCountdown), "countdown");
            });

            runner.Run("CALC-010 14:00 afternoon starts without extra pay", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 14, 0, 0));
                TestRunner.Equal(DayPhase.AfternoonWork, s.Phase, "phase");
                TestRunner.Equal(180, s.CompletedPaidMinutes, "completed minutes");
                TestRunner.Equal("¥577.59", Formatters.Money(s.TodayEarned), "earned");
                TestRunner.Equal(300, s.RemainingPaidMinutes, "remaining minutes");
                TestRunner.Equal("05:00:00", Formatters.Countdown(s.MainCountdown), "countdown");
            });

            runner.Run("CALC-011 14:01 resumes pay, progress 37.7%", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 14, 1, 0));
                TestRunner.Equal(DayPhase.AfternoonWork, s.Phase, "phase");
                TestRunner.Equal(181, s.CompletedPaidMinutes, "completed minutes");
                TestRunner.Equal("¥580.80", Formatters.Money(s.TodayEarned), "earned");
                TestRunner.Equal(299, s.RemainingPaidMinutes, "remaining minutes");
                TestRunner.Equal("37.7%", Formatters.Progress(s.Progress), "progress");
                TestRunner.Equal("04:59:00", Formatters.Countdown(s.MainCountdown), "countdown");
            });

            runner.Run("CALC-018 16:28:35 matches the concept numbers", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 16, 28, 35));
                TestRunner.Equal(DayPhase.AfternoonWork, s.Phase, "phase");
                TestRunner.Equal(328, s.CompletedPaidMinutes, "completed minutes");
                TestRunner.Equal("¥1,052.49", Formatters.Money(s.TodayEarned), "earned");
                TestRunner.Equal("68.3%", Formatters.Progress(s.Progress), "progress");
                TestRunner.Equal("02:31:25", Formatters.Countdown(s.MainCountdown), "countdown");
                TestRunner.Equal(152, s.RemainingPaidMinutes, "remaining minutes");
                TestRunner.Equal("2 小时 32 分", Formatters.WorkDuration(s.RemainingPaidMinutes), "remaining label");
            });

            runner.Run("CALC-012 18:59:59 almost complete and one second left", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 18, 59, 59));
                TestRunner.Equal(DayPhase.AfternoonWork, s.Phase, "phase");
                TestRunner.Equal(479, s.CompletedPaidMinutes, "completed minutes");
                TestRunner.Equal("¥1,537.02", Formatters.Money(s.TodayEarned), "earned");
                TestRunner.Equal(1, s.RemainingPaidMinutes, "remaining minutes");
                TestRunner.Equal("00:00:01", Formatters.Countdown(s.MainCountdown), "countdown");
            });

            runner.Run("CALC-013 19:00 full daily pay and complete state", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 19, 0, 0));
                TestRunner.Equal(DayPhase.AfterWork, s.Phase, "phase");
                TestRunner.Equal(480, s.CompletedPaidMinutes, "completed minutes");
                TestRunner.Equal("¥1,540.23", Formatters.Money(s.TodayEarned), "earned");
                TestRunner.Equal(0, s.RemainingPaidMinutes, "remaining minutes");
                TestRunner.Equal("100%", Formatters.Progress(s.Progress), "progress");
                TestRunner.Equal("00:00:00", Formatters.Countdown(s.MainCountdown), "countdown");
            });

            runner.Run("CALC-014 after work stays capped", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 23, 59, 59));
                TestRunner.Equal(DayPhase.AfterWork, s.Phase, "phase");
                TestRunner.Equal("¥1,540.23", Formatters.Money(s.TodayEarned), "earned");
                TestRunner.Equal(480, s.CompletedPaidMinutes, "completed minutes");
                TestRunner.Equal(0, s.RemainingPaidMinutes, "remaining minutes");
            });

            runner.Run("CALC-015 Saturday is rest day with zero pay", delegate
            {
                PaySnapshot s = Snapshot(At(Saturday, 12, 0, 0));
                TestRunner.Equal(DayPhase.RestDay, s.Phase, "phase");
                TestRunner.Equal(0, s.CompletedPaidMinutes, "completed minutes");
                TestRunner.Equal("¥0.00", Formatters.Money(s.TodayEarned), "earned");
                TestRunner.Equal("0%", Formatters.Progress(s.Progress), "progress");
            });

            runner.Run("CALC-016 Sunday is rest day with zero pay", delegate
            {
                PaySnapshot s = Snapshot(At(Sunday, 19, 30, 0));
                TestRunner.Equal(DayPhase.RestDay, s.Phase, "phase");
                TestRunner.Equal("¥0.00", Formatters.Money(s.TodayEarned), "earned");
            });

            runner.Run("CALC-017 Friday still counts as a workday", delegate
            {
                PaySnapshot s = Snapshot(At(Friday, 10, 0, 0));
                TestRunner.Equal(DayPhase.MorningWork, s.Phase, "phase");
                TestRunner.Equal(30, s.CompletedPaidMinutes, "completed minutes");
                TestRunner.Equal("¥96.26", Formatters.Money(s.TodayEarned), "earned");
            });

            runner.Run("CALC-019 midnight is before work", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 0, 0, 0));
                TestRunner.Equal(DayPhase.BeforeWork, s.Phase, "phase");
                TestRunner.Equal("09:30:00", Formatters.Countdown(s.MainCountdown), "countdown");
            });

            runner.Run("CALC-020 salary never leaves its bounds", delegate
            {
                DateTime[] samples = new DateTime[]
                {
                    At(Monday, 0, 0, 0), At(Monday, 9, 30, 0), At(Monday, 12, 30, 0),
                    At(Monday, 14, 0, 0), At(Monday, 19, 0, 0), At(Monday, 23, 59, 59),
                    At(Saturday, 10, 0, 0)
                };
                foreach (DateTime sample in samples)
                {
                    PaySnapshot s = Snapshot(sample);
                    TestRunner.True(s.TodayEarned >= 0m, "earned >= 0 at " + sample);
                    TestRunner.True(s.TodayEarned <= s.DailyWage, "earned <= daily at " + sample);
                    TestRunner.True(s.Progress >= 0.0 && s.Progress <= 1.0, "progress bounds at " + sample);
                    TestRunner.True(s.RemainingPaidMinutes >= 0, "remaining >= 0 at " + sample);
                }
            });

            runner.Run("CALC-021 510-minute schedule uses its own minute rate", delegate
            {
                AppSettings settings = AppSettings.CreateDefault();
                settings.WorkStartMinutes = 9 * 60;
                settings.LunchStartMinutes = 12 * 60 + 30;
                settings.LunchEndMinutes = 14 * 60;
                settings.WorkEndMinutes = 19 * 60;
                PaySnapshot s = PayCalculator.Calculate(settings, At(Monday, 12, 30, 0));
                TestRunner.Equal(510, s.TotalPaidMinutes, "total paid minutes");
                TestRunner.Equal("¥3.02", Formatters.Money(s.MinuteWage), "minute wage");
                TestRunner.Equal("¥181.20", Formatters.Money(s.HourlyWage), "hourly wage");
                TestRunner.Equal("¥1,540.23", Formatters.Money(
                    PayCalculator.Calculate(settings, At(Monday, 19, 0, 0)).TodayEarned), "end pay");
            });

            runner.Run("CALC-022 420-minute schedule uses its own minute rate", delegate
            {
                AppSettings settings = AppSettings.CreateDefault();
                settings.WorkStartMinutes = 9 * 60;
                settings.LunchStartMinutes = 12 * 60;
                settings.LunchEndMinutes = 13 * 60;
                settings.WorkEndMinutes = 17 * 60;
                PaySnapshot noon = PayCalculator.Calculate(settings, At(Monday, 12, 0, 0));
                PaySnapshot end = PayCalculator.Calculate(settings, At(Monday, 17, 0, 0));
                TestRunner.Equal(420, noon.TotalPaidMinutes, "total paid minutes");
                TestRunner.Equal("¥3.67", Formatters.Money(noon.MinuteWage), "minute wage");
                TestRunner.Equal(DayPhase.LunchBreak, noon.Phase, "noon phase");
                TestRunner.Equal("¥1,540.23", Formatters.Money(end.TodayEarned), "end pay");
                TestRunner.Equal(DayPhase.AfterWork, end.Phase, "end phase");
            });

            runner.Run("CALC-023 full-day calculation is internally exact", delegate
            {
                PaySnapshot s = Snapshot(At(Monday, 19, 0, 0));
                TestRunner.Equal(s.DailyWage, s.TodayEarned, "today earned equals daily wage");
            });

            runner.Run("ACT-001 2026 monthly workday counts", delegate
            {
                int[] expected = new int[] { 21, 16, 22, 21, 19, 21, 23, 21, 22, 18, 21, 23 };
                int total = 0;
                for (int month = 1; month <= 12; month++)
                {
                    int count = WorkdayCalendar.WorkdaysInMonth(2026, month);
                    TestRunner.Equal(expected[month - 1], count, "2026-" + month);
                    total += count;
                }
                TestRunner.Equal(248, total, "2026 total");
            });

            runner.Run("ACT-002 2027 monthly workday counts", delegate
            {
                int[] expected = new int[] { 21, 15, 23, 21, 19, 21, 22, 22, 22, 17, 22, 23 };
                int total = 0;
                for (int month = 1; month <= 12; month++)
                {
                    int count = WorkdayCalendar.WorkdaysInMonth(2027, month);
                    TestRunner.Equal(expected[month - 1], count, "2027-" + month);
                    total += count;
                }
                TestRunner.Equal(248, total, "2027 total");
            });

            runner.Run("ACT-003 actual mode uses November 2026 count of 21", delegate
            {
                PaySnapshot s = ActualSnapshot(At(Monday, 9, 31, 0));
                TestRunner.True(s.ActualWorkdays, "actual flag");
                TestRunner.True(s.CalendarDataAvailable, "calendar data");
                TestRunner.Equal(DayKind.Workday, s.DayKind, "day kind");
                TestRunner.Equal(21, s.MonthlyWorkdays, "monthly workdays");
                TestRunner.Equal("¥1,595.24", Formatters.Money(s.DailyWage), "daily wage");
                TestRunner.Equal("¥3.32", Formatters.Money(s.MinuteWage), "minute wage");
                TestRunner.Equal("¥199.40", Formatters.Money(s.HourlyWage), "hourly wage");
                TestRunner.Equal("¥3.32", Formatters.Money(s.TodayEarned), "earned");
            });

            runner.Run("ACT-004 2026 National Day is a holiday and uses 18 days", delegate
            {
                PaySnapshot s = ActualSnapshot(At(new DateTime(2026, 10, 1), 12, 0, 0));
                TestRunner.Equal(DayPhase.RestDay, s.Phase, "phase");
                TestRunner.Equal(DayKind.Holiday, s.DayKind, "day kind");
                TestRunner.Equal(18, s.MonthlyWorkdays, "monthly workdays");
                TestRunner.Equal("¥1,861.11", Formatters.Money(s.DailyWage), "daily wage");
                TestRunner.Equal("¥0.00", Formatters.Money(s.TodayEarned), "earned");
            });

            runner.Run("ACT-005 2026-10-10 Saturday is a make-up workday", delegate
            {
                PaySnapshot s = ActualSnapshot(At(new DateTime(2026, 10, 10), 9, 31, 0));
                TestRunner.Equal(DayPhase.MorningWork, s.Phase, "phase");
                TestRunner.Equal(DayKind.MakeupWorkday, s.DayKind, "day kind");
                TestRunner.Equal("¥3.88", Formatters.Money(s.TodayEarned), "earned");
            });

            runner.Run("ACT-006 2026-09-20 Sunday is a make-up workday", delegate
            {
                PaySnapshot s = ActualSnapshot(At(new DateTime(2026, 9, 20), 9, 31, 0));
                TestRunner.Equal(DayPhase.MorningWork, s.Phase, "phase");
                TestRunner.Equal(DayKind.MakeupWorkday, s.DayKind, "day kind");
                TestRunner.Equal(22, s.MonthlyWorkdays, "monthly workdays");
                TestRunner.Equal("¥3.17", Formatters.Money(s.TodayEarned), "earned");
            });

            runner.Run("ACT-007 2026 Spring Festival holiday has zero pay", delegate
            {
                PaySnapshot s = ActualSnapshot(At(new DateTime(2026, 2, 17), 10, 0, 0));
                TestRunner.Equal(DayPhase.RestDay, s.Phase, "phase");
                TestRunner.Equal(DayKind.Holiday, s.DayKind, "day kind");
                TestRunner.Equal(16, s.MonthlyWorkdays, "monthly workdays");
                TestRunner.Equal("¥2,093.75", Formatters.Money(s.DailyWage), "daily wage");
                TestRunner.Equal("¥0.00", Formatters.Money(s.TodayEarned), "earned");
            });

            runner.Run("ACT-008 2027 Spring Festival holiday and 15-day month", delegate
            {
                PaySnapshot s = ActualSnapshot(At(new DateTime(2027, 2, 5), 10, 0, 0));
                TestRunner.Equal(DayPhase.RestDay, s.Phase, "phase");
                TestRunner.Equal(DayKind.Holiday, s.DayKind, "day kind");
                TestRunner.Equal(15, s.MonthlyWorkdays, "monthly workdays");
                TestRunner.Equal("¥2,233.33", Formatters.Money(s.DailyWage), "daily wage");
                TestRunner.Equal("¥0.00", Formatters.Money(s.TodayEarned), "earned");
            });

            runner.Run("ACT-009 2027-02-14 Sunday make-up and next Monday work", delegate
            {
                PaySnapshot sunday = ActualSnapshot(At(new DateTime(2027, 2, 14), 9, 31, 0));
                PaySnapshot monday = ActualSnapshot(At(new DateTime(2027, 2, 15), 9, 31, 0));
                TestRunner.Equal(DayKind.MakeupWorkday, sunday.DayKind, "Sunday kind");
                TestRunner.Equal(DayPhase.MorningWork, sunday.Phase, "Sunday phase");
                TestRunner.Equal("¥4.65", Formatters.Money(sunday.TodayEarned), "Sunday earned");
                TestRunner.Equal(DayKind.Workday, monday.DayKind, "Monday kind");
                TestRunner.Equal("¥4.65", Formatters.Money(monday.TodayEarned), "Monday earned");
            });

            runner.Run("ACT-010 2027 National Day holiday and make-up Saturday", delegate
            {
                PaySnapshot holiday = ActualSnapshot(At(new DateTime(2027, 10, 1), 12, 0, 0));
                PaySnapshot makeup = ActualSnapshot(At(new DateTime(2027, 10, 9), 9, 31, 0));
                TestRunner.Equal(DayKind.Holiday, holiday.DayKind, "holiday kind");
                TestRunner.Equal(17, holiday.MonthlyWorkdays, "October workdays");
                TestRunner.Equal("¥0.00", Formatters.Money(holiday.TodayEarned), "holiday earned");
                TestRunner.Equal(DayKind.MakeupWorkday, makeup.DayKind, "makeup kind");
                TestRunner.Equal("¥4.11", Formatters.Money(makeup.TodayEarned), "makeup earned");
            });

            runner.Run("ACT-011 outside 2026-2027 falls back to Monday-Friday", delegate
            {
                PaySnapshot monday = ActualSnapshot(At(new DateTime(2028, 1, 3), 9, 31, 0));
                PaySnapshot saturday = ActualSnapshot(At(new DateTime(2028, 1, 1), 12, 0, 0));
                TestRunner.Equal(DayKind.Workday, monday.DayKind, "Monday kind");
                TestRunner.False(monday.CalendarDataAvailable, "calendar data flag");
                TestRunner.Equal(21, monday.MonthlyWorkdays, "January 2028 weekdays");
                TestRunner.Equal("¥3.32", Formatters.Money(monday.TodayEarned), "Monday earned");
                TestRunner.Equal(DayKind.Weekend, saturday.DayKind, "Saturday kind");
                TestRunner.Equal(DayPhase.RestDay, saturday.Phase, "Saturday phase");
            });

            runner.Run("ACT-012 average mode ignores the holiday table", delegate
            {
                PaySnapshot s = Snapshot(At(new DateTime(2026, 10, 1), 9, 31, 0));
                TestRunner.Equal(DayPhase.MorningWork, s.Phase, "phase");
                TestRunner.Equal(DayKind.Workday, s.DayKind, "day kind");
                TestRunner.Equal(0, s.MonthlyWorkdays, "monthly workdays");
                TestRunner.Equal("¥3.21", Formatters.Money(s.TodayEarned), "earned");
            });

            runner.Run("ACT-013 actual mode reaches the exact daily wage at 19:00", delegate
            {
                PaySnapshot s = ActualSnapshot(At(Monday, 19, 0, 0));
                TestRunner.Equal(DayPhase.AfterWork, s.Phase, "phase");
                TestRunner.Equal(s.DailyWage, s.TodayEarned, "daily equals earned");
                TestRunner.Equal(480, s.CompletedPaidMinutes, "completed");
            });

            runner.Run("ACT-014 actual-workday flag survives clone", delegate
            {
                AppSettings s = AppSettings.CreateDefault();
                s.ActualWorkdaysEnabled = true;
                TestRunner.True(s.Clone().ActualWorkdaysEnabled, "clone flag");
            });

            runner.Run("ACT-015 calendar data covers only 2026-2027", delegate
            {
                TestRunner.True(WorkdayCalendar.HasData(2026), "2026");
                TestRunner.True(WorkdayCalendar.HasData(2027), "2027");
                TestRunner.False(WorkdayCalendar.HasData(2025), "2025");
                TestRunner.False(WorkdayCalendar.HasData(2028), "2028");
            });

            runner.Run("ACT-016 day kinds and holiday boundaries", delegate
            {
                TestRunner.Equal(DayKind.Holiday,
                    WorkdayCalendar.GetDayKind(new DateTime(2026, 1, 1)), "2026 New Year");
                TestRunner.Equal(DayKind.MakeupWorkday,
                    WorkdayCalendar.GetDayKind(new DateTime(2026, 1, 4)), "2026 Jan makeup");
                TestRunner.Equal(DayKind.Workday,
                    WorkdayCalendar.GetDayKind(Monday), "normal Monday");
                TestRunner.Equal(DayKind.Weekend,
                    WorkdayCalendar.GetDayKind(Saturday), "normal Saturday");
                TestRunner.Equal(DayKind.Holiday,
                    WorkdayCalendar.GetDayKind(new DateTime(2027, 6, 9)), "2027 Dragon Boat");
                TestRunner.Equal(DayKind.MakeupWorkday,
                    WorkdayCalendar.GetDayKind(new DateTime(2027, 10, 9)), "2027 National Day makeup");
            });

            runner.Run("ACT-017 month label and day input", delegate
            {
                TestRunner.Equal("2026年11月", Formatters.MonthLabel(new DateTime(2026, 11, 11)), "month");
                TestRunner.Equal("21.75", Formatters.DaysInput(21.75m), "days input");
                TestRunner.Equal("31", Formatters.DaysInput(31m), "whole days");
            });

            runner.Run("FMT-001 currency formatting round-half-away and grouping", delegate
            {
                TestRunner.Equal("¥0.00", Formatters.Money(0m), "zero");
                TestRunner.Equal("¥3.20", Formatters.Money(3.2m), "one decimal");
                TestRunner.Equal("¥1,540.23", Formatters.Money(1540.229885m), "grouping and rounding");
                TestRunner.Equal("¥1,000.00", Formatters.Money(1000m), "thousands");
                TestRunner.Equal("¥3.21", Formatters.Money(3.205m), "half away from zero");
            });

            runner.Run("FMT-002 countdown formatting and ceiling", delegate
            {
                TestRunner.Equal("00:00:00", Formatters.Countdown(TimeSpan.Zero), "zero");
                TestRunner.Equal("00:00:01", Formatters.Countdown(TimeSpan.FromSeconds(0.2)), "ceiling");
                TestRunner.Equal("00:59:59", Formatters.Countdown(TimeSpan.FromSeconds(59 * 60 + 59)), "minutes");
                TestRunner.Equal("09:30:00", Formatters.Countdown(TimeSpan.FromMinutes(570)), "hours");
                TestRunner.Equal("00:00:00", Formatters.Countdown(TimeSpan.FromSeconds(-1)), "negative");
            });

            runner.Run("FMT-003 progress formatting", delegate
            {
                TestRunner.Equal("0%", Formatters.Progress(0.0), "zero");
                TestRunner.Equal("37.5%", Formatters.Progress(0.375), "three eighths");
                TestRunner.Equal("68.3%", Formatters.Progress(328.0 / 480.0), "typical");
                TestRunner.Equal("100%", Formatters.Progress(1.0), "full");
            });

            runner.Run("FMT-004 duration formatting", delegate
            {
                TestRunner.Equal("8 小时 0 分", Formatters.WorkDuration(480), "full day");
                TestRunner.Equal("5 小时 0 分", Formatters.WorkDuration(300), "five hours");
                TestRunner.Equal("2 小时 32 分", Formatters.WorkDuration(152), "two hours");
                TestRunner.Equal("32 分钟", Formatters.WorkDuration(32), "minutes only");
                TestRunner.Equal("0 分钟", Formatters.WorkDuration(0), "zero");
            });

            runner.Run("FMT-005 time-of-day helpers", delegate
            {
                DateTime now = At(Monday, 16, 28, 35);
                TestRunner.Equal("11月9日 · 周一", Formatters.DateHeader(now), "date header");
                TestRunner.Equal("16:28:35", Formatters.TimeOfDay(now), "clock");
                TestRunner.Equal("09:30", Formatters.ClockHHmm(570), "HHmm");
            });

            runner.Run("PARSE-001 time input accepts H:mm and HH:mm", delegate
            {
                int value;
                TestRunner.True(Formatters.TryParseTime("9:05", out value), "single-digit hour");
                TestRunner.Equal(545, value, "parsed minute");
                TestRunner.True(Formatters.TryParseTime("09:05", out value), "two-digit hour");
                TestRunner.Equal(545, value, "parsed minute");
                TestRunner.True(Formatters.TryParseTime("19:00", out value), "evening");
                TestRunner.Equal(1140, value, "evening minute");
            });

            runner.Run("PARSE-002 invalid time formats are rejected", delegate
            {
                int value;
                TestRunner.False(Formatters.TryParseTime("24:00", out value), "hour range");
                TestRunner.False(Formatters.TryParseTime("12:60", out value), "minute range");
                TestRunner.False(Formatters.TryParseTime("9:5", out value), "short minute");
                TestRunner.False(Formatters.TryParseTime("9", out value), "no colon");
                TestRunner.False(Formatters.TryParseTime("", out value), "blank");
                TestRunner.False(Formatters.TryParseTime("9:05x", out value), "suffix");
            });

            runner.Run("PARSE-003 money accepts grouping and decimal point", delegate
            {
                decimal value;
                TestRunner.True(Formatters.TryParseNumber("33,500.00", out value), "grouped");
                TestRunner.Equal(33500m, value, "grouped value");
                TestRunner.True(Formatters.TryParseNumber("21.75", out value), "days value");
                TestRunner.Equal(21.75m, value, "days");
                TestRunner.False(Formatters.TryParseNumber("", out value), "blank");
                TestRunner.False(Formatters.TryParseNumber("abc", out value), "letters");
            });
        }

        private static void RunValidation(TestRunner runner)
        {
            runner.Run("VALID-001 defaults are valid", delegate
            {
                TestRunner.True(SettingsValidator.Validate(AppSettings.CreateDefault()).IsValid, "defaults");
            });

            runner.Run("VALID-002 salary bounds are validated", delegate
            {
                AppSettings s = AppSettings.CreateDefault();
                s.MonthlySalary = 0m;
                TestRunner.False(SettingsValidator.Validate(s).IsValid, "zero salary");
                s.MonthlySalary = -1m;
                TestRunner.False(SettingsValidator.Validate(s).IsValid, "negative salary");
                s.MonthlySalary = SettingsValidator.MaxMonthlySalary + 1m;
                TestRunner.False(SettingsValidator.Validate(s).IsValid, "too large salary");
            });

            runner.Run("VALID-003 work-day bounds are validated", delegate
            {
                AppSettings s = AppSettings.CreateDefault();
                s.MonthlyWorkDays = 0m;
                TestRunner.False(SettingsValidator.Validate(s).IsValid, "zero days");
                s.MonthlyWorkDays = 31.01m;
                TestRunner.False(SettingsValidator.Validate(s).IsValid, "over 31 days");
                s.MonthlyWorkDays = 31m;
                TestRunner.True(SettingsValidator.Validate(s).IsValid, "31 days accepted");
            });

            runner.Run("VALID-004 time ranges are validated", delegate
            {
                AppSettings s = AppSettings.CreateDefault();
                s.WorkStartMinutes = -1;
                TestRunner.False(SettingsValidator.Validate(s).IsValid, "negative time");
                s = AppSettings.CreateDefault();
                s.WorkEndMinutes = 1440;
                TestRunner.False(SettingsValidator.Validate(s).IsValid, "time 24:00");
            });

            runner.Run("VALID-005 time ordering is validated", delegate
            {
                AppSettings s = AppSettings.CreateDefault();
                s.WorkStartMinutes = s.LunchStartMinutes;
                TestRunner.False(SettingsValidator.Validate(s).IsValid, "start equals lunch");
                s = AppSettings.CreateDefault();
                s.LunchStartMinutes = s.LunchEndMinutes;
                TestRunner.False(SettingsValidator.Validate(s).IsValid, "lunch start equals end");
                s = AppSettings.CreateDefault();
                s.LunchEndMinutes = s.WorkEndMinutes;
                TestRunner.False(SettingsValidator.Validate(s).IsValid, "lunch end equals work end");
            });

            runner.Run("VALID-006 early-morning schedule is allowed", delegate
            {
                AppSettings s = AppSettings.CreateDefault();
                s.WorkStartMinutes = 0;
                s.LunchStartMinutes = 4 * 60;
                s.LunchEndMinutes = 5 * 60;
                s.WorkEndMinutes = 9 * 60;
                TestRunner.True(SettingsValidator.Validate(s).IsValid, "early schedule");
                TestRunner.Equal(480, WorkSchedule.FromSettings(s).TotalPaidMinutes, "paid minutes");
            });

            runner.Run("VALID-007 overnight schedule is rejected by same-day ordering", delegate
            {
                AppSettings s = AppSettings.CreateDefault();
                s.WorkStartMinutes = 22 * 60;
                s.LunchStartMinutes = 23 * 60;
                s.LunchEndMinutes = 23 * 60 + 30;
                s.WorkEndMinutes = 6 * 60;
                TestRunner.False(SettingsValidator.Validate(s).IsValid, "overnight ordering");
            });
        }
    }

    internal static class StorageTests
    {
        public static void Run(TestRunner runner)
        {
            runner.Run("STORE-001 missing file returns defaults", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    SettingsStore store = new SettingsStore(Path.Combine(dir, "settings.json"));
                    SettingsLoadResult result = store.Load();
                    TestRunner.True(result.UsedDefaults, "used defaults");
                    TestRunner.Equal(33500m, result.Settings.MonthlySalary, "default salary");
                    TestRunner.Equal(570, result.Settings.WorkStartMinutes, "default start");
                    TestRunner.True(result.Settings.AlwaysOnTop, "default topmost");
                });
            });

            runner.Run("STORE-002 round-trips every field and topmost state", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    string path = Path.Combine(dir, "nested", "settings.json");
                    SettingsStore store = new SettingsStore(path);
                    AppSettings settings = AppSettings.CreateDefault();
                    settings.MonthlySalary = 40000m;
                    settings.MonthlyWorkDays = 22m;
                    settings.WorkStartMinutes = 9 * 60;
                    settings.LunchStartMinutes = 12 * 60;
                    settings.LunchEndMinutes = 13 * 60;
                    settings.WorkEndMinutes = 18 * 60;
                    settings.AlwaysOnTop = false;
                    settings.ActualWorkdaysEnabled = true;

                    SettingsSaveResult save = store.Save(settings);
                    TestRunner.True(save.Success, "save success: " + save.ErrorMessage);
                    TestRunner.True(File.Exists(path), "file exists");

                    SettingsLoadResult load = store.Load();
                    TestRunner.False(load.UsedDefaults, "not defaults");
                    TestRunner.Equal(40000m, load.Settings.MonthlySalary, "salary");
                    TestRunner.Equal(22m, load.Settings.MonthlyWorkDays, "days");
                    TestRunner.Equal(540, load.Settings.WorkStartMinutes, "start");
                    TestRunner.Equal(720, load.Settings.LunchStartMinutes, "lunch start");
                    TestRunner.Equal(780, load.Settings.LunchEndMinutes, "lunch end");
                    TestRunner.Equal(1080, load.Settings.WorkEndMinutes, "end");
                    TestRunner.False(load.Settings.AlwaysOnTop, "topmost");
                    TestRunner.True(load.Settings.ActualWorkdaysEnabled, "actual workdays");
                });
            });

            runner.Run("STORE-003 corrupt JSON falls back safely", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    string path = Path.Combine(dir, "settings.json");
                    File.WriteAllText(path, "{ this is not json", new UTF8Encoding(false));
                    SettingsStore store = new SettingsStore(path);
                    SettingsLoadResult result = store.Load();
                    TestRunner.True(result.UsedDefaults, "used defaults");
                    TestRunner.True(result.ErrorMessage != null, "error message recorded");
                });
            });

            runner.Run("STORE-004 invalid semantic values fall back safely", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    string path = Path.Combine(dir, "settings.json");
                    string json = "{\"version\":1,\"monthlySalary\":0,\"monthlyWorkDays\":21.75," +
                        "\"workStartMinutes\":570,\"lunchStartMinutes\":750," +
                        "\"lunchEndMinutes\":840,\"workEndMinutes\":1140,\"alwaysOnTop\":true}";
                    File.WriteAllText(path, json, new UTF8Encoding(false));
                    SettingsStore store = new SettingsStore(path);
                    SettingsLoadResult result = store.Load();
                    TestRunner.True(result.UsedDefaults, "used defaults");
                    TestRunner.Equal(33500m, result.Settings.MonthlySalary, "default salary");
                });
            });

            runner.Run("STORE-005 unsupported version falls back safely", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    string path = Path.Combine(dir, "settings.json");
                    AppSettings settings = AppSettings.CreateDefault();
                    SettingsStore writer = new SettingsStore(path);
                    TestRunner.True(writer.Save(settings).Success, "seed save");
                    string json = File.ReadAllText(path).Replace("\"version\":2", "\"version\":3");
                    File.WriteAllText(path, json, new UTF8Encoding(false));
                    SettingsLoadResult result = new SettingsStore(path).Load();
                    TestRunner.True(result.UsedDefaults, "version fallback");
                });
            });

            runner.Run("STORE-006 unknown fields are tolerated", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    string path = Path.Combine(dir, "settings.json");
                    string json = "{\"version\":1,\"monthlySalary\":40000,\"monthlyWorkDays\":21.75," +
                        "\"workStartMinutes\":570,\"lunchStartMinutes\":750," +
                        "\"lunchEndMinutes\":840,\"workEndMinutes\":1140," +
                        "\"alwaysOnTop\":false,\"futureField\":\"ignored\"}";
                    File.WriteAllText(path, json, new UTF8Encoding(false));
                    SettingsLoadResult result = new SettingsStore(path).Load();
                    TestRunner.False(result.UsedDefaults, "known fields loaded");
                    TestRunner.Equal(40000m, result.Settings.MonthlySalary, "salary");
                });
            });

            runner.Run("STORE-007 save failure is reported without throwing", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    string blocker = Path.Combine(dir, "blocked");
                    File.WriteAllText(blocker, "not a directory", new UTF8Encoding(false));
                    string path = Path.Combine(blocker, "settings.json");
                    SettingsStore store = new SettingsStore(path);
                    SettingsSaveResult result = store.Save(AppSettings.CreateDefault());
                    TestRunner.False(result.Success, "save must fail");
                    TestRunner.True(result.ErrorMessage.Length > 0, "error message present");
                });
            });

            runner.Run("STORE-008 save leaves no temp artifacts", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    SettingsStore store = new SettingsStore(Path.Combine(dir, "settings.json"));
                    TestRunner.True(store.Save(AppSettings.CreateDefault()).Success, "save success");
                    string[] files = Directory.GetFiles(dir);
                    TestRunner.Equal(1, files.Length, "only one file");
                    TestRunner.True(files[0].EndsWith("settings.json", StringComparison.OrdinalIgnoreCase), "file name");
                });
            });

            runner.Run("STORE-009 invalid model is rejected before writing", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    SettingsStore store = new SettingsStore(Path.Combine(dir, "settings.json"));
                    AppSettings invalid = AppSettings.CreateDefault();
                    invalid.MonthlySalary = -5m;
                    SettingsSaveResult result = store.Save(invalid);
                    TestRunner.False(result.Success, "save rejected");
                    TestRunner.False(File.Exists(store.SettingsPath), "no file written");
                });
            });

            runner.Run("STORE-010 a second save overwrites the existing file", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    SettingsStore store = new SettingsStore(Path.Combine(dir, "settings.json"));
                    AppSettings first = AppSettings.CreateDefault();
                    first.MonthlySalary = 40000m;
                    first.AlwaysOnTop = true;
                    TestRunner.True(store.Save(first).Success, "first save");

                    AppSettings second = first.Clone();
                    second.AlwaysOnTop = false;
                    SettingsSaveResult update = store.Save(second);
                    TestRunner.True(update.Success, "update save: " + update.ErrorMessage);

                    SettingsLoadResult loaded = store.Load();
                    TestRunner.False(loaded.UsedDefaults, "loaded updated file");
                    TestRunner.Equal(40000m, loaded.Settings.MonthlySalary, "salary retained");
                    TestRunner.False(loaded.Settings.AlwaysOnTop, "topmost updated");
                });
            });

            runner.Run("STORE-011 roaming is used when no portable settings exist", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    string roamingDir = Path.Combine(dir, "roaming");
                    string portableDir = Path.Combine(dir, "portable");
                    Directory.CreateDirectory(roamingDir);
                    Directory.CreateDirectory(portableDir);
                    string roaming = Path.Combine(roamingDir, "settings.json");
                    string portable = Path.Combine(portableDir, "settings.json");
                    Dictionary<string, bool> writable = Writable(roamingDir, true, portableDir, true);

                    string selected = SettingsPathResolver.Resolve(roaming, portable,
                        delegate(string path) { return writable[path]; });

                    TestRunner.Equal(roaming, selected, "selected roaming path");
                });
            });

            runner.Run("STORE-012 an existing portable settings file wins", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    string roamingDir = Path.Combine(dir, "roaming");
                    string portableDir = Path.Combine(dir, "portable");
                    Directory.CreateDirectory(roamingDir);
                    Directory.CreateDirectory(portableDir);
                    string roaming = Path.Combine(roamingDir, "settings.json");
                    string portable = Path.Combine(portableDir, "settings.json");
                    File.WriteAllText(roaming, "roaming", new UTF8Encoding(false));
                    File.WriteAllText(portable, "portable", new UTF8Encoding(false));
                    Dictionary<string, bool> writable = Writable(roamingDir, true, portableDir, true);

                    string selected = SettingsPathResolver.Resolve(roaming, portable,
                        delegate(string path) { return writable[path]; });

                    TestRunner.Equal(portable, selected, "selected portable path");
                });
            });

            runner.Run("STORE-013 read-only roaming settings migrate beside the exe", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    string roamingDir = Path.Combine(dir, "roaming");
                    string portableDir = Path.Combine(dir, "portable");
                    Directory.CreateDirectory(roamingDir);
                    Directory.CreateDirectory(portableDir);
                    string roaming = Path.Combine(roamingDir, "settings.json");
                    string portable = Path.Combine(portableDir, "settings.json");
                    File.WriteAllText(roaming, "seed-settings", new UTF8Encoding(false));
                    Dictionary<string, bool> writable = Writable(roamingDir, false, portableDir, true);

                    string selected = SettingsPathResolver.Resolve(roaming, portable,
                        delegate(string path) { return writable[path]; });

                    TestRunner.Equal(portable, selected, "selected portable path");
                    TestRunner.True(File.Exists(portable), "portable file created");
                    TestRunner.Equal("seed-settings", File.ReadAllText(portable), "settings copied");
                });
            });

            runner.Run("STORE-014 portable file is read even when nothing is writable", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    string roamingDir = Path.Combine(dir, "roaming");
                    string portableDir = Path.Combine(dir, "portable");
                    Directory.CreateDirectory(roamingDir);
                    Directory.CreateDirectory(portableDir);
                    string roaming = Path.Combine(roamingDir, "settings.json");
                    string portable = Path.Combine(portableDir, "settings.json");
                    File.WriteAllText(roaming, "roaming", new UTF8Encoding(false));
                    File.WriteAllText(portable, "portable", new UTF8Encoding(false));
                    Dictionary<string, bool> writable = Writable(roamingDir, false, portableDir, false);

                    string selected = SettingsPathResolver.Resolve(roaming, portable,
                        delegate(string path) { return writable[path]; });

                    TestRunner.Equal(portable, selected, "readable portable path");
                });
            });

            runner.Run("STORE-015 failed migration keeps the readable roaming file", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    string roamingDir = Path.Combine(dir, "roaming");
                    string portableDir = Path.Combine(dir, "portable");
                    Directory.CreateDirectory(roamingDir);
                    Directory.CreateDirectory(portableDir);
                    string roaming = Path.Combine(roamingDir, "settings.json");
                    string portable = Path.Combine(portableDir, "settings.json");
                    File.WriteAllText(roaming, "seed-settings", new UTF8Encoding(false));
                    Directory.CreateDirectory(portable);
                    Dictionary<string, bool> writable = Writable(roamingDir, false, portableDir, true);

                    string selected = SettingsPathResolver.Resolve(roaming, portable,
                        delegate(string path) { return writable[path]; });

                    TestRunner.Equal(roaming, selected, "roaming retained after failed copy");
                    TestRunner.True(File.Exists(roaming), "original file untouched");
                });
            });

            runner.Run("STORE-016 write probe distinguishes directories from files", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    string file = Path.Combine(dir, "not-a-directory");
                    File.WriteAllText(file, "x", new UTF8Encoding(false));
                    TestRunner.True(SettingsPathResolver.CanWriteDirectory(dir), "temp directory writable");
                    TestRunner.False(SettingsPathResolver.CanWriteDirectory(file), "file is not a writable directory");
                });
            });

            runner.Run("STORE-017 version 1 settings migrate without losing values", delegate
            {
                WithTempDirectory(delegate(string dir)
                {
                    string path = Path.Combine(dir, "settings.json");
                    string json = "{\"version\":1,\"monthlySalary\":40000,\"monthlyWorkDays\":22," +
                        "\"workStartMinutes\":540,\"lunchStartMinutes\":720," +
                        "\"lunchEndMinutes\":780,\"workEndMinutes\":1080,\"alwaysOnTop\":false}";
                    File.WriteAllText(path, json, new UTF8Encoding(false));
                    SettingsLoadResult result = new SettingsStore(path).Load();
                    TestRunner.False(result.UsedDefaults, "migrated file loaded");
                    TestRunner.Equal(2, result.Settings.Version, "version upgraded");
                    TestRunner.Equal(40000m, result.Settings.MonthlySalary, "salary retained");
                    TestRunner.Equal(22m, result.Settings.MonthlyWorkDays, "days retained");
                    TestRunner.False(result.Settings.ActualWorkdaysEnabled, "actual mode defaults off");
                });
            });
        }

        private static Dictionary<string, bool> Writable(string firstPath, bool firstValue,
            string secondPath, bool secondValue)
        {
            Dictionary<string, bool> result = new Dictionary<string, bool>(
                StringComparer.OrdinalIgnoreCase);
            result[Path.GetFullPath(firstPath)] = firstValue;
            result[Path.GetFullPath(secondPath)] = secondValue;
            return result;
        }

        private static void WithTempDirectory(Action<string> action)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "_testdata_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                action(dir);
            }
            finally
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch
                {
                    // Test cleanup is best effort.
                }
            }
        }
    }

    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch
            {
                // Older consoles may not support setting the code page.
            }

            Console.WriteLine("Salary Monitor automated tests");
            Console.WriteLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            Console.WriteLine("Runtime: " + Environment.Version);
            Console.WriteLine();

            TestRunner runner = new TestRunner();
            CalculationTests.Run(runner);
            StorageTests.Run(runner);
            return runner.Finish();
        }
    }
}
