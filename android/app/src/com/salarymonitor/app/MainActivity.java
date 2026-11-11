package com.salarymonitor.app;

import android.app.Activity;
import android.content.Intent;
import android.content.res.ColorStateList;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.util.TypedValue;
import android.view.View;
import android.view.WindowManager;
import android.widget.ImageButton;
import android.widget.TextView;

import com.salarymonitor.app.core.AppSettings;
import com.salarymonitor.app.core.DayKind;
import com.salarymonitor.app.core.DayPhase;
import com.salarymonitor.app.core.Formatters;
import com.salarymonitor.app.core.PayCalculator;
import com.salarymonitor.app.core.PaySnapshot;
import com.salarymonitor.app.core.PhaseText;
import com.salarymonitor.app.data.SettingsRepository;
import com.salarymonitor.app.ui.ProgressBarView;

import java.time.LocalDateTime;

public final class MainActivity extends Activity {
    private static final int REQUEST_SETTINGS = 41;
    private static final int COLOR_BG = 0xFF071019;
    private static final int TEXT_PRIMARY = 0xFFF8FAFC;
    private static final int TEXT_SECONDARY = 0xFF94A3B8;
    private static final int WORK_BG = 0x202DD4BF;
    private static final int WORK_BORDER = 0x372DD4BF;
    private static final int WORK_TEXT = 0xFF99F6E4;
    private static final int LUNCH_BG = 0x1CFBBF24;
    private static final int LUNCH_BORDER = 0x34FBBF24;
    private static final int LUNCH_TEXT = 0xFFFDE68A;
    private static final int NEUTRAL_BG = 0x0F94A3B8;
    private static final int NEUTRAL_BORDER = 0x1E94A3B8;
    private static final int NEUTRAL_TEXT = 0xFFCBD5E1;

    private final Handler handler = new Handler(Looper.getMainLooper());
    private final Runnable ticker = new Runnable() {
        @Override
        public void run() {
            if (isFinishing()) {
                return;
            }
            updateUi();
            handler.postDelayed(this, 1000L);
        }
    };

    private AppSettings settings;
    private TextView statusText;
    private View statusDot;
    private View statusPill;
    private TextView dateText;
    private TextView clockText;
    private TextView amountText;
    private TextView dailyText;
    private TextView hourlyText;
    private TextView minuteText;
    private TextView countdownLabel;
    private TextView offTimeText;
    private TextView timerText;
    private TextView remainingText;
    private TextView progressPercentText;
    private TextView progressMinutesText;
    private TextView updateNoteText;
    private TextView footerHint;
    private ProgressBarView progressBar;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        getWindow().setStatusBarColor(COLOR_BG);
        getWindow().setNavigationBarColor(COLOR_BG);
        setContentView(R.layout.activity_main);

        settings = SettingsRepository.load(this);
        bindViews();

