using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SalaryMonitor.Core
{
    public sealed class ValidationIssue
    {
        public string Field { get; private set; }
        public string Message { get; private set; }

        public ValidationIssue(string field, string message)
        {
            this.Field = field ?? string.Empty;
            this.Message = message ?? string.Empty;
        }
    }

    public sealed class ValidationResult
    {
        private readonly List<ValidationIssue> _issues;

        public ValidationResult()
        {
            _issues = new List<ValidationIssue>();
        }

        public bool IsValid
        {
            get { return _issues.Count == 0; }
        }

        public ReadOnlyCollection<ValidationIssue> Issues
        {
            get { return _issues.AsReadOnly(); }
        }

        public void Add(string field, string message)
        {
            _issues.Add(new ValidationIssue(field, message));
        }

        public string FirstMessage
        {
            get
            {
                if (_issues.Count == 0)
                {
                    return string.Empty;
                }
                return _issues[0].Message;
            }
        }
    }

    public static class SettingsValidator
    {
        public const decimal MaxMonthlySalary = 9999999.99m;
        public const decimal MaxMonthlyWorkDays = 31m;
        public const int MinutesPerDay = 24 * 60;

        public static ValidationResult Validate(AppSettings settings)
        {
            ValidationResult result = new ValidationResult();
            if (settings == null)
            {
                result.Add("Settings", "设置数据为空");
                return result;
            }

            if (settings.MonthlySalary <= 0m || settings.MonthlySalary > MaxMonthlySalary)
            {
                result.Add("MonthlySalary", "月薪必须大于 0 且不超过 9,999,999.99");
            }

            if (settings.MonthlyWorkDays <= 0m || settings.MonthlyWorkDays > MaxMonthlyWorkDays)
            {
                result.Add("MonthlyWorkDays", "月计薪天数需大于 0 且不超过 31");
            }

            ValidateTimeField(result, "WorkStartMinutes", "上班时间", settings.WorkStartMinutes);
            ValidateTimeField(result, "LunchStartMinutes", "午休开始时间", settings.LunchStartMinutes);
            ValidateTimeField(result, "LunchEndMinutes", "午休结束时间", settings.LunchEndMinutes);
            ValidateTimeField(result, "WorkEndMinutes", "下班时间", settings.WorkEndMinutes);

            if (settings.WorkStartMinutes >= settings.LunchStartMinutes)
            {
                result.Add("LunchStartMinutes", "午休开始时间必须晚于上班时间");
            }

            if (settings.LunchStartMinutes >= settings.LunchEndMinutes)
            {
                result.Add("LunchEndMinutes", "午休结束时间必须晚于午休开始时间");
            }

            if (settings.LunchEndMinutes >= settings.WorkEndMinutes)
            {
                result.Add("WorkEndMinutes", "下班时间必须晚于午休结束时间");
            }

            return result;
        }

        private static void ValidateTimeField(ValidationResult result, string field,
            string label, int minutes)
        {
            if (minutes < 0 || minutes >= MinutesPerDay)
            {
                result.Add(field, label + "必须是 00:00 到 23:59 之间的时间");
            }
        }
    }
}
