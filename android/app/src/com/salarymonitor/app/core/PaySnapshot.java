package com.salarymonitor.app.core;

import java.math.BigDecimal;
import java.time.Duration;
import java.time.LocalDateTime;

/** Immutable values rendered for one instant. */
public final class PaySnapshot {
    private final LocalDateTime now;
    private final DayPhase phase;
    private final DayKind dayKind;
    private final boolean actualWorkdays;
    private final int monthlyWorkdays;
    private final boolean calendarDataAvailable;
    private final LocalDateTime workStart;
    private final LocalDateTime workEnd;
    private final LocalDateTime lunchStart;
    private final LocalDateTime lunchEnd;
    private final int completedPaidMinutes;
    private final int totalPaidMinutes;
    private final int remainingPaidMinutes;
    private final BigDecimal dailyWage;
    private final BigDecimal hourlyWage;
    private final BigDecimal minuteWage;
    private final BigDecimal todayEarned;
    private final double progress;
    private final Duration mainCountdown;

    public PaySnapshot(LocalDateTime now, DayPhase phase,
                       DayKind dayKind, boolean actualWorkdays,
                       int monthlyWorkdays, boolean calendarDataAvailable,
                       LocalDateTime workStart, LocalDateTime workEnd,
                       LocalDateTime lunchStart, LocalDateTime lunchEnd,
                       int completedPaidMinutes, int totalPaidMinutes,
                       BigDecimal dailyWage, BigDecimal hourlyWage, BigDecimal minuteWage,
                       BigDecimal todayEarned, double progress, Duration mainCountdown) {
        this.now = now;
        this.phase = phase;
        this.dayKind = dayKind == null ? DayKind.WORKDAY : dayKind;
        this.actualWorkdays = actualWorkdays;
        this.monthlyWorkdays = Math.max(0, monthlyWorkdays);
        this.calendarDataAvailable = calendarDataAvailable;
        this.workStart = workStart;
        this.workEnd = workEnd;
        this.lunchStart = lunchStart;
        this.lunchEnd = lunchEnd;
        this.completedPaidMinutes = Math.max(0, completedPaidMinutes);
        this.totalPaidMinutes = Math.max(0, totalPaidMinutes);
        this.remainingPaidMinutes = Math.max(0,
                this.totalPaidMinutes - this.completedPaidMinutes);
        this.dailyWage = dailyWage == null ? BigDecimal.ZERO : dailyWage;
        this.hourlyWage = hourlyWage == null ? BigDecimal.ZERO : hourlyWage;
        this.minuteWage = minuteWage == null ? BigDecimal.ZERO : minuteWage;
        this.todayEarned = todayEarned == null ? BigDecimal.ZERO : todayEarned;
        this.progress = Math.max(0.0, Math.min(1.0, progress));
        this.mainCountdown = mainCountdown == null || mainCountdown.isNegative()
                ? Duration.ZERO : mainCountdown;
    }

    public LocalDateTime getNow() {
        return now;
    }

    public DayPhase getPhase() {
        return phase;
    }

    public DayKind getDayKind() {
        return dayKind;
    }

    public boolean isActualWorkdays() {
        return actualWorkdays;
    }

    public int getMonthlyWorkdays() {
        return monthlyWorkdays;
    }

    public boolean isCalendarDataAvailable() {
        return calendarDataAvailable;
    }

    public LocalDateTime getWorkStart() {
        return workStart;
    }

    public LocalDateTime getWorkEnd() {
        return workEnd;
    }

    public LocalDateTime getLunchStart() {
        return lunchStart;
    }

    public LocalDateTime getLunchEnd() {
        return lunchEnd;
    }

    public int getCompletedPaidMinutes() {
        return completedPaidMinutes;
    }

    public int getTotalPaidMinutes() {
        return totalPaidMinutes;
    }

    public int getRemainingPaidMinutes() {
        return remainingPaidMinutes;
    }

    public BigDecimal getDailyWage() {
        return dailyWage;
    }

    public BigDecimal getHourlyWage() {
        return hourlyWage;
    }

    public BigDecimal getMinuteWage() {
        return minuteWage;
    }

    public BigDecimal getTodayEarned() {
        return todayEarned;
    }

    public double getProgress() {
        return progress;
    }

    public Duration getMainCountdown() {
        return mainCountdown;
    }
}
