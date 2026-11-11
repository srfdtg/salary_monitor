import com.salarymonitor.app.core.AppSettings;
import com.salarymonitor.app.core.DayKind;
import com.salarymonitor.app.core.DayPhase;
import com.salarymonitor.app.core.Formatters;
import com.salarymonitor.app.core.PayCalculator;
import com.salarymonitor.app.core.PaySnapshot;
import com.salarymonitor.app.core.PhaseText;
import com.salarymonitor.app.core.SettingsValidator;
import com.salarymonitor.app.core.WorkSchedule;
import com.salarymonitor.app.core.WorkdayCalendar;

import java.math.BigDecimal;
import java.time.Duration;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

public final class CoreTests {
    private static final LocalDate MONDAY = LocalDate.of(2026, 11, 9);
    private static final LocalDate FRIDAY = LocalDate.of(2026, 11, 6);
    private static final LocalDate SATURDAY = LocalDate.of(2026, 11, 7);
    private static final LocalDate SUNDAY = LocalDate.of(2026, 11, 8);

    private static int passed;
    private static int failed;
    private static final List<String> failures = new ArrayList<>();

    private interface Case {
        void run();
    }

    private static void test(String name, Case c) {
        try {
            c.run();
            passed++;
            System.out.println("[PASS] " + name);
        } catch (Throwable ex) {
            failed++;
            String message = name + ": " + ex.getMessage();
            failures.add(message);
            System.out.println("[FAIL] " + message);
            if (!(ex instanceof AssertionError)) {
                ex.printStackTrace(System.out);
            }
        }
    }

    private static void eq(Object expected, Object actual, String context) {
        if (!Objects.equals(expected, actual)) {
            throw new AssertionError(context + " expected <" + expected + "> but was <"
                    + actual + ">");
        }
    }

    private static void truth(boolean value, String context) {
        if (!value) {
            throw new AssertionError(context + " was false");
        }
    }

    private static void falsity(boolean value, String context) {
        if (value) {
            throw new AssertionError(context + " was true");
        }
    }

    private static void near(double expected, double actual, double tolerance, String context) {
        if (Math.abs(expected - actual) > tolerance) {
            throw new AssertionError(context + " expected near <" + expected
                    + "> but was <" + actual + ">");
        }
    }

    private static LocalDateTime at(LocalDate day, int hour, int minute, int second) {
        return day.atTime(hour, minute, second);
    }

    private static PaySnapshot snapshot(LocalDateTime now) {
        return PayCalculator.calculate(AppSettings.createDefault(), now);
    }

    private static PaySnapshot actualSnapshot(LocalDateTime now) {
        AppSettings settings = AppSettings.createDefault();
        settings.setActualWorkdaysEnabled(true);
        return PayCalculator.calculate(settings, now);
    }

    public static void main(String[] args) {
        calculations();
        formatting();
        validation();
        phaseLabels();
        actualWorkdays();

        System.out.println();
        System.out.println("------------------------------------------------------------");
        System.out.println("Automated tests: " + passed + " passed, " + failed + " failed.");
        if (failed > 0) {
            System.out.println("Failures:");
            for (String failure : failures) {
                System.out.println(" - " + failure);
            }
            System.out.println("RESULT: FAIL");
            System.exit(1);
        }
        System.out.println("RESULT: PASS");
    }

