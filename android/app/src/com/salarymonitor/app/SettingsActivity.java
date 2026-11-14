package com.salarymonitor.app;

import android.app.Activity;
import android.app.TimePickerDialog;
import android.content.res.ColorStateList;
import android.graphics.Color;
import android.os.Bundle;
import android.text.Editable;
import android.text.TextWatcher;
import android.view.View;
import android.widget.Button;
import android.widget.CompoundButton;
import android.widget.EditText;
import android.widget.ImageButton;
import android.widget.ScrollView;
import android.widget.Switch;
import android.widget.TextView;
import android.widget.Toast;
import android.widget.TimePicker;

import com.salarymonitor.app.core.AppSettings;
import com.salarymonitor.app.core.Formatters;
import com.salarymonitor.app.core.SettingsValidator;
import com.salarymonitor.app.core.WorkdayCalendar;
import com.salarymonitor.app.data.SettingsRepository;

import java.math.BigDecimal;
import java.math.MathContext;
import java.time.LocalDate;

public final class SettingsActivity extends Activity {
    private static final int INDEX_WORK_START = 0;
    private static final int INDEX_LUNCH_START = 1;
    private static final int INDEX_LUNCH_END = 2;
    private static final int INDEX_WORK_END = 3;
    private static final MathContext MC = MathContext.DECIMAL128;
    private static final int DANGER = 0xFFFCA5A5;

    private final int[] times = new int[4];

    private ScrollView scroll;
    private EditText salaryInput;
    private EditText daysInput;
    private TextView startTime;
    private TextView lunchStartTime;
    private TextView lunchEndTime;
    private TextView endTime;
    private TextView startUnit;
    private TextView lunchStartUnit;
    private TextView lunchEndUnit;
    private TextView endUnit;
    private TextView incomeSummary;
    private TextView durationSummary;
    private TextView actualHint;
    private TextView errorText;
    private Switch keepScreenOnSwitch;
    private Switch actualWorkdaysSwitch;
    private View salaryField;
    private View daysField;
    private View daysColumn;
    private View startField;
    private View lunchStartField;
    private View lunchEndField;
    private View endField;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        getWindow().setStatusBarColor(0xFF071019);
        getWindow().setNavigationBarColor(0xFF071019);
        setContentView(R.layout.activity_settings);

