package com.salarymonitor.app.core;

import java.math.BigDecimal;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

public final class SettingsValidator {
    public static final BigDecimal MAX_MONTHLY_SALARY = new BigDecimal("9999999.99");
    public static final BigDecimal MAX_MONTHLY_WORK_DAYS = new BigDecimal("31");
    public static final int MINUTES_PER_DAY = 24 * 60;

    private SettingsValidator() {
    }

    public static Result validate(AppSettings settings) {
        Result result = new Result();
        if (settings == null) {
            result.add("Settings", "设置数据为空");
            return result;
        }

        if (settings.getMonthlySalary().signum() <= 0
                || settings.getMonthlySalary().compareTo(MAX_MONTHLY_SALARY) > 0) {
            result.add("MonthlySalary", "月薪必须大于 0 且不超过 9,999,999.99");
        }

        if (settings.getMonthlyWorkDays().signum() <= 0
                || settings.getMonthlyWorkDays().compareTo(MAX_MONTHLY_WORK_DAYS) > 0) {
            result.add("MonthlyWorkDays", "月计薪天数需大于 0 且不超过 31");
        }

        validateTime(result, "WorkStartMinutes", "上班时间", settings.getWorkStartMinutes());
        validateTime(result, "LunchStartMinutes", "午休开始时间", settings.getLunchStartMinutes());
        validateTime(result, "LunchEndMinutes", "午休结束时间", settings.getLunchEndMinutes());
        validateTime(result, "WorkEndMinutes", "下班时间", settings.getWorkEndMinutes());

        if (settings.getWorkStartMinutes() >= settings.getLunchStartMinutes()) {
            result.add("LunchStartMinutes", "午休开始时间必须晚于上班时间");
        }
        if (settings.getLunchStartMinutes() >= settings.getLunchEndMinutes()) {
            result.add("LunchEndMinutes", "午休结束时间必须晚于午休开始时间");
        }
        if (settings.getLunchEndMinutes() >= settings.getWorkEndMinutes()) {
            result.add("WorkEndMinutes", "下班时间必须晚于午休结束时间");
        }
        return result;
    }

    private static void validateTime(Result result, String field, String label, int minutes) {
        if (minutes < 0 || minutes >= MINUTES_PER_DAY) {
            result.add(field, label + "必须是 00:00 到 23:59 之间的时间");
        }
    }

    public static final class Issue {
        private final String field;
        private final String message;

        Issue(String field, String message) {
            this.field = field == null ? "" : field;
            this.message = message == null ? "" : message;
        }

        public String getField() {
            return field;
        }

        public String getMessage() {
            return message;
        }
    }

    public static final class Result {
        private final List<Issue> issues = new ArrayList<>();

        public boolean isValid() {
            return issues.isEmpty();
        }

        public void add(String field, String message) {
            issues.add(new Issue(field, message));
        }

        public List<Issue> getIssues() {
            return Collections.unmodifiableList(issues);
        }

        public String firstMessage() {
            return issues.isEmpty() ? "" : issues.get(0).getMessage();
        }

        public String firstField() {
            return issues.isEmpty() ? "" : issues.get(0).getField();
        }
    }
}