        ImageButton settingsButton = findViewById(R.id.headerSettings);
        settingsButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                openSettings();
            }
        });

        updateUi();
    }

    private void bindViews() {
        statusPill = findViewById(R.id.statusPill);
        statusDot = findViewById(R.id.statusDot);
        statusText = findViewById(R.id.statusText);
        dateText = findViewById(R.id.dateText);
        clockText = findViewById(R.id.clockText);
        amountText = findViewById(R.id.amountText);
        dailyText = findViewById(R.id.dailyText);
        hourlyText = findViewById(R.id.hourlyText);
        minuteText = findViewById(R.id.minuteText);
        countdownLabel = findViewById(R.id.countdownLabel);
        offTimeText = findViewById(R.id.offTimeText);
        timerText = findViewById(R.id.timerText);
        remainingText = findViewById(R.id.remainingText);
        progressPercentText = findViewById(R.id.progressPercentText);
        progressMinutesText = findViewById(R.id.progressMinutesText);
        progressBar = findViewById(R.id.progressBar);
        updateNoteText = findViewById(R.id.updateNoteText);
        footerHint = findViewById(R.id.footerHint);
    }

    @Override
    protected void onResume() {
        super.onResume();
        settings = SettingsRepository.load(this);
        applyKeepScreenOn();
        updateUi();
        handler.removeCallbacks(ticker);
        handler.postDelayed(ticker, 1000L);
    }

    @Override
    protected void onPause() {
        handler.removeCallbacks(ticker);
        super.onPause();
    }

    @Override
    protected void onDestroy() {
        handler.removeCallbacks(ticker);
        super.onDestroy();
    }

    private void applyKeepScreenOn() {
        if (settings.isKeepScreenOn()) {
            getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        } else {
            getWindow().clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        }
    }

    private void openSettings() {
        Intent intent = new Intent(this, SettingsActivity.class);
        startActivityForResult(intent, REQUEST_SETTINGS);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode == REQUEST_SETTINGS && resultCode == RESULT_OK) {
            settings = SettingsRepository.load(this);
            applyKeepScreenOn();
            updateUi();
        }
    }

    private void updateUi() {
        LocalDateTime now = LocalDateTime.now();
        PaySnapshot snapshot = PayCalculator.calculate(settings, now);

        String amount = Formatters.moneyValue(snapshot.getTodayEarned());
        amountText.setText(amount);
        int amountSize;
        if (amount.length() > 12) {
            amountSize = 30;
        } else if (amount.length() > 9) {
            amountSize = 38;
        } else {
            amountSize = 48;
        }
        amountText.setTextSize(TypedValue.COMPLEX_UNIT_SP, amountSize);

        dailyText.setText(Formatters.money(snapshot.getDailyWage()));
        hourlyText.setText(Formatters.money(snapshot.getHourlyWage()));
        minuteText.setText(Formatters.money(snapshot.getMinuteWage()));

        dateText.setText(Formatters.dateHeader(now));
        clockText.setText(Formatters.timeOfDay(now));

        renderPhase(snapshot);
        renderBasis(snapshot);

        if (snapshot.getPhase() == DayPhase.REST_DAY) {
            progressBar.setProgress(0.0);
            progressPercentText.setText("—");
            progressMinutesText.setText(PhaseText.restPayNote(snapshot.getDayKind()));
        } else {
            progressBar.setProgress(snapshot.getProgress());
            progressPercentText.setText(Formatters.progress(snapshot.getProgress()));
            progressMinutesText.setText(snapshot.getCompletedPaidMinutes()
                    + " / " + snapshot.getTotalPaidMinutes() + " 分钟");
        }
    }

    private void renderBasis(PaySnapshot snapshot) {
        String basis;
        if (snapshot.isActualWorkdays()) {
            if (!snapshot.isCalendarDataAvailable()) {
                basis = "未收录 " + snapshot.getNow().getYear() + " 年节假日";
            } else if (snapshot.getDayKind() == DayKind.MAKEUP_WORKDAY) {
                basis = "今日调休上班 · 本月 " + snapshot.getMonthlyWorkdays() + " 个工作日";
            } else {
                basis = "按本月 " + snapshot.getMonthlyWorkdays() + " 个工作日";
            }
        } else {
            basis = "按 " + Formatters.daysInput(settings.getMonthlyWorkDays()) + " 天平均";
        }
        footerHint.setText(basis + "估算 · 以实际薪资为准");
    }

    private void renderPhase(PaySnapshot snapshot) {
        DayPhase phase = snapshot.getPhase();
        String status = PhaseText.status(phase, snapshot.getDayKind());
        statusText.setText(status);
        countdownLabel.setText(PhaseText.countdownCaption(phase));
        updateNoteText.setText(PhaseText.updateNote(phase, snapshot.getDayKind()));

        timerText.setTypeface(Typeface.MONOSPACE);
        timerText.setTextSize(TypedValue.COMPLEX_UNIT_SP, 32);
        timerText.setTextColor(TEXT_PRIMARY);

        switch (phase) {
            case REST_DAY:
                setStatusPalette(NEUTRAL_BG, NEUTRAL_BORDER, NEUTRAL_TEXT);
                offTimeText.setText(PhaseText.restPayNote(snapshot.getDayKind()));
                timerText.setTypeface(Typeface.create("sans-serif", Typeface.NORMAL));
                timerText.setTextSize(TypedValue.COMPLEX_UNIT_SP, 28);
                timerText.setTextColor(TEXT_SECONDARY);
                timerText.setText("休息日");
                remainingText.setText("今日不计薪");
                break;

            case BEFORE_WORK:
                setStatusPalette(NEUTRAL_BG, NEUTRAL_BORDER, NEUTRAL_TEXT);
                offTimeText.setText("上班时间 " + Formatters.clockHHmm(
                        settings.getWorkStartMinutes()));
                timerText.setText(Formatters.countdown(snapshot.getMainCountdown()));
                remainingText.setText(Formatters.workDuration(
                        snapshot.getRemainingPaidMinutes()));
                break;

            case LUNCH_BREAK:
                setStatusPalette(LUNCH_BG, LUNCH_BORDER, LUNCH_TEXT);
                offTimeText.setText("午休至 " + Formatters.clockHHmm(
                        settings.getLunchEndMinutes()));
                timerText.setText(Formatters.countdown(snapshot.getMainCountdown()));
                remainingText.setText(Formatters.workDuration(
                        snapshot.getRemainingPaidMinutes()));
                break;

            case MORNING_WORK:
            case AFTERNOON_WORK:
                setStatusPalette(WORK_BG, WORK_BORDER, WORK_TEXT);
                offTimeText.setText("下班时间 " + Formatters.clockHHmm(
                        settings.getWorkEndMinutes()));
                timerText.setText(Formatters.countdown(snapshot.getMainCountdown()));
                remainingText.setText(Formatters.workDuration(
                        snapshot.getRemainingPaidMinutes()));
                break;

            case AFTER_WORK:
            default:
                setStatusPalette(NEUTRAL_BG, NEUTRAL_BORDER, NEUTRAL_TEXT);
                offTimeText.setText("下班时间 " + Formatters.clockHHmm(
                        settings.getWorkEndMinutes()));
                timerText.setText("00:00:00");
                remainingText.setText("0 分钟");
                break;
        }
    }

    private void setStatusPalette(int background, int border, int text) {
        GradientDrawable pill = new GradientDrawable();
        pill.setColor(background);
        pill.setCornerRadius(dp(14));
        pill.setStroke(dp(1), border);
        statusPill.setBackground(pill);
        statusText.setTextColor(text);
        statusDot.setBackgroundTintList(ColorStateList.valueOf(text));
    }

    private int dp(int value) {
        return Math.round(value * getResources().getDisplayMetrics().density);
    }
}