        bindViews();
        populate(SettingsRepository.load(this));
        wireEvents();
    }

    private void bindViews() {
        scroll = findViewById(R.id.settingsScroll);
        salaryInput = findViewById(R.id.salaryInput);
        daysInput = findViewById(R.id.daysInput);
        startTime = findViewById(R.id.startTime);
        lunchStartTime = findViewById(R.id.lunchStartTime);
        lunchEndTime = findViewById(R.id.lunchEndTime);
        endTime = findViewById(R.id.endTime);
        startUnit = findViewById(R.id.startUnit);
        lunchStartUnit = findViewById(R.id.lunchStartUnit);
        lunchEndUnit = findViewById(R.id.lunchEndUnit);
        endUnit = findViewById(R.id.endUnit);
        incomeSummary = findViewById(R.id.incomeSummary);
        durationSummary = findViewById(R.id.durationSummary);
        actualHint = findViewById(R.id.actualHint);
        errorText = findViewById(R.id.errorText);
        keepScreenOnSwitch = findViewById(R.id.keepScreenOnSwitch);
        actualWorkdaysSwitch = findViewById(R.id.actualWorkdaysSwitch);
        salaryField = findViewById(R.id.salaryField);
        daysField = findViewById(R.id.daysField);
        daysColumn = findViewById(R.id.daysColumn);
        startField = findViewById(R.id.startField);
        lunchStartField = findViewById(R.id.lunchStartField);
        lunchEndField = findViewById(R.id.lunchEndField);
        endField = findViewById(R.id.endField);
    }

    private void wireEvents() {
        findViewById(R.id.closeButton).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                finish();
            }
        });
        findViewById(R.id.cancelButton).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                finish();
            }
        });
        findViewById(R.id.saveButton).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                save();
            }
        });
        findViewById(R.id.resetButton).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                populate(AppSettings.createDefault());
                Toast.makeText(SettingsActivity.this, "已恢复默认值", Toast.LENGTH_SHORT).show();
            }
        });

        startField.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                showTimePicker(INDEX_WORK_START);
            }
        });
        lunchStartField.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                showTimePicker(INDEX_LUNCH_START);
            }
        });
        lunchEndField.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                showTimePicker(INDEX_LUNCH_END);
            }
        });
        endField.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                showTimePicker(INDEX_WORK_END);
            }
        });

        TextWatcher watcher = new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence s, int start, int count, int after) {
            }

            @Override
            public void onTextChanged(CharSequence s, int start, int before, int count) {
            }

            @Override
            public void afterTextChanged(Editable s) {
                clearError();
                updateSummary();
            }
        };
        salaryInput.addTextChangedListener(watcher);
        daysInput.addTextChangedListener(watcher);
        actualWorkdaysSwitch.setOnCheckedChangeListener(
                new CompoundButton.OnCheckedChangeListener() {
                    @Override
                    public void onCheckedChanged(CompoundButton buttonView, boolean isChecked) {
                        clearError();
                        updateActualMode();
                        updateSummary();
                    }
                });
    }

    private void populate(AppSettings settings) {
        salaryInput.setText(Formatters.amountInput(settings.getMonthlySalary()));
        daysInput.setText(Formatters.daysInput(settings.getMonthlyWorkDays()));
        times[INDEX_WORK_START] = settings.getWorkStartMinutes();
        times[INDEX_LUNCH_START] = settings.getLunchStartMinutes();
        times[INDEX_LUNCH_END] = settings.getLunchEndMinutes();
        times[INDEX_WORK_END] = settings.getWorkEndMinutes();
        keepScreenOnSwitch.setChecked(settings.isKeepScreenOn());
        actualWorkdaysSwitch.setChecked(settings.isActualWorkdaysEnabled());
        updateTimeViews();
        updateActualMode();
        updateSummary();
        clearError();
    }

    private void showTimePicker(final int index) {
        int minutes = times[index];
        int hour = minutes / 60;
        int minute = minutes % 60;
        TimePickerDialog dialog = new TimePickerDialog(this,
                new TimePickerDialog.OnTimeSetListener() {
                    @Override
                    public void onTimeSet(TimePicker view, int hourOfDay, int minute) {
                        times[index] = hourOfDay * 60 + minute;
                        updateTimeViews();
                        clearError();
                        updateSummary();
                    }
                }, hour, minute, true);
        dialog.show();
    }

    private void updateTimeViews() {
        startTime.setText(Formatters.clockHHmm(times[INDEX_WORK_START]));
        lunchStartTime.setText(Formatters.clockHHmm(times[INDEX_LUNCH_START]));
        lunchEndTime.setText(Formatters.clockHHmm(times[INDEX_LUNCH_END]));
        endTime.setText(Formatters.clockHHmm(times[INDEX_WORK_END]));
        startUnit.setText(Formatters.timeUnitLabel(times[INDEX_WORK_START]));
        lunchStartUnit.setText(Formatters.timeUnitLabel(times[INDEX_LUNCH_START]));
        lunchEndUnit.setText(Formatters.timeUnitLabel(times[INDEX_LUNCH_END]));
        endUnit.setText(Formatters.timeUnitLabel(times[INDEX_WORK_END]));
    }

    private void updateActualMode() {
        boolean actual = actualWorkdaysSwitch.isChecked();
        daysColumn.setVisibility(actual ? View.GONE : View.VISIBLE);

        LocalDate now = LocalDate.now();
        if (actual) {
            int workdays = WorkdayCalendar.workdaysInMonth(
                    now.getYear(), now.getMonthValue());
            String month = Formatters.monthLabel(now);
            if (WorkdayCalendar.hasData(now.getYear())) {
                actualHint.setText(month + " · " + workdays
                        + " 个工作日（已按法定节假日与调休计算）");
            } else {
                actualHint.setText(month + " · " + workdays
                        + " 个工作日（未收录该年节假日，按周一至周五）");
            }
        } else {
            actualHint.setText("关闭后按上方月计薪天数平均计算；开启后按当月实际工作日计算。");
        }
    }

    private void updateSummary() {
        BigDecimal salary = Formatters.parseDecimal(salaryInput.getText().toString());
        boolean actual = actualWorkdaysSwitch.isChecked();
        LocalDate now = LocalDate.now();
        int actualWorkdays = WorkdayCalendar.workdaysInMonth(
                now.getYear(), now.getMonthValue());
        BigDecimal days = actual
                ? BigDecimal.valueOf(actualWorkdays)
                : Formatters.parseDecimal(daysInput.getText().toString());
        int total = totalMinutes();

        if (salary == null || days == null || salary.signum() <= 0 || days.signum() <= 0) {
            incomeSummary.setText("输入有效的月薪与天数后自动计算");
            incomeSummary.setTextColor(0xFF94A3B8);
        } else {
            BigDecimal daily = salary.divide(days, MC);
            BigDecimal minute = total > 0
                    ? daily.divide(BigDecimal.valueOf(total), MC)
                    : BigDecimal.ZERO;
            String basis = actual
                    ? Formatters.monthLabel(now) + " " + actualWorkdays + " 个工作日"
                    : "按 " + Formatters.daysInput(days) + " 天平均";
            incomeSummary.setText(basis + " · 日薪 " + Formatters.money(daily)
                    + " · 分钟薪 " + Formatters.money(minute));
            incomeSummary.setTextColor(0xFFA8B7C9);
        }

        if (!orderValid() || total <= 0) {
            durationSummary.setText("时间顺序有误，请检查");
            durationSummary.setTextColor(DANGER);
        } else {
            durationSummary.setText("每日有效工时 " + Formatters.workDuration(total));
            durationSummary.setTextColor(0xFFA8B7C9);
        }
    }

    private boolean orderValid() {
        return times[INDEX_WORK_START] < times[INDEX_LUNCH_START]
                && times[INDEX_LUNCH_START] < times[INDEX_LUNCH_END]
                && times[INDEX_LUNCH_END] < times[INDEX_WORK_END];
    }

    private int totalMinutes() {
        int morning = Math.max(0, times[INDEX_LUNCH_START] - times[INDEX_WORK_START]);
        int afternoon = Math.max(0, times[INDEX_WORK_END] - times[INDEX_LUNCH_END]);
        return morning + afternoon;
    }

    private void save() {
        clearError();
        BigDecimal salary = Formatters.parseDecimal(salaryInput.getText().toString());
        if (salary == null) {
            showError("请输入有效的月薪", salaryField);
            return;
        }
        BigDecimal days = Formatters.parseDecimal(daysInput.getText().toString());
        boolean actual = actualWorkdaysSwitch.isChecked();
        if (actual && (days == null || days.signum() <= 0
                || days.compareTo(SettingsValidator.MAX_MONTHLY_WORK_DAYS) > 0)) {
            days = SettingsRepository.load(this).getMonthlyWorkDays();
        }
        if (days == null) {
            showError("请输入有效的月计薪天数", daysField);
            return;
        }

        AppSettings settings = new AppSettings();
        settings.setMonthlySalary(salary);
        settings.setMonthlyWorkDays(days);
        settings.setWorkStartMinutes(times[INDEX_WORK_START]);
        settings.setLunchStartMinutes(times[INDEX_LUNCH_START]);
        settings.setLunchEndMinutes(times[INDEX_LUNCH_END]);
        settings.setWorkEndMinutes(times[INDEX_WORK_END]);
        settings.setKeepScreenOn(keepScreenOnSwitch.isChecked());
        settings.setActualWorkdaysEnabled(actualWorkdaysSwitch.isChecked());
        // The visibility toggle lives on the dashboard, not in this form.
        settings.setAmountsHidden(SettingsRepository.load(this).isAmountsHidden());

        SettingsValidator.Result validation = SettingsValidator.validate(settings);
        if (!validation.isValid()) {
            showError(validation.firstMessage(), fieldFor(validation.firstField()));
            return;
        }

        if (!SettingsRepository.save(this, settings)) {
            showError("保存失败，请重试", null);
            return;
        }
        setResult(RESULT_OK);
        finish();
    }

    private View fieldFor(String name) {
        if ("MonthlySalary".equals(name)) {
            return salaryField;
        }
        if ("MonthlyWorkDays".equals(name)) {
            return daysField;
        }
        if ("WorkStartMinutes".equals(name)) {
            return startField;
        }
        if ("LunchStartMinutes".equals(name)) {
            return lunchStartField;
        }
        if ("LunchEndMinutes".equals(name)) {
            return lunchEndField;
        }
        if ("WorkEndMinutes".equals(name)) {
            return endField;
        }
        return null;
    }

    private void showError(String message, View field) {
        errorText.setText(message);
        errorText.setVisibility(View.VISIBLE);
        if (field != null) {
            field.setBackgroundResource(R.drawable.bg_input_error);
        }
        errorText.post(new Runnable() {
            @Override
            public void run() {
                scroll.smoothScrollTo(0, errorText.getTop());
            }
        });
    }

    private void clearError() {
        errorText.setVisibility(View.GONE);
        salaryField.setBackgroundResource(R.drawable.bg_input);
        daysField.setBackgroundResource(R.drawable.bg_input);
        startField.setBackgroundResource(R.drawable.bg_input);
        lunchStartField.setBackgroundResource(R.drawable.bg_input);
        lunchEndField.setBackgroundResource(R.drawable.bg_input);
        endField.setBackgroundResource(R.drawable.bg_input);
    }
}
