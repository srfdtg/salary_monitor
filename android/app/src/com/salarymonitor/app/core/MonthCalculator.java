package com.salarymonitor.app.core;

import java.math.BigDecimal;
import java.math.MathContext;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.YearMonth;

/**
 * Month-to-date view calculated from the current settings and the bundled
 * calendar. v1.3 intentionally does not store a daily ledger.
 */
public final class MonthCalculator {
    private static final MathContext MC = MathContext.DECIMAL128;

    private MonthCalculator() {
    }

    public static MonthSnapshot calculate(AppSettings settings, LocalDateTime now) {
        if (settings == null) {
            throw new IllegalArgumentException("settings == null");
        }
        if (now == null) {
            throw new IllegalArgumentException("now == null");
        }

        PaySnapshot today = PayCalculator.calculate(settings, now);
        boolean actual = settings.isActualWorkdaysEnabled();
        int totalWorkdays = countWorkdays(now.getYear(), now.getMonthValue(), actual);

        int completedFull = 0;
        LocalDate monthStart = LocalDate.of(now.getYear(), now.getMonthValue(), 1);
        for (LocalDate day = monthStart; day.isBefore(now.toLocalDate()); day = day.plusDays(1)) {
            if (isWorkday(day, actual)) {
                completedFull++;
            }
        }

        BigDecimal todayFraction = BigDecimal.ZERO;
        if (today.getTotalPaidMinutes() > 0 && today.getPhase() != DayPhase.REST_DAY) {
            todayFraction = BigDecimal.valueOf(today.getCompletedPaidMinutes())
                    .divide(BigDecimal.valueOf(today.getTotalPaidMinutes()), MC);
        }

        BigDecimal completedWorkdays = BigDecimal.valueOf(completedFull).add(todayFraction);
        if (completedWorkdays.signum() < 0) {
            completedWorkdays = BigDecimal.ZERO;
        }
        if (completedWorkdays.compareTo(BigDecimal.valueOf(totalWorkdays)) > 0) {
            completedWorkdays = BigDecimal.valueOf(totalWorkdays);
        }

        BigDecimal monthEarned = today.getDailyWage().multiply(completedWorkdays, MC);
        if (monthEarned.signum() < 0) {
            monthEarned = BigDecimal.ZERO;
        }

        double progress = 0.0;
        if (totalWorkdays > 0) {
            progress = completedWorkdays.doubleValue() / (double) totalWorkdays;
        }
        progress = Math.max(0.0, Math.min(1.0, progress));

        int payday = settings.getPayday();
        LocalDate nextPayday = null;
        int daysUntil = 0;
        boolean isToday = false;
        if (payday >= 1 && payday <= 31) {
            LocalDate effectiveThisMonth = paydayInMonth(now.getYear(), now.getMonthValue(), payday);
            if (!now.toLocalDate().isAfter(effectiveThisMonth)) {
                nextPayday = effectiveThisMonth;
            } else {
                YearMonth nextMonth = YearMonth.from(now).plusMonths(1);
                nextPayday = paydayInMonth(nextMonth.getYear(), nextMonth.getMonthValue(), payday);
            }
            daysUntil = (int) (nextPayday.toEpochDay() - now.toLocalDate().toEpochDay());
            if (daysUntil < 0) {
                daysUntil = 0;
            }
            isToday = now.toLocalDate().equals(effectiveThisMonth);
        }

        return new MonthSnapshot(
                now,
                now.getYear(),
                now.getMonthValue(),
                actual,
                actual && WorkdayCalendar.hasData(now.getYear()),
                totalWorkdays,
                completedFull,
                completedWorkdays,
                today.getCompletedPaidMinutes(),
                today.getTotalPaidMinutes(),
                today.getDailyWage(),
                monthEarned,
                progress,
                payday,
                nextPayday,
                daysUntil,
                isToday);
    }

    public static int countWorkdays(int year, int month, boolean actual) {
        YearMonth yearMonth = YearMonth.of(year, month);
        int count = 0;
        for (int day = 1; day <= yearMonth.lengthOfMonth(); day++) {
            if (isWorkday(LocalDate.of(year, month, day), actual)) {
                count++;
            }
        }
        return count;
    }

    public static boolean isWorkday(LocalDate date, boolean actual) {
        if (actual) {
            return WorkdayCalendar.isWorkday(date);
        }
        return date.getDayOfWeek() != java.time.DayOfWeek.SATURDAY
                && date.getDayOfWeek() != java.time.DayOfWeek.SUNDAY;
    }

    public static LocalDate paydayInMonth(int year, int month, int payday) {
        YearMonth yearMonth = YearMonth.of(year, month);
        int effective = Math.max(1, Math.min(yearMonth.lengthOfMonth(), payday));
        return LocalDate.of(year, month, effective);
    }
}
