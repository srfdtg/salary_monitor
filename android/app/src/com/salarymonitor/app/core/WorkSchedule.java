package com.salarymonitor.app.core;

import java.time.DayOfWeek;
import java.time.Duration;
import java.time.LocalDateTime;

/**
 * A single-day schedule with one unpaid lunch break. Compensation is counted in
 * completed minutes: 09:30:59 is still zero minutes, 09:31:00 is one minute.
 */
public final class WorkSchedule {
    private final int workStartMinutes;
    private final int lunchStartMinutes;
    private final int lunchEndMinutes;
    private final int workEndMinutes;

    public WorkSchedule(int workStartMinutes, int lunchStartMinutes,
                        int lunchEndMinutes, int workEndMinutes) {
        this.workStartMinutes = workStartMinutes;
        this.lunchStartMinutes = lunchStartMinutes;
        this.lunchEndMinutes = lunchEndMinutes;
        this.workEndMinutes = workEndMinutes;
    }

    public static WorkSchedule fromSettings(AppSettings settings) {
        if (settings == null) {
            throw new IllegalArgumentException("settings == null");
        }
        return new WorkSchedule(
                settings.getWorkStartMinutes(),
                settings.getLunchStartMinutes(),
                settings.getLunchEndMinutes(),
                settings.getWorkEndMinutes());
    }

    public static boolean isWeekend(LocalDateTime now) {
        DayOfWeek day = now.getDayOfWeek();
        return day == DayOfWeek.SATURDAY || day == DayOfWeek.SUNDAY;
    }

    public int getWorkStartMinutes() {
        return workStartMinutes;
    }

    public int getLunchStartMinutes() {
        return lunchStartMinutes;
    }

    public int getLunchEndMinutes() {
        return lunchEndMinutes;
    }

    public int getWorkEndMinutes() {
        return workEndMinutes;
    }

    public int getMorningMinutes() {
        return Math.max(0, lunchStartMinutes - workStartMinutes);
    }

    public int getAfternoonMinutes() {
        return Math.max(0, workEndMinutes - lunchEndMinutes);
    }

    public int getTotalPaidMinutes() {
        return getMorningMinutes() + getAfternoonMinutes();
    }

    public LocalDateTime getWorkStart(LocalDateTime date) {
        return atMinutes(date, workStartMinutes);
    }

    public LocalDateTime getLunchStart(LocalDateTime date) {
        return atMinutes(date, lunchStartMinutes);
    }

    public LocalDateTime getLunchEnd(LocalDateTime date) {
        return atMinutes(date, lunchEndMinutes);
    }

    public LocalDateTime getWorkEnd(LocalDateTime date) {
        return atMinutes(date, workEndMinutes);
    }

    private static LocalDateTime atMinutes(LocalDateTime date, int minutes) {
        return date.toLocalDate().atStartOfDay().plusMinutes(minutes);
    }

    public DayPhase getPhase(LocalDateTime now) {
        return getPhase(now, !isWeekend(now));
    }

    public DayPhase getPhase(LocalDateTime now, boolean workday) {
        if (!workday) {
            return DayPhase.REST_DAY;
        }

        LocalDateTime start = getWorkStart(now);
        LocalDateTime lunchStart = getLunchStart(now);
        LocalDateTime lunchEnd = getLunchEnd(now);
        LocalDateTime end = getWorkEnd(now);

        if (now.isBefore(start)) {
            return DayPhase.BEFORE_WORK;
        }
        if (now.isBefore(lunchStart)) {
            return DayPhase.MORNING_WORK;
        }
        if (now.isBefore(lunchEnd)) {
            return DayPhase.LUNCH_BREAK;
        }
        if (now.isBefore(end)) {
            return DayPhase.AFTERNOON_WORK;
        }
        return DayPhase.AFTER_WORK;
    }

    public int getCompletedPaidMinutes(LocalDateTime now) {
        return getCompletedPaidMinutes(now, !isWeekend(now));
    }

    public int getCompletedPaidMinutes(LocalDateTime now, boolean workday) {
        if (!workday) {
            return 0;
        }

        LocalDateTime start = getWorkStart(now);
        LocalDateTime lunchStart = getLunchStart(now);
        LocalDateTime lunchEnd = getLunchEnd(now);
        LocalDateTime end = getWorkEnd(now);

        if (now.isBefore(start)) {
            return 0;
        }
        if (now.isBefore(lunchStart)) {
            return Math.min(getMorningMinutes(), completedMinutes(start, now));
        }
        if (now.isBefore(lunchEnd)) {
            return getMorningMinutes();
        }
        if (now.isBefore(end)) {
            return getMorningMinutes()
                    + Math.min(getAfternoonMinutes(), completedMinutes(lunchEnd, now));
        }
        return getTotalPaidMinutes();
    }

    private static int completedMinutes(LocalDateTime from, LocalDateTime now) {
        if (!now.isAfter(from)) {
            return 0;
        }
        long minutes = Duration.between(from, now).toMinutes();
        if (minutes > Integer.MAX_VALUE) {
            return Integer.MAX_VALUE;
        }
        return (int) minutes;
    }
}
