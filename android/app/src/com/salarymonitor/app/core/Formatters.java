package com.salarymonitor.app.core;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.text.DecimalFormat;
import java.text.DecimalFormatSymbols;
import java.time.Duration;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.Locale;

public final class Formatters {
    public static final String HIDDEN_AMOUNT = "••••••";
    public static final String HIDDEN_MONEY = "¥••••";

    private static final DateTimeFormatter TIME_OF_DAY =
            DateTimeFormatter.ofPattern("HH:mm:ss", Locale.US);

    private Formatters() {
    }

    public static String moneyValue(BigDecimal amount) {
        BigDecimal value = amount == null ? BigDecimal.ZERO : amount;
        DecimalFormat format = new DecimalFormat("#,##0.00",
                DecimalFormatSymbols.getInstance(Locale.US));
        format.setRoundingMode(RoundingMode.HALF_UP);
        return format.format(value.setScale(2, RoundingMode.HALF_UP));
    }

    public static String money(BigDecimal amount) {
        return "¥" + moneyValue(amount);
    }

    public static String countdown(Duration span) {
        long seconds = 0;
        if (span != null && !span.isNegative()) {
            seconds = span.getSeconds();
            if (span.getNano() > 0) {
                seconds++;
            }
        }
        long hours = seconds / 3600;
        long minutes = (seconds % 3600) / 60;
        long rest = seconds % 60;
        return String.format(Locale.US, "%02d:%02d:%02d", hours, minutes, rest);
    }

    public static String progress(double ratio) {
        double value = Math.max(0.0, Math.min(1.0, ratio));
        double percent = Math.round(value * 1000.0) / 10.0;
        double whole = Math.round(percent);
        if (Math.abs(percent - whole) < 0.0001) {
            return String.format(Locale.US, "%d%%", (int) whole);
        }
        return String.format(Locale.US, "%.1f%%", percent);
    }

    public static String workDuration(int minutes) {
        int value = Math.max(0, minutes);
        int hours = value / 60;
        int rest = value % 60;
        if (hours <= 0) {
            return rest + " 分钟";
        }
        return hours + " 小时 " + rest + " 分";
    }

    public static String dateHeader(LocalDateTime now) {
        String[] weekdays = {"周日", "周一", "周二", "周三", "周四", "周五", "周六"};
        int index = now.getDayOfWeek().getValue() % 7;
        return String.format(Locale.US, "%d月%d日 · %s",
                now.getMonthValue(), now.getDayOfMonth(), weekdays[index]);
    }

    public static String timeOfDay(LocalDateTime now) {
        return TIME_OF_DAY.format(now);
    }

    public static String monthLabel(LocalDate date) {
        LocalDate value = date == null ? LocalDate.now() : date;
        return monthLabel(value.getYear(), value.getMonthValue());
    }

    public static String monthLabel(int year, int month) {
        return String.format(Locale.US, "%d年%d月", year, month);
    }

    public static String clockHHmm(int minutes) {
        int normalized = Math.max(0, minutes) % (24 * 60);
        return String.format(Locale.US, "%02d:%02d",
                normalized / 60, normalized % 60);
    }

    public static String timeUnitLabel(int minutes) {
        int normalized = Math.max(0, minutes) % (24 * 60);
        int hour = normalized / 60;
        if (hour < 6) {
            return "凌晨";
        }
        if (hour < 12) {
            return "上午";
        }
        if (hour == 12) {
            return "中午";
        }
        if (hour < 18) {
            return "下午";
        }
        return "晚上";
    }

    public static String amountInput(BigDecimal amount) {
        BigDecimal value = amount == null ? BigDecimal.ZERO : amount;
        return value.setScale(2, RoundingMode.HALF_UP).toPlainString();
    }

    public static String daysInput(BigDecimal days) {
        BigDecimal value = days == null ? BigDecimal.ZERO : days;
        BigDecimal stripped = value.stripTrailingZeros();
        if (stripped.scale() < 0) {
            stripped = stripped.setScale(0);
        }
        return stripped.toPlainString();
    }

    public static BigDecimal parseDecimal(String text) {
        if (text == null) {
            return null;
        }
        String cleaned = text.trim().replace(",", "").replace("，", "");
        if (cleaned.length() == 0) {
            return null;
        }
        try {
            return new BigDecimal(cleaned);
        } catch (NumberFormatException ex) {
            return null;
        }
    }

    public static Integer parseTimeOfDay(String text) {
        if (text == null) {
            return null;
        }
        String value = text.trim();
        String[] parts = value.split(":");
        if (parts.length != 2) {
            return null;
        }
        if (parts[0].length() < 1 || parts[0].length() > 2 || parts[1].length() != 2) {
            return null;
        }
        if (!asciiDigits(parts[0]) || !asciiDigits(parts[1])) {
            return null;
        }
        try {
            int hour = Integer.parseInt(parts[0]);
            int minute = Integer.parseInt(parts[1]);
            if (hour < 0 || hour > 23 || minute < 0 || minute > 59) {
                return null;
            }
            return hour * 60 + minute;
        } catch (NumberFormatException ex) {
            return null;
        }
    }

    private static boolean asciiDigits(String value) {
        if (value.length() == 0) {
            return false;
        }
        for (int i = 0; i < value.length(); i++) {
            char c = value.charAt(i);
            if (c < '0' || c > '9') {
                return false;
            }
        }
        return true;
    }
}
