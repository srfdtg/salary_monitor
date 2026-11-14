using System;
using System.Runtime.Serialization;

namespace SalaryMonitor.Core
{
    /// <summary>
    /// Persisted salary and schedule configuration. Times are stored as minutes
    /// from local midnight so the JSON remains unambiguous and DST-safe enough
    /// for a single-day widget.
    /// </summary>
    [DataContract]
    public sealed class AppSettings
    {
        public const int CurrentVersion = 3;
        public const decimal DefaultMonthlySalary = 33500m;
        public const decimal DefaultMonthlyWorkDays = 21.75m;
        public const int DefaultWorkStartMinutes = 9 * 60 + 30;   // 09:30
        public const int DefaultLunchStartMinutes = 12 * 60 + 30; // 12:30
        public const int DefaultLunchEndMinutes = 14 * 60;        // 14:00
        public const int DefaultWorkEndMinutes = 19 * 60;         // 19:00

        [DataMember(Name = "version", Order = 0)]
        public int Version { get; set; }

        [DataMember(Name = "monthlySalary", Order = 1)]
        public decimal MonthlySalary { get; set; }

        [DataMember(Name = "monthlyWorkDays", Order = 2)]
        public decimal MonthlyWorkDays { get; set; }

        [DataMember(Name = "workStartMinutes", Order = 3)]
        public int WorkStartMinutes { get; set; }

        [DataMember(Name = "lunchStartMinutes", Order = 4)]
        public int LunchStartMinutes { get; set; }

        [DataMember(Name = "lunchEndMinutes", Order = 5)]
        public int LunchEndMinutes { get; set; }

        [DataMember(Name = "workEndMinutes", Order = 6)]
        public int WorkEndMinutes { get; set; }

        [DataMember(Name = "alwaysOnTop", Order = 7)]
        public bool AlwaysOnTop { get; set; }

        [DataMember(Name = "actualWorkdaysEnabled", Order = 8)]
        public bool ActualWorkdaysEnabled { get; set; }

        [DataMember(Name = "amountsHidden", Order = 9)]
        public bool AmountsHidden { get; set; }

        public AppSettings()
        {
        }

        public static AppSettings CreateDefault()
        {
            AppSettings settings = new AppSettings();
            settings.Version = CurrentVersion;
            settings.MonthlySalary = DefaultMonthlySalary;
            settings.MonthlyWorkDays = DefaultMonthlyWorkDays;
            settings.WorkStartMinutes = DefaultWorkStartMinutes;
            settings.LunchStartMinutes = DefaultLunchStartMinutes;
            settings.LunchEndMinutes = DefaultLunchEndMinutes;
            settings.WorkEndMinutes = DefaultWorkEndMinutes;
            settings.AlwaysOnTop = true;
            settings.ActualWorkdaysEnabled = false;
            settings.AmountsHidden = false;
            return settings;
        }

        public AppSettings Clone()
        {
            AppSettings copy = new AppSettings();
            copy.Version = this.Version;
            copy.MonthlySalary = this.MonthlySalary;
            copy.MonthlyWorkDays = this.MonthlyWorkDays;
            copy.WorkStartMinutes = this.WorkStartMinutes;
            copy.LunchStartMinutes = this.LunchStartMinutes;
            copy.LunchEndMinutes = this.LunchEndMinutes;
            copy.WorkEndMinutes = this.WorkEndMinutes;
            copy.AlwaysOnTop = this.AlwaysOnTop;
            copy.ActualWorkdaysEnabled = this.ActualWorkdaysEnabled;
            copy.AmountsHidden = this.AmountsHidden;
            return copy;
        }
    }
}
