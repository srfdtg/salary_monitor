package com.salarymonitor.app.core;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.LocalDateTime;

/** Immutable month-to-date values rendered on the month page. */
public final class MonthSnapshot {
    private final LocalDateTime now;
    private final int year;
    private final int month;
    private final boolean actualWorkdays;
    private final boolean calendarDataAvailable;
    private final int totalWorkdays;
    private final int completedFullWorkdays;
    private final BigDecimal completedWorkdays;
    private final int todayCompletedPaidMinutes;
    private final int todayTotalPaidMinutes;
    private final BigDecimal dailyWage;
    private final BigDecimal monthEarned;
    private final double progress;
    private final int payday;
    private final LocalDate nextPayday;
    private final int daysUntilPayday;
    private final boolean paydayToday;

    public MonthSnapshot(LocalDateTime now, int year, int month,
                         boolean actualWorkdays, boolean calendarDataAvailable,
                         int totalWorkdays, int completedFullWorkdays,
                         BigDecimal completedWorkdays,
                         int todayCompletedPaidMinutes, int todayTotalPaidMinutes,
                         BigDecimal dailyWage, BigDecimal monthEarned, double progress,
                         int payday, LocalDate nextPayday, int daysUntilPayday,
                         boolean paydayToday) {
        this.now = now;
        this.year = year;
        this.month = month;
        this.actualWorkdays = actualWorkdays;
        this.calendarDataAvailable = calendarDataAvailable;
        this.totalWorkdays = Math.max(0, totalWorkdays);
        this.completedFullWorkdays = Math.max(0, completedFullWorkdays);
        this.completedWorkdays = completedWorkdays == null
                ? BigDecimal.ZERO : completedWorkdays.max(BigDecimal.ZERO);
        this.todayCompletedPaidMinutes = Math.max(0, todayCompletedPaidMinutes);
        this.todayTotalPaidMinutes = Math.max(0, todayTotalPaidMinutes);
        this.dailyWage = dailyWage == null ? BigDecimal.ZERO : dailyWage.max(BigDecimal.ZERO);
        this.monthEarned = monthEarned == null
                ? BigDecimal.ZERO : monthEarned.max(BigDecimal.ZERO);
        this.progress = Math.max(0.0, Math.min(1.0, progress));
        this.payday = payday;
        this.nextPayday = nextPayday;
        this.daysUntilPayday = Math.max(0, daysUntilPayday);
        this.paydayToday = paydayToday;
    }

    public boolean isPaydayConfigured() {
        return payday >= 1 && payday <= 31;
    }

    public LocalDateTime getNow() {
        return now;
    }

    public int getYear() {
        return year;
    }

    public int getMonth() {
        return month;
    }

    public boolean isActualWorkdays() {
        return actualWorkdays;
    }

    public boolean isCalendarDataAvailable() {
        return calendarDataAvailable;
    }

    public int getTotalWorkdays() {
        return totalWorkdays;
    }

    public int getCompletedFullWorkdays() {
        return completedFullWorkdays;
    }

    public BigDecimal getCompletedWorkdays() {
        return completedWorkdays;
    }

    public int getTodayCompletedPaidMinutes() {
        return todayCompletedPaidMinutes;
    }

    public int getTodayTotalPaidMinutes() {
        return todayTotalPaidMinutes;
    }

    public BigDecimal getDailyWage() {
        return dailyWage;
    }

    public BigDecimal getMonthEarned() {
        return monthEarned;
    }

    public double getProgress() {
        return progress;
    }

    public int getPayday() {
        return payday;
    }

    public LocalDate getNextPayday() {
        return nextPayday;
    }

    public int getDaysUntilPayday() {
        return daysUntilPayday;
    }

    public boolean isPaydayToday() {
        return paydayToday;
    }
}
