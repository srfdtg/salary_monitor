package com.salarymonitor.app.core;

import java.math.BigDecimal;
import java.math.MathContext;
import java.time.Duration;
import java.time.LocalDateTime;

public final class PayCalculator {
    private static final MathContext MC = MathContext.DECIMAL128;

    private PayCalculator() {
    }

    public static PaySnapshot calculate(AppSettings settings, LocalDateTime now) {
        if (settings == null) {
            throw new IllegalArgumentException("settings == null");
        }
        if (now == null) {
            throw new IllegalArgumentException("now == null");
        }

        WorkSchedule schedule = WorkSchedule.fromSettings(settings);
        boolean actualWorkdays = settings.isActualWorkdaysEnabled();
        DayKind dayKind = actualWorkdays
                ? WorkdayCalendar.dayKind(now.toLocalDate())
                : (WorkSchedule.isWeekend(now) ? DayKind.WEEKEND : DayKind.WORKDAY);
        boolean workday = dayKind.isWorkday();
        int monthlyWorkdays = actualWorkdays
                ? WorkdayCalendar.workdaysInMonth(now.getYear(), now.getMonthValue())
                : 0;

        DayPhase phase = schedule.getPhase(now, workday);
        int total = schedule.getTotalPaidMinutes();

        BigDecimal wageDays = actualWorkdays
                ? BigDecimal.valueOf(monthlyWorkdays)
                : settings.getMonthlyWorkDays();
        BigDecimal dailyWage = BigDecimal.ZERO;
        if (wageDays.signum() > 0) {
            dailyWage = settings.getMonthlySalary().divide(wageDays, MC);
        }

        BigDecimal minuteWage = total > 0
                ? dailyWage.divide(BigDecimal.valueOf(total), MC)
                : BigDecimal.ZERO;
        BigDecimal hourlyWage = minuteWage.multiply(BigDecimal.valueOf(60), MC);

        int completed = schedule.getCompletedPaidMinutes(now, workday);
        completed = Math.max(0, Math.min(total, completed));

        BigDecimal earned = BigDecimal.ZERO;
        if (total > 0 && workday) {
            earned = dailyWage.multiply(BigDecimal.valueOf(completed), MC)
                    .divide(BigDecimal.valueOf(total), MC);
            if (earned.signum() < 0) {
                earned = BigDecimal.ZERO;
            }
            if (earned.compareTo(dailyWage) > 0) {
                earned = dailyWage;
            }
        }

        double progress = total > 0 ? (double) completed / (double) total : 0.0;

        LocalDateTime target;
        Duration countdown;
        if (phase == DayPhase.BEFORE_WORK) {
            target = schedule.getWorkStart(now);
            countdown = Duration.between(now, target);
        } else if (phase == DayPhase.REST_DAY) {
            target = schedule.getWorkEnd(now);
            countdown = Duration.ZERO;
        } else {
            target = schedule.getWorkEnd(now);
            countdown = Duration.between(now, target);
            if (countdown.isNegative()) {
                countdown = Duration.ZERO;
            }
        }

        return new PaySnapshot(
                now,
                phase,
                dayKind,
                actualWorkdays,
                monthlyWorkdays,
                WorkdayCalendar.hasData(now.getYear()),
                schedule.getWorkStart(now),
                schedule.getWorkEnd(now),
                schedule.getLunchStart(now),
                schedule.getLunchEnd(now),
                completed,
                total,
                dailyWage,
                hourlyWage,
                minuteWage,
                earned,
                progress,
                countdown);
    }
}
