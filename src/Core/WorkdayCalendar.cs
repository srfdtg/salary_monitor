using System;

namespace SalaryMonitor.Core
{
    /// <summary>
    /// Chinese mainland public-holiday and make-up workday table for 2026 and 2027.
    ///
    /// Sources:
    /// - 国办发明电〔2025〕7号 (2026 arrangement)
    /// - 国办发明电〔2026〕10号 (2027 arrangement)
    ///
    /// A normal week is Monday-Friday. Official holiday ranges override weekdays
    /// to rest days; official make-up dates override weekends to workdays. Dates
    /// outside 2026-2027 fall back to Monday-Friday.
    /// </summary>
    public static class WorkdayCalendar
    {
        public const int DataStartYear = 2026;
        public const int DataEndYear = 2027;

        private struct DateRange
        {
            public readonly DateTime Start;
            public readonly DateTime End;

            public DateRange(DateTime start, DateTime end)
            {
                this.Start = start.Date;
                this.End = end.Date;
            }

            public bool Contains(DateTime date)
            {
                return date >= Start && date <= End;
            }
        }

        private static readonly DateRange[] HolidayRanges =
        {
            new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 3)),
            new DateRange(new DateTime(2026, 2, 15), new DateTime(2026, 2, 23)),
            new DateRange(new DateTime(2026, 4, 4), new DateTime(2026, 4, 6)),
            new DateRange(new DateTime(2026, 5, 1), new DateTime(2026, 5, 5)),
            new DateRange(new DateTime(2026, 6, 19), new DateTime(2026, 6, 21)),
            new DateRange(new DateTime(2026, 9, 25), new DateTime(2026, 9, 27)),
            new DateRange(new DateTime(2026, 10, 1), new DateTime(2026, 10, 7)),
            new DateRange(new DateTime(2027, 1, 1), new DateTime(2027, 1, 3)),
            new DateRange(new DateTime(2027, 2, 5), new DateTime(2027, 2, 13)),
            new DateRange(new DateTime(2027, 4, 3), new DateTime(2027, 4, 5)),
            new DateRange(new DateTime(2027, 5, 1), new DateTime(2027, 5, 5)),
            new DateRange(new DateTime(2027, 6, 9), new DateTime(2027, 6, 9)),
            new DateRange(new DateTime(2027, 9, 15), new DateTime(2027, 9, 15)),
            new DateRange(new DateTime(2027, 10, 1), new DateTime(2027, 10, 7))
        };

        private static readonly DateTime[] MakeupDays =
        {
            new DateTime(2026, 1, 4),
            new DateTime(2026, 2, 14),
            new DateTime(2026, 2, 28),
            new DateTime(2026, 5, 9),
            new DateTime(2026, 9, 20),
            new DateTime(2026, 10, 10),
            new DateTime(2027, 1, 31),
            new DateTime(2027, 2, 14),
            new DateTime(2027, 5, 8),
            new DateTime(2027, 9, 26),
            new DateTime(2027, 10, 9)
        };

        public static bool HasData(int year)
        {
            return year >= DataStartYear && year <= DataEndYear;
        }

        public static DayKind GetDayKind(DateTime date)
        {
            DateTime value = date.Date;
            if (IsMakeupDay(value))
            {
                return DayKind.MakeupWorkday;
            }
            if (IsHoliday(value))
            {
                return DayKind.Holiday;
            }
            if (value.DayOfWeek == DayOfWeek.Saturday ||
                value.DayOfWeek == DayOfWeek.Sunday)
            {
                return DayKind.Weekend;
            }
            return DayKind.Workday;
        }

        public static bool IsWorkday(DateTime date)
        {
            return GetDayKind(date).IsWorkday();
        }

        public static int WorkdaysInMonth(int year, int month)
        {
            int count = 0;
            int days = DateTime.DaysInMonth(year, month);
            for (int day = 1; day <= days; day++)
            {
                if (IsWorkday(new DateTime(year, month, day)))
                {
                    count++;
                }
            }
            return count;
        }

        public static bool IsHoliday(DateTime date)
        {
            DateTime value = date.Date;
            for (int i = 0; i < HolidayRanges.Length; i++)
            {
                if (HolidayRanges[i].Contains(value))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsMakeupDay(DateTime date)
        {
            DateTime value = date.Date;
            for (int i = 0; i < MakeupDays.Length; i++)
            {
                if (MakeupDays[i] == value)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