    private static void calculations() {
        test("CALC-001 default daily/minute/hourly rates and 480 minutes", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 9, 30, 0));
            eq(480, s.getTotalPaidMinutes(), "total paid minutes");
            eq("¥1,540.23", Formatters.money(s.getDailyWage()), "daily wage");
            eq("¥3.21", Formatters.money(s.getMinuteWage()), "minute wage");
            eq("¥192.53", Formatters.money(s.getHourlyWage()), "hourly wage");
        });

        test("CALC-002 09:00 before work shows countdown to start", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 9, 0, 0));
            eq(DayPhase.BEFORE_WORK, s.getPhase(), "phase");
            eq(0, s.getCompletedPaidMinutes(), "completed minutes");
            eq("¥0.00", Formatters.money(s.getTodayEarned()), "earned");
            eq(480, s.getRemainingPaidMinutes(), "remaining minutes");
            eq("00:30:00", Formatters.countdown(s.getMainCountdown()), "countdown");
            eq("0%", Formatters.progress(s.getProgress()), "progress");
        });

        test("CALC-003 09:29:59 before work countdown is one second", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 9, 29, 59));
            eq(DayPhase.BEFORE_WORK, s.getPhase(), "phase");
            eq("00:00:01", Formatters.countdown(s.getMainCountdown()), "countdown");
            eq("¥0.00", Formatters.money(s.getTodayEarned()), "earned");
        });

        test("CALC-004 09:30 starts work at zero pay", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 9, 30, 0));
            eq(DayPhase.MORNING_WORK, s.getPhase(), "phase");
            eq(0, s.getCompletedPaidMinutes(), "completed minutes");
            eq("¥0.00", Formatters.money(s.getTodayEarned()), "earned");
            eq("09:30:00", Formatters.countdown(s.getMainCountdown()), "countdown");
            eq(480, s.getRemainingPaidMinutes(), "remaining minutes");
        });

        test("CALC-005 09:30:59 still has not completed a paid minute", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 9, 30, 59));
            eq(DayPhase.MORNING_WORK, s.getPhase(), "phase");
            eq(0, s.getCompletedPaidMinutes(), "completed minutes");
            eq("¥0.00", Formatters.money(s.getTodayEarned()), "earned");
        });

        test("CALC-006 09:31 first paid minute", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 9, 31, 0));
            eq(1, s.getCompletedPaidMinutes(), "completed minutes");
            eq("¥3.21", Formatters.money(s.getTodayEarned()), "earned");
            eq(479, s.getRemainingPaidMinutes(), "remaining minutes");
            eq("09:29:00", Formatters.countdown(s.getMainCountdown()), "countdown");
            eq("0.2%", Formatters.progress(s.getProgress()), "progress");
        });

        test("CALC-007 09:32 second paid minute", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 9, 32, 0));
            eq(2, s.getCompletedPaidMinutes(), "completed minutes");
            eq("¥6.42", Formatters.money(s.getTodayEarned()), "earned");
        });

        test("CALC-008 12:30 lunch starts and freezes at three hours", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 12, 30, 0));
            eq(DayPhase.LUNCH_BREAK, s.getPhase(), "phase");
            eq(180, s.getCompletedPaidMinutes(), "completed minutes");
            eq("¥577.59", Formatters.money(s.getTodayEarned()), "earned");
            eq(300, s.getRemainingPaidMinutes(), "remaining minutes");
            eq("37.5%", Formatters.progress(s.getProgress()), "progress");
            eq("06:30:00", Formatters.countdown(s.getMainCountdown()), "countdown");
        });

        test("CALC-009 13:59:59 lunch keeps money frozen, countdown moves", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 13, 59, 59));
            eq(DayPhase.LUNCH_BREAK, s.getPhase(), "phase");
            eq(180, s.getCompletedPaidMinutes(), "completed minutes");
            eq("¥577.59", Formatters.money(s.getTodayEarned()), "earned");
            eq(300, s.getRemainingPaidMinutes(), "remaining minutes");
            eq("05:00:01", Formatters.countdown(s.getMainCountdown()), "countdown");
        });

        test("CALC-010 14:00 afternoon starts without extra pay", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 14, 0, 0));
            eq(DayPhase.AFTERNOON_WORK, s.getPhase(), "phase");
            eq(180, s.getCompletedPaidMinutes(), "completed minutes");
            eq("¥577.59", Formatters.money(s.getTodayEarned()), "earned");
            eq(300, s.getRemainingPaidMinutes(), "remaining minutes");
            eq("05:00:00", Formatters.countdown(s.getMainCountdown()), "countdown");
        });

        test("CALC-011 14:01 resumes pay, progress 37.7%", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 14, 1, 0));
            eq(DayPhase.AFTERNOON_WORK, s.getPhase(), "phase");
            eq(181, s.getCompletedPaidMinutes(), "completed minutes");
            eq("¥580.80", Formatters.money(s.getTodayEarned()), "earned");
            eq(299, s.getRemainingPaidMinutes(), "remaining minutes");
            eq("37.7%", Formatters.progress(s.getProgress()), "progress");
            eq("04:59:00", Formatters.countdown(s.getMainCountdown()), "countdown");
        });

        test("CALC-012 18:59:59 almost complete and one second left", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 18, 59, 59));
            eq(DayPhase.AFTERNOON_WORK, s.getPhase(), "phase");
            eq(479, s.getCompletedPaidMinutes(), "completed minutes");
            eq("¥1,537.02", Formatters.money(s.getTodayEarned()), "earned");
            eq(1, s.getRemainingPaidMinutes(), "remaining minutes");
            eq("00:00:01", Formatters.countdown(s.getMainCountdown()), "countdown");
        });

        test("CALC-013 19:00 full daily pay and complete state", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 19, 0, 0));
            eq(DayPhase.AFTER_WORK, s.getPhase(), "phase");
            eq(480, s.getCompletedPaidMinutes(), "completed minutes");
            eq("¥1,540.23", Formatters.money(s.getTodayEarned()), "earned");
            eq(0, s.getRemainingPaidMinutes(), "remaining minutes");
            eq("100%", Formatters.progress(s.getProgress()), "progress");
            eq("00:00:00", Formatters.countdown(s.getMainCountdown()), "countdown");
        });

        test("CALC-014 after work stays capped", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 23, 59, 59));
            eq(DayPhase.AFTER_WORK, s.getPhase(), "phase");
            eq("¥1,540.23", Formatters.money(s.getTodayEarned()), "earned");
            eq(480, s.getCompletedPaidMinutes(), "completed minutes");
            eq(0, s.getRemainingPaidMinutes(), "remaining minutes");
        });

        test("CALC-015 Saturday is rest day with zero pay", () -> {
            PaySnapshot s = snapshot(at(SATURDAY, 12, 0, 0));
            eq(DayPhase.REST_DAY, s.getPhase(), "phase");
            eq(0, s.getCompletedPaidMinutes(), "completed minutes");
            eq("¥0.00", Formatters.money(s.getTodayEarned()), "earned");
            eq("0%", Formatters.progress(s.getProgress()), "progress");
        });

        test("CALC-016 Sunday is rest day with zero pay", () -> {
            PaySnapshot s = snapshot(at(SUNDAY, 19, 30, 0));
            eq(DayPhase.REST_DAY, s.getPhase(), "phase");
            eq("¥0.00", Formatters.money(s.getTodayEarned()), "earned");
        });

        test("CALC-017 Friday still counts as a workday", () -> {
            PaySnapshot s = snapshot(at(FRIDAY, 10, 0, 0));
            eq(DayPhase.MORNING_WORK, s.getPhase(), "phase");
            eq(30, s.getCompletedPaidMinutes(), "completed minutes");
            eq("¥96.26", Formatters.money(s.getTodayEarned()), "earned");
        });

        test("CALC-018 16:28:35 matches the concept numbers", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 16, 28, 35));
            eq(DayPhase.AFTERNOON_WORK, s.getPhase(), "phase");
            eq(328, s.getCompletedPaidMinutes(), "completed minutes");
            eq("¥1,052.49", Formatters.money(s.getTodayEarned()), "earned");
            eq("68.3%", Formatters.progress(s.getProgress()), "progress");
            eq("02:31:25", Formatters.countdown(s.getMainCountdown()), "countdown");
            eq(152, s.getRemainingPaidMinutes(), "remaining minutes");
            eq("2 小时 32 分", Formatters.workDuration(s.getRemainingPaidMinutes()),
                    "remaining label");
        });

        test("CALC-019 midnight is before work", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 0, 0, 0));
            eq(DayPhase.BEFORE_WORK, s.getPhase(), "phase");
            eq("09:30:00", Formatters.countdown(s.getMainCountdown()), "countdown");
        });

        test("CALC-020 salary never leaves its bounds", () -> {
            LocalDateTime[] samples = {
                    at(MONDAY, 0, 0, 0), at(MONDAY, 9, 30, 0), at(MONDAY, 12, 30, 0),
                    at(MONDAY, 14, 0, 0), at(MONDAY, 19, 0, 0), at(MONDAY, 23, 59, 59),
                    at(SATURDAY, 10, 0, 0)
            };
            for (LocalDateTime sample : samples) {
                PaySnapshot s = snapshot(sample);
                truth(s.getTodayEarned().signum() >= 0, "earned >= 0 at " + sample);
                truth(s.getTodayEarned().compareTo(s.getDailyWage()) <= 0,
                        "earned <= daily at " + sample);
                truth(s.getProgress() >= 0.0 && s.getProgress() <= 1.0,
                        "progress bounds at " + sample);
                truth(s.getRemainingPaidMinutes() >= 0, "remaining >= 0 at " + sample);
            }
        });

        test("CALC-021 510-minute schedule uses its own minute rate", () -> {
            AppSettings settings = AppSettings.createDefault();
            settings.setWorkStartMinutes(9 * 60);
            settings.setLunchStartMinutes(12 * 60 + 30);
            settings.setLunchEndMinutes(14 * 60);
            settings.setWorkEndMinutes(19 * 60);
            PaySnapshot s = PayCalculator.calculate(settings, at(MONDAY, 12, 30, 0));
            eq(510, s.getTotalPaidMinutes(), "total paid minutes");
            eq("¥3.02", Formatters.money(s.getMinuteWage()), "minute wage");
            eq("¥181.20", Formatters.money(s.getHourlyWage()), "hourly wage");
            eq("¥1,540.23", Formatters.money(PayCalculator.calculate(settings,
                    at(MONDAY, 19, 0, 0)).getTodayEarned()), "end pay");
        });

        test("CALC-022 420-minute schedule uses its own minute rate", () -> {
            AppSettings settings = AppSettings.createDefault();
            settings.setWorkStartMinutes(9 * 60);
            settings.setLunchStartMinutes(12 * 60);
            settings.setLunchEndMinutes(13 * 60);
            settings.setWorkEndMinutes(17 * 60);
            PaySnapshot noon = PayCalculator.calculate(settings, at(MONDAY, 12, 0, 0));
            PaySnapshot end = PayCalculator.calculate(settings, at(MONDAY, 17, 0, 0));
            eq(420, noon.getTotalPaidMinutes(), "total paid minutes");
            eq("¥3.67", Formatters.money(noon.getMinuteWage()), "minute wage");
            eq(DayPhase.LUNCH_BREAK, noon.getPhase(), "noon phase");
            eq("¥1,540.23", Formatters.money(end.getTodayEarned()), "end pay");
            eq(DayPhase.AFTER_WORK, end.getPhase(), "end phase");
        });

        test("CALC-023 full-day calculation is internally exact", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 19, 0, 0));
            eq(0, s.getDailyWage().compareTo(s.getTodayEarned()), "today earned equals daily");
        });

        test("CALC-024 defaults match the requested schedule", () -> {
            AppSettings s = AppSettings.createDefault();
            eq(new BigDecimal("33500"), s.getMonthlySalary(), "salary");
            eq(new BigDecimal("21.75"), s.getMonthlyWorkDays(), "days");
            eq(570, s.getWorkStartMinutes(), "start");
            eq(750, s.getLunchStartMinutes(), "lunch start");
            eq(840, s.getLunchEndMinutes(), "lunch end");
            eq(1140, s.getWorkEndMinutes(), "end");
            truth(s.isKeepScreenOn(), "keep screen on default");
        });

        test("CALC-025 copy preserves all values", () -> {
            AppSettings s = AppSettings.createDefault();
            s.setMonthlySalary(new BigDecimal("40000"));
            s.setMonthlyWorkDays(new BigDecimal("20"));
            s.setWorkStartMinutes(480);
            s.setLunchStartMinutes(720);
            s.setLunchEndMinutes(780);
            s.setWorkEndMinutes(1080);
            s.setKeepScreenOn(false);
            AppSettings copy = s.copy();
            eq(s.getMonthlySalary(), copy.getMonthlySalary(), "salary");
            eq(s.getMonthlyWorkDays(), copy.getMonthlyWorkDays(), "days");
            eq(s.getWorkStartMinutes(), copy.getWorkStartMinutes(), "start");
            eq(s.getLunchStartMinutes(), copy.getLunchStartMinutes(), "lunch start");
            eq(s.getLunchEndMinutes(), copy.getLunchEndMinutes(), "lunch end");
            eq(s.getWorkEndMinutes(), copy.getWorkEndMinutes(), "end");
            eq(s.isKeepScreenOn(), copy.isKeepScreenOn(), "keep screen on");
        });

        test("CALC-026 weekend check is Saturday/Sunday only", () -> {
            truth(WorkSchedule.isWeekend(at(SATURDAY, 10, 0, 0)), "Saturday");
            truth(WorkSchedule.isWeekend(at(SUNDAY, 10, 0, 0)), "Sunday");
            falsity(WorkSchedule.isWeekend(at(MONDAY, 10, 0, 0)), "Monday");
            falsity(WorkSchedule.isWeekend(at(FRIDAY, 10, 0, 0)), "Friday");
        });
    }

    private static void formatting() {
        test("FMT-001 currency formatting round-half-away and grouping", () -> {
            eq("¥0.00", Formatters.money(BigDecimal.ZERO), "zero");
            eq("¥3.20", Formatters.money(new BigDecimal("3.2")), "one decimal");
            eq("¥1,540.23", Formatters.money(new BigDecimal("1540.229885")), "grouping");
            eq("¥1,000.00", Formatters.money(new BigDecimal("1000")), "thousands");
            eq("¥3.21", Formatters.money(new BigDecimal("3.205")), "half away from zero");
        });

        test("FMT-002 countdown formatting and ceiling", () -> {
            eq("00:00:00", Formatters.countdown(Duration.ZERO), "zero");
            eq("00:00:01", Formatters.countdown(Duration.ofMillis(200)), "ceiling");
            eq("00:59:59", Formatters.countdown(Duration.ofSeconds(59 * 60 + 59)), "minutes");
            eq("09:30:00", Formatters.countdown(Duration.ofMinutes(570)), "hours");
            eq("00:00:00", Formatters.countdown(Duration.ofSeconds(-1)), "negative");
        });

        test("FMT-003 progress formatting", () -> {
            eq("0%", Formatters.progress(0.0), "zero");
            eq("37.5%", Formatters.progress(0.375), "three eighths");
            eq("68.3%", Formatters.progress(328.0 / 480.0), "typical");
            eq("100%", Formatters.progress(1.0), "full");
        });

        test("FMT-004 duration formatting", () -> {
            eq("8 小时 0 分", Formatters.workDuration(480), "full day");
            eq("5 小时 0 分", Formatters.workDuration(300), "five hours");
            eq("2 小时 32 分", Formatters.workDuration(152), "two hours");
            eq("32 分钟", Formatters.workDuration(32), "minutes only");
            eq("0 分钟", Formatters.workDuration(0), "zero");
        });

        test("FMT-005 time-of-day helpers", () -> {
            LocalDateTime now = at(MONDAY, 16, 28, 35);
            eq("11月9日 · 周一", Formatters.dateHeader(now), "date header");
            eq("16:28:35", Formatters.timeOfDay(now), "clock");
            eq("09:30", Formatters.clockHHmm(570), "HHmm");
        });

        test("FMT-006 time-unit labels", () -> {
            eq("凌晨", Formatters.timeUnitLabel(0), "midnight");
            eq("上午", Formatters.timeUnitLabel(570), "morning");
            eq("中午", Formatters.timeUnitLabel(750), "noon");
            eq("下午", Formatters.timeUnitLabel(840), "afternoon");
            eq("晚上", Formatters.timeUnitLabel(1140), "evening");
        });

        test("FMT-007 input formatting", () -> {
            eq("33500.00", Formatters.amountInput(new BigDecimal("33500")), "amount");
            eq("21.75", Formatters.daysInput(new BigDecimal("21.75")), "days");
            eq("31", Formatters.daysInput(new BigDecimal("31.00")), "whole days");
        });

        test("PARSE-001 time input accepts H:mm and HH:mm", () -> {
            eq(Integer.valueOf(545), Formatters.parseTimeOfDay("9:05"), "single-digit hour");
            eq(Integer.valueOf(545), Formatters.parseTimeOfDay("09:05"), "two-digit hour");
            eq(Integer.valueOf(1140), Formatters.parseTimeOfDay("19:00"), "evening");
        });

        test("PARSE-002 invalid time formats are rejected", () -> {
            eq(null, Formatters.parseTimeOfDay("24:00"), "hour range");
            eq(null, Formatters.parseTimeOfDay("12:60"), "minute range");
            eq(null, Formatters.parseTimeOfDay("9:5"), "short minute");
            eq(null, Formatters.parseTimeOfDay("9"), "no colon");
            eq(null, Formatters.parseTimeOfDay(""), "blank");
            eq(null, Formatters.parseTimeOfDay("9:05x"), "suffix");
        });

        test("PARSE-003 money accepts grouping and decimal point", () -> {
            eq(new BigDecimal("33500.00"), Formatters.parseDecimal("33,500.00"), "grouped");
            eq(new BigDecimal("21.75"), Formatters.parseDecimal("21.75"), "days");
            eq(null, Formatters.parseDecimal(""), "blank");
            eq(null, Formatters.parseDecimal("abc"), "letters");
        });

        test("FMT-008 progress is clamped", () -> {
            eq("0%", Formatters.progress(-0.2), "negative");
            eq("100%", Formatters.progress(1.4), "over one");
        });

        test("FMT-009 money is clamped only by caller, formatting handles zero", () -> {
            eq("¥0.00", Formatters.money(null), "null amount");
        });
    }

    private static void validation() {
        test("VALID-001 defaults are valid", () -> {
            truth(SettingsValidator.validate(AppSettings.createDefault()).isValid(), "defaults");
        });

        test("VALID-002 salary bounds are validated", () -> {
            AppSettings s = AppSettings.createDefault();
            s.setMonthlySalary(BigDecimal.ZERO);
            falsity(SettingsValidator.validate(s).isValid(), "zero salary");
            s.setMonthlySalary(new BigDecimal("-1"));
            falsity(SettingsValidator.validate(s).isValid(), "negative salary");
            s.setMonthlySalary(SettingsValidator.MAX_MONTHLY_SALARY.add(BigDecimal.ONE));
            falsity(SettingsValidator.validate(s).isValid(), "too large salary");
        });

        test("VALID-003 work-day bounds are validated", () -> {
            AppSettings s = AppSettings.createDefault();
            s.setMonthlyWorkDays(BigDecimal.ZERO);
            falsity(SettingsValidator.validate(s).isValid(), "zero days");
            s.setMonthlyWorkDays(new BigDecimal("31.01"));
            falsity(SettingsValidator.validate(s).isValid(), "over 31 days");
            s.setMonthlyWorkDays(new BigDecimal("31"));
            truth(SettingsValidator.validate(s).isValid(), "31 days accepted");
        });

        test("VALID-004 time ranges are validated", () -> {
            AppSettings s = AppSettings.createDefault();
            s.setWorkStartMinutes(-1);
            falsity(SettingsValidator.validate(s).isValid(), "negative time");
            s = AppSettings.createDefault();
            s.setWorkEndMinutes(1440);
            falsity(SettingsValidator.validate(s).isValid(), "time 24:00");
        });

        test("VALID-005 time ordering is validated", () -> {
            AppSettings s = AppSettings.createDefault();
            s.setWorkStartMinutes(s.getLunchStartMinutes());
            falsity(SettingsValidator.validate(s).isValid(), "start equals lunch");
            s = AppSettings.createDefault();
            s.setLunchStartMinutes(s.getLunchEndMinutes());
            falsity(SettingsValidator.validate(s).isValid(), "lunch start equals end");
            s = AppSettings.createDefault();
            s.setLunchEndMinutes(s.getWorkEndMinutes());
            falsity(SettingsValidator.validate(s).isValid(), "lunch end equals work end");
        });

        test("VALID-006 early-morning schedule is allowed", () -> {
            AppSettings s = AppSettings.createDefault();
            s.setWorkStartMinutes(0);
            s.setLunchStartMinutes(4 * 60);
            s.setLunchEndMinutes(5 * 60);
            s.setWorkEndMinutes(9 * 60);
            truth(SettingsValidator.validate(s).isValid(), "early schedule");
            eq(480, WorkSchedule.fromSettings(s).getTotalPaidMinutes(), "paid minutes");
        });

        test("VALID-007 overnight schedule is rejected by same-day ordering", () -> {
            AppSettings s = AppSettings.createDefault();
            s.setWorkStartMinutes(22 * 60);
            s.setLunchStartMinutes(23 * 60);
            s.setLunchEndMinutes(23 * 60 + 30);
            s.setWorkEndMinutes(2 * 60);
            falsity(SettingsValidator.validate(s).isValid(), "overnight");
        });

        test("VALID-008 null settings is invalid", () -> {
            falsity(SettingsValidator.validate(null).isValid(), "null");
        });

        test("VALID-009 first issue is actionable", () -> {
            AppSettings s = AppSettings.createDefault();
            s.setMonthlySalary(BigDecimal.ZERO);
            SettingsValidator.Result result = SettingsValidator.validate(s);
            eq("MonthlySalary", result.firstField(), "field");
            truth(result.firstMessage().contains("月薪"), "message");
        });
    }

    private static void phaseLabels() {
        test("UI-001 phase status labels", () -> {
            eq("周末休息", PhaseText.status(DayPhase.REST_DAY), "rest");
            eq("未上班", PhaseText.status(DayPhase.BEFORE_WORK), "before");
            eq("上午工作中", PhaseText.status(DayPhase.MORNING_WORK), "morning");
            eq("午休中", PhaseText.status(DayPhase.LUNCH_BREAK), "lunch");
            eq("下午工作中", PhaseText.status(DayPhase.AFTERNOON_WORK), "afternoon");
            eq("今日已下班", PhaseText.status(DayPhase.AFTER_WORK), "after");
        });

        test("UI-002 countdown captions", () -> {
            eq("今日状态", PhaseText.countdownCaption(DayPhase.REST_DAY), "rest");
            eq("距上班", PhaseText.countdownCaption(DayPhase.BEFORE_WORK), "before");
            eq("距离下班", PhaseText.countdownCaption(DayPhase.MORNING_WORK), "morning");
            eq("距离下班", PhaseText.countdownCaption(DayPhase.AFTER_WORK), "after");
        });

        test("UI-003 update notes", () -> {
            eq("周末不计薪", PhaseText.updateNote(DayPhase.REST_DAY), "rest");
            eq("每满 1 分钟更新工资", PhaseText.updateNote(DayPhase.BEFORE_WORK), "before");
            eq("午休中 · 金额暂停", PhaseText.updateNote(DayPhase.LUNCH_BREAK), "lunch");
            eq("今日工资已封顶", PhaseText.updateNote(DayPhase.AFTER_WORK), "after");
        });

        test("UI-004 completed minutes at boundary seconds", () -> {
            eq(180, snapshot(at(MONDAY, 12, 30, 59)).getCompletedPaidMinutes(),
                    "lunch boundary");
            eq(180, snapshot(at(MONDAY, 14, 0, 59)).getCompletedPaidMinutes(),
                    "afternoon boundary");
            eq(181, snapshot(at(MONDAY, 14, 1, 0)).getCompletedPaidMinutes(),
                    "next minute");
        });

        test("UI-005 remaining minutes never negative", () -> {
            PaySnapshot s = snapshot(at(MONDAY, 23, 0, 0));
            eq(0, s.getRemainingPaidMinutes(), "after work");
            eq(Duration.ZERO, s.getMainCountdown(), "countdown");
        });
    }

    private static void actualWorkdays() {
        test("CALC-027 2026 monthly workday counts", () -> {
            int[] expected = {21, 16, 22, 21, 19, 21, 23, 21, 22, 18, 21, 23};
            int total = 0;
            for (int month = 1; month <= 12; month++) {
                int count = WorkdayCalendar.workdaysInMonth(2026, month);
                eq(expected[month - 1], count, "2026-" + month);
                total += count;
            }
            eq(248, total, "2026 total");
        });

        test("CALC-028 2027 monthly workday counts", () -> {
            int[] expected = {21, 15, 23, 21, 19, 21, 22, 22, 22, 17, 22, 23};
            int total = 0;
            for (int month = 1; month <= 12; month++) {
                int count = WorkdayCalendar.workdaysInMonth(2027, month);
                eq(expected[month - 1], count, "2027-" + month);
                total += count;
            }
            eq(248, total, "2027 total");
        });

        test("CALC-029 actual mode uses November 2026 count of 21", () -> {
            PaySnapshot s = actualSnapshot(at(MONDAY, 9, 31, 0));
            truth(s.isActualWorkdays(), "actual flag");
            truth(s.isCalendarDataAvailable(), "calendar data");
            eq(DayKind.WORKDAY, s.getDayKind(), "day kind");
            eq(21, s.getMonthlyWorkdays(), "monthly workdays");
            eq("¥1,595.24", Formatters.money(s.getDailyWage()), "daily wage");
            eq("¥3.32", Formatters.money(s.getMinuteWage()), "minute wage");
            eq("¥199.40", Formatters.money(s.getHourlyWage()), "hourly wage");
            eq("¥3.32", Formatters.money(s.getTodayEarned()), "earned");
        });

        test("CALC-030 2026 National Day is a holiday and uses 18 days", () -> {
            PaySnapshot s = actualSnapshot(at(LocalDate.of(2026, 10, 1), 12, 0, 0));
            eq(DayPhase.REST_DAY, s.getPhase(), "phase");
            eq(DayKind.HOLIDAY, s.getDayKind(), "day kind");
            eq(18, s.getMonthlyWorkdays(), "monthly workdays");
            eq("¥1,861.11", Formatters.money(s.getDailyWage()), "daily wage");
            eq("¥0.00", Formatters.money(s.getTodayEarned()), "earned");
            eq("节假日休息", PhaseText.status(s.getPhase(), s.getDayKind()), "status");
            eq("节假日不计薪", PhaseText.updateNote(s.getPhase(), s.getDayKind()), "note");
            eq(Duration.ZERO, s.getMainCountdown(), "countdown");
        });

        test("CALC-031 2026-10-10 Saturday is a make-up workday", () -> {
            PaySnapshot s = actualSnapshot(at(LocalDate.of(2026, 10, 10), 9, 31, 0));
            eq(DayPhase.MORNING_WORK, s.getPhase(), "phase");
            eq(DayKind.MAKEUP_WORKDAY, s.getDayKind(), "day kind");
            eq("¥3.88", Formatters.money(s.getTodayEarned()), "earned");
        });

        test("CALC-032 2026-09-20 Sunday is a make-up workday", () -> {
            PaySnapshot s = actualSnapshot(at(LocalDate.of(2026, 9, 20), 9, 31, 0));
            eq(DayPhase.MORNING_WORK, s.getPhase(), "phase");
            eq(DayKind.MAKEUP_WORKDAY, s.getDayKind(), "day kind");
            eq(22, s.getMonthlyWorkdays(), "monthly workdays");
            eq("¥3.17", Formatters.money(s.getTodayEarned()), "earned");
        });

        test("CALC-033 2026 Spring Festival holiday has zero pay", () -> {
            PaySnapshot s = actualSnapshot(at(LocalDate.of(2026, 2, 17), 10, 0, 0));
            eq(DayPhase.REST_DAY, s.getPhase(), "phase");
            eq(DayKind.HOLIDAY, s.getDayKind(), "day kind");
            eq(16, s.getMonthlyWorkdays(), "monthly workdays");
            eq("¥2,093.75", Formatters.money(s.getDailyWage()), "daily wage");
            eq("¥0.00", Formatters.money(s.getTodayEarned()), "earned");
        });

        test("CALC-034 2027 Spring Festival holiday and 15-day month", () -> {
            PaySnapshot s = actualSnapshot(at(LocalDate.of(2027, 2, 5), 10, 0, 0));
            eq(DayPhase.REST_DAY, s.getPhase(), "phase");
            eq(DayKind.HOLIDAY, s.getDayKind(), "day kind");
            eq(15, s.getMonthlyWorkdays(), "monthly workdays");
            eq("¥2,233.33", Formatters.money(s.getDailyWage()), "daily wage");
            eq("¥0.00", Formatters.money(s.getTodayEarned()), "earned");
        });

        test("CALC-035 2027-02-14 Sunday make-up and next Monday work", () -> {
            PaySnapshot sunday = actualSnapshot(at(LocalDate.of(2027, 2, 14), 9, 31, 0));
            PaySnapshot monday = actualSnapshot(at(LocalDate.of(2027, 2, 15), 9, 31, 0));
            eq(DayKind.MAKEUP_WORKDAY, sunday.getDayKind(), "Sunday kind");
            eq(DayPhase.MORNING_WORK, sunday.getPhase(), "Sunday phase");
            eq("¥4.65", Formatters.money(sunday.getTodayEarned()), "Sunday earned");
            eq(DayKind.WORKDAY, monday.getDayKind(), "Monday kind");
            eq("¥4.65", Formatters.money(monday.getTodayEarned()), "Monday earned");
        });

        test("CALC-036 2027 National Day holiday and make-up Saturday", () -> {
            PaySnapshot holiday = actualSnapshot(at(LocalDate.of(2027, 10, 1), 12, 0, 0));
            PaySnapshot makeup = actualSnapshot(at(LocalDate.of(2027, 10, 9), 9, 31, 0));
            eq(DayKind.HOLIDAY, holiday.getDayKind(), "holiday kind");
            eq(17, holiday.getMonthlyWorkdays(), "October workdays");
            eq("¥0.00", Formatters.money(holiday.getTodayEarned()), "holiday earned");
            eq(DayKind.MAKEUP_WORKDAY, makeup.getDayKind(), "makeup kind");
            eq("¥4.11", Formatters.money(makeup.getTodayEarned()), "makeup earned");
        });

        test("CALC-037 outside 2026-2027 falls back to Monday-Friday", () -> {
            PaySnapshot monday = actualSnapshot(at(LocalDate.of(2028, 1, 3), 9, 31, 0));
            PaySnapshot saturday = actualSnapshot(at(LocalDate.of(2028, 1, 1), 12, 0, 0));
            eq(DayKind.WORKDAY, monday.getDayKind(), "Monday kind");
            falsity(monday.isCalendarDataAvailable(), "calendar data flag");
            eq(21, monday.getMonthlyWorkdays(), "January 2028 weekdays");
            eq("¥3.32", Formatters.money(monday.getTodayEarned()), "Monday earned");
            eq(DayKind.WEEKEND, saturday.getDayKind(), "Saturday kind");
            eq(DayPhase.REST_DAY, saturday.getPhase(), "Saturday phase");
        });

        test("CALC-038 average mode ignores the holiday table", () -> {
            PaySnapshot s = snapshot(at(LocalDate.of(2026, 10, 1), 9, 31, 0));
            eq(DayPhase.MORNING_WORK, s.getPhase(), "phase");
            eq(DayKind.WORKDAY, s.getDayKind(), "day kind");
            eq(0, s.getMonthlyWorkdays(), "monthly workdays");
            eq("¥3.21", Formatters.money(s.getTodayEarned()), "earned");
        });

        test("CALC-039 actual mode reaches the exact daily wage at 19:00", () -> {
            PaySnapshot s = actualSnapshot(at(MONDAY, 19, 0, 0));
            eq(DayPhase.AFTER_WORK, s.getPhase(), "phase");
            eq(0, s.getDailyWage().compareTo(s.getTodayEarned()), "daily equals earned");
            eq(480, s.getCompletedPaidMinutes(), "completed");
        });

        test("CALC-040 actual-workday flag survives copy", () -> {
            AppSettings s = AppSettings.createDefault();
            s.setActualWorkdaysEnabled(true);
            truth(s.copy().isActualWorkdaysEnabled(), "copy flag");
        });

        test("CALC-041 calendar data covers only 2026-2027", () -> {
            truth(WorkdayCalendar.hasData(2026), "2026");
            truth(WorkdayCalendar.hasData(2027), "2027");
            falsity(WorkdayCalendar.hasData(2025), "2025");
            falsity(WorkdayCalendar.hasData(2028), "2028");
        });

        test("CALC-042 day kinds and holiday boundaries", () -> {
            eq(DayKind.HOLIDAY, WorkdayCalendar.dayKind(LocalDate.of(2026, 1, 1)),
                    "2026 New Year");
            eq(DayKind.MAKEUP_WORKDAY, WorkdayCalendar.dayKind(LocalDate.of(2026, 1, 4)),
                    "2026 Jan makeup");
            eq(DayKind.WORKDAY, WorkdayCalendar.dayKind(MONDAY), "normal Monday");
            eq(DayKind.WEEKEND, WorkdayCalendar.dayKind(SATURDAY), "normal Saturday");
            eq(DayKind.HOLIDAY, WorkdayCalendar.dayKind(LocalDate.of(2027, 6, 9)),
                    "2027 Dragon Boat");
            eq(DayKind.MAKEUP_WORKDAY, WorkdayCalendar.dayKind(LocalDate.of(2027, 10, 9)),
                    "2027 National Day makeup");
        });

        test("FMT-010 month label", () -> {
            eq("2026年11月", Formatters.monthLabel(2026, 11), "month");
            eq("2027年2月", Formatters.monthLabel(LocalDate.of(2027, 2, 5)), "month date");
        });
    }
}
