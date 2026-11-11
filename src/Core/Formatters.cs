using System;
using System.Globalization;

namespace SalaryMonitor.Core
{
    public static class Formatters
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static string MoneyValue(decimal amount)
        {
            decimal rounded = Decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
            return rounded.ToString("N2", Invariant);
        }

        public static string Money(decimal amount)
        {
            return "¥" + MoneyValue(amount);
        }

        public static string Countdown(TimeSpan span)
        {
            if (span < TimeSpan.Zero)
            {
                span = TimeSpan.Zero;
            }

            long totalSeconds = (long)Math.Ceiling(span.TotalSeconds);
            long hours = totalSeconds / 3600;
            long minutes = (totalSeconds % 3600) / 60;
            long seconds = totalSeconds % 60;
            return string.Format(Invariant, "{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
        }

        public static string Progress(double ratio)
        {
            if (ratio < 0.0) { ratio = 0.0; }
            if (ratio > 1.0) { ratio = 1.0; }

            double percent = Math.Round(ratio * 100.0, 1, MidpointRounding.AwayFromZero);
            double whole = Math.Round(percent, 0, MidpointRounding.AwayFromZero);
            if (Math.Abs(percent - whole) < 0.0001)
            {
                return ((int)whole).ToString(Invariant) + "%";
            }
            return percent.ToString("0.#", Invariant) + "%";
        }

        public static string WorkDuration(int minutes)
        {
            if (minutes < 0) { minutes = 0; }
            int hours = minutes / 60;
            int remainder = minutes % 60;
            if (hours <= 0)
            {
                return remainder.ToString(Invariant) + " 分钟";
            }
            return hours.ToString(Invariant) + " 小时 " + remainder.ToString(Invariant) + " 分";
        }

        public static string DateHeader(DateTime now)
        {
            string[] weekdays = new string[]
            {
                "周日", "周一", "周二", "周三", "周四", "周五", "周六"
            };
            int index = (int)now.DayOfWeek;
            if (index < 0 || index >= weekdays.Length)
            {
                index = 0;
            }
            return string.Format(Invariant, "{0}月{1}日 · {2}",
                now.Month, now.Day, weekdays[index]);
        }

        public static string TimeOfDay(DateTime now)
        {
            return now.ToString("HH:mm:ss", Invariant);
        }

        public static string MonthLabel(DateTime date)
        {
            return date.Year.ToString(Invariant) + "年" +
                date.Month.ToString(Invariant) + "月";
        }

        public static string DaysInput(decimal days)
        {
            return days.ToString("0.##", Invariant);
        }

        public static string ClockHHmm(int minutes)
        {
            int normalized = minutes;
            if (normalized < 0)
            {
                normalized = 0;
            }
            normalized = normalized % (24 * 60);
            return string.Format(Invariant, "{0:00}:{1:00}",
                normalized / 60, normalized % 60);
        }

        public static bool TryParseNumber(string text, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string cleaned = text.Trim().Replace(",", string.Empty).Replace("，", string.Empty);
            return Decimal.TryParse(cleaned, NumberStyles.Number, Invariant, out value);
        }

        public static bool TryParseTime(string text, out int minutesOfDay)
        {
            minutesOfDay = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.Trim();
            string[] parts = trimmed.Split(':');
            if (parts.Length != 2)
            {
                return false;
            }

            string hourPart = parts[0];
            string minutePart = parts[1];
            if (hourPart.Length < 1 || hourPart.Length > 2 || minutePart.Length != 2)
            {
                return false;
            }

            if (!IsAsciiDigits(hourPart) || !IsAsciiDigits(minutePart))
            {
                return false;
            }

            int hour;
            int minute;
            if (!Int32.TryParse(hourPart, NumberStyles.None, Invariant, out hour))
            {
                return false;
            }
            if (!Int32.TryParse(minutePart, NumberStyles.None, Invariant, out minute))
            {
                return false;
            }
            if (hour < 0 || hour > 23 || minute < 0 || minute > 59)
            {
                return false;
            }

            minutesOfDay = hour * 60 + minute;
            return true;
        }

        private static bool IsAsciiDigits(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }
            return value.Length > 0;
        }
    }
}
