package com.salarymonitor.app.core;

import java.time.DayOfWeek;
import java.time.LocalDate;
import java.time.YearMonth;

/**
 * Chinese mainland public-holiday and make-up workday table for 2026 and 2027.
 *
 * Sources:
 * - 国办发明电〔2025〕7号 (2026 arrangement)
 * - 国办发明电〔2026〕10号 (2027 arrangement)
 *
 * A normal week is Monday-Friday. Official holiday ranges override weekdays to
 * rest days; official make-up dates override weekends to workdays. Dates outside
 * 2026-2027 fall back to Monday-Friday because no official table is bundled.
 */
public final class WorkdayCalendar {
    public static final int DATA_START_YEAR = 2026;
    public static final int DATA_END_YEAR = 2027;

    private static final LocalDate[][] HOLIDAY_RANGES = {
            {date(2026, 1, 1), date(2026, 1, 3)},
            {date(2026, 2, 15), date(2026, 2, 23)},
            {date(2026, 4, 4), date(2026, 4, 6)},
            {date(2026, 5, 1), date(2026, 5, 5)},
            {date(2026, 6, 19), date(2026, 6, 21)},
            {date(2026, 9, 25), date(2026, 9, 27)},
            {date(2026, 10, 1), date(2026, 10, 7)},
            {date(2027, 1, 1), date(2027, 1, 3)},
            {date(2027, 2, 5), date(2027, 2, 13)},
            {date(2027, 4, 3), date(2027, 4, 5)},
            {date(2027, 5, 1), date(2027, 5, 5)},
            {date(2027, 6, 9), date(2027, 6, 9)},
            {date(2027, 9, 15), date(2027, 9, 15)},
            {date(2027, 10, 1), date(2027, 10, 7)}
    };

    private static final LocalDate[] MAKEUP_DAYS = {
            date(2026, 1, 4),
            date(2026, 2, 14),
            date(2026, 2, 28),
            date(2026, 5, 9),
            date(2026, 9, 20),
            date(2026, 10, 10),
            date(2027, 1, 31),
            date(2027, 2, 14),
            date(2027, 5, 8),
            date(2027, 9, 26),
            date(2027, 10, 9)
    };

    private WorkdayCalendar() {
    }

    public static boolean hasData(int year) {
        return year >= DATA_START_YEAR && year <= DATA_END_YEAR;
    }

    public static DayKind dayKind(LocalDate date) {
        if (date == null) {
            throw new IllegalArgumentException("date == null");
        }
        if (isMakeupDay(date)) {
            return DayKind.MAKEUP_WORKDAY;
        }
        if (isHoliday(date)) {
            return DayKind.HOLIDAY;
        }
        DayOfWeek day = date.getDayOfWeek();
        if (day == DayOfWeek.SATURDAY || day == DayOfWeek.SUNDAY) {
            return DayKind.WEEKEND;
        }
        return DayKind.WORKDAY;
    }

    public static boolean isWorkday(LocalDate date) {
        return dayKind(date).isWorkday();
    }

    public static int workdaysInMonth(int year, int month) {
        YearMonth yearMonth = YearMonth.of(year, month);
        int count = 0;
        for (int day = 1; day <= yearMonth.lengthOfMonth(); day++) {
            if (isWorkday(LocalDate.of(year, month, day))) {
                count++;
            }
        }
        return count;
    }

    public static boolean isHoliday(LocalDate date) {
        if (date == null) {
            return false;
        }
        for (LocalDate[] range : HOLIDAY_RANGES) {
            if (!date.isBefore(range[0]) && !date.isAfter(range[1])) {
                return true;
            }
        }
        return false;
    }

    public static boolean isMakeupDay(LocalDate date) {
        if (date == null) {
            return false;
        }
        for (LocalDate makeup : MAKEUP_DAYS) {
            if (makeup.equals(date)) {
                return true;
            }
        }
        return false;
    }

    private static LocalDate date(int year, int month, int day) {
        return LocalDate.of(year, month, day);
    }
}
