package com.salarymonitor.app.data;

import android.content.Context;
import android.content.SharedPreferences;

import com.salarymonitor.app.core.AppSettings;
import com.salarymonitor.app.core.Formatters;
import com.salarymonitor.app.core.SettingsValidator;

import java.math.BigDecimal;

/** Local-only settings storage. The app has no network permission. */
public final class SettingsRepository {
    private static final String PREFS_NAME = "salary_monitor";
    private static final String KEY_VERSION = "version";
    private static final String KEY_SALARY = "monthly_salary";
    private static final String KEY_WORK_DAYS = "monthly_work_days";
    private static final String KEY_WORK_START = "work_start_minutes";
    private static final String KEY_LUNCH_START = "lunch_start_minutes";
    private static final String KEY_LUNCH_END = "lunch_end_minutes";
    private static final String KEY_WORK_END = "work_end_minutes";
    private static final String KEY_KEEP_SCREEN_ON = "keep_screen_on";
    private static final String KEY_ACTUAL_WORKDAYS = "actual_workdays_enabled";

    private SettingsRepository() {
    }

    public static AppSettings load(Context context) {
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        try {
            AppSettings defaults = AppSettings.createDefault();
            BigDecimal salary = readDecimal(prefs, KEY_SALARY,
                    defaults.getMonthlySalary());
            BigDecimal days = readDecimal(prefs, KEY_WORK_DAYS,
                    defaults.getMonthlyWorkDays());

            AppSettings settings = new AppSettings();
            settings.setVersion(prefs.getInt(KEY_VERSION, AppSettings.CURRENT_VERSION));
            settings.setMonthlySalary(salary);
            settings.setMonthlyWorkDays(days);
            settings.setWorkStartMinutes(prefs.getInt(KEY_WORK_START,
                    defaults.getWorkStartMinutes()));
            settings.setLunchStartMinutes(prefs.getInt(KEY_LUNCH_START,
                    defaults.getLunchStartMinutes()));
            settings.setLunchEndMinutes(prefs.getInt(KEY_LUNCH_END,
                    defaults.getLunchEndMinutes()));
            settings.setWorkEndMinutes(prefs.getInt(KEY_WORK_END,
                    defaults.getWorkEndMinutes()));
            settings.setKeepScreenOn(prefs.getBoolean(KEY_KEEP_SCREEN_ON, true));
            settings.setActualWorkdaysEnabled(prefs.getBoolean(KEY_ACTUAL_WORKDAYS, false));

            SettingsValidator.Result check = SettingsValidator.validate(settings);
            if (!check.isValid()) {
                return defaults;
            }
            return settings;
        } catch (RuntimeException ex) {
            return AppSettings.createDefault();
        }
    }

    public static boolean save(Context context, AppSettings settings) {
        if (settings == null) {
            return false;
        }
        SharedPreferences prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE);
        SharedPreferences.Editor editor = prefs.edit();
        editor.putInt(KEY_VERSION, AppSettings.CURRENT_VERSION);
        editor.putString(KEY_SALARY, settings.getMonthlySalary().toPlainString());
        editor.putString(KEY_WORK_DAYS, settings.getMonthlyWorkDays().toPlainString());
        editor.putInt(KEY_WORK_START, settings.getWorkStartMinutes());
        editor.putInt(KEY_LUNCH_START, settings.getLunchStartMinutes());
        editor.putInt(KEY_LUNCH_END, settings.getLunchEndMinutes());
        editor.putInt(KEY_WORK_END, settings.getWorkEndMinutes());
        editor.putBoolean(KEY_KEEP_SCREEN_ON, settings.isKeepScreenOn());
        editor.putBoolean(KEY_ACTUAL_WORKDAYS, settings.isActualWorkdaysEnabled());
        return editor.commit();
    }

    private static BigDecimal readDecimal(SharedPreferences prefs, String key,
                                          BigDecimal fallback) {
        String value = prefs.getString(key, null);
        if (value == null) {
            return fallback;
        }
        BigDecimal parsed = Formatters.parseDecimal(value);
        return parsed == null ? fallback : parsed;
    }
}
