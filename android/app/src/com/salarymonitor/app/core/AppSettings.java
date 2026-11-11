package com.salarymonitor.app.core;

import java.math.BigDecimal;

/**
 * Persisted salary and schedule configuration. The defaults intentionally match
 * the Windows build: 33,500 CNY / 21.75 days, 09:30-12:30 and 14:00-19:00.
 *
 * The optional actual-workday mode uses the bundled 2026-2027 holiday table and
 * divides the monthly salary by the number of workdays in the current month.
 */
public final class AppSettings {
    public static final int CURRENT_VERSION = 2;
    public static final BigDecimal DEFAULT_MONTHLY_SALARY = new BigDecimal("33500");
    public static final BigDecimal DEFAULT_MONTHLY_WORK_DAYS = new BigDecimal("21.75");
    public static final int DEFAULT_WORK_START_MINUTES = 9 * 60 + 30;
    public static final int DEFAULT_LUNCH_START_MINUTES = 12 * 60 + 30;
    public static final int DEFAULT_LUNCH_END_MINUTES = 14 * 60;
    public static final int DEFAULT_WORK_END_MINUTES = 19 * 60;

    private int version = CURRENT_VERSION;
    private BigDecimal monthlySalary = DEFAULT_MONTHLY_SALARY;
    private BigDecimal monthlyWorkDays = DEFAULT_MONTHLY_WORK_DAYS;
    private int workStartMinutes = DEFAULT_WORK_START_MINUTES;
    private int lunchStartMinutes = DEFAULT_LUNCH_START_MINUTES;
    private int lunchEndMinutes = DEFAULT_LUNCH_END_MINUTES;
    private int workEndMinutes = DEFAULT_WORK_END_MINUTES;
    private boolean keepScreenOn = true;
    private boolean actualWorkdaysEnabled = false;

    public AppSettings() {
    }

    public static AppSettings createDefault() {
        return new AppSettings();
    }

    public AppSettings copy() {
        AppSettings copy = new AppSettings();
        copy.version = version;
        copy.monthlySalary = monthlySalary;
        copy.monthlyWorkDays = monthlyWorkDays;
        copy.workStartMinutes = workStartMinutes;
        copy.lunchStartMinutes = lunchStartMinutes;
        copy.lunchEndMinutes = lunchEndMinutes;
        copy.workEndMinutes = workEndMinutes;
        copy.keepScreenOn = keepScreenOn;
        copy.actualWorkdaysEnabled = actualWorkdaysEnabled;
        return copy;
    }

    public int getVersion() {
        return version;
    }

    public void setVersion(int version) {
        this.version = version;
    }

    public BigDecimal getMonthlySalary() {
        return monthlySalary;
    }

    public void setMonthlySalary(BigDecimal monthlySalary) {
        this.monthlySalary = monthlySalary == null ? BigDecimal.ZERO : monthlySalary;
    }

    public BigDecimal getMonthlyWorkDays() {
        return monthlyWorkDays;
    }

    public void setMonthlyWorkDays(BigDecimal monthlyWorkDays) {
        this.monthlyWorkDays = monthlyWorkDays == null ? BigDecimal.ZERO : monthlyWorkDays;
    }

    public int getWorkStartMinutes() {
        return workStartMinutes;
    }

    public void setWorkStartMinutes(int workStartMinutes) {
        this.workStartMinutes = workStartMinutes;
    }

    public int getLunchStartMinutes() {
        return lunchStartMinutes;
    }

    public void setLunchStartMinutes(int lunchStartMinutes) {
        this.lunchStartMinutes = lunchStartMinutes;
    }

    public int getLunchEndMinutes() {
        return lunchEndMinutes;
    }

    public void setLunchEndMinutes(int lunchEndMinutes) {
        this.lunchEndMinutes = lunchEndMinutes;
    }

    public int getWorkEndMinutes() {
        return workEndMinutes;
    }

    public void setWorkEndMinutes(int workEndMinutes) {
        this.workEndMinutes = workEndMinutes;
    }

    public boolean isKeepScreenOn() {
        return keepScreenOn;
    }

    public void setKeepScreenOn(boolean keepScreenOn) {
        this.keepScreenOn = keepScreenOn;
    }

    public boolean isActualWorkdaysEnabled() {
        return actualWorkdaysEnabled;
    }

    public void setActualWorkdaysEnabled(boolean actualWorkdaysEnabled) {
        this.actualWorkdaysEnabled = actualWorkdaysEnabled;
    }
}
