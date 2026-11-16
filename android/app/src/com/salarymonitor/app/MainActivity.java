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
import android.widget.Toast;

import com.salarymonitor.app.core.AppSettings;
import com.salarymonitor.app.core.DayKind;
import com.salarymonitor.app.core.DayPhase;
import com.salarymonitor.app.core.Formatters;
import com.salarymonitor.app.core.MonthCalculator;
import com.salarymonitor.app.core.MonthSnapshot;
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
    private static final int TEXT_MUTED = 0xFF64748B;
    private static final int TEXT_ACCENT = 0xFF67E8F9;
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
    private MonthSnapshot monthSnapshot;
    private boolean amountsHidden;
    private boolean monthView;
    private ImageButton amountVisibilityButton;
    private View todayPage;
    private View monthPage;
    private TextView todayTab;
    private TextView monthTab;
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

    private TextView monthAmountText;
    private TextView monthBasisTag;
    private TextView monthDailyText;
    private TextView monthWorkdaysText;
    private TextView monthElapsedText;
    private TextView monthProgressPercentText;
    private TextView monthProgressDaysText;
    private TextView monthPaydayTitle;
    private TextView monthPaydayDateText;
    private TextView monthPaydayDaysText;
    private TextView monthPaydayUnitText;
    private TextView monthPaydayHintText;
    private ProgressBarView monthProgressBar;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        getWindow().setStatusBarColor(COLOR_BG);
        getWindow().setNavigationBarColor(COLOR_BG);
        setContentView(R.layout.activity_main);

        settings = SettingsRepository.load(this);
        amountsHidden = settings.isAmountsHidden();
        bindViews();

        ImageButton settingsButton = findViewById(R.id.headerSettings);
        settingsButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                openSettings();
            }
        });

        amountVisibilityButton = findViewById(R.id.amountVisibilityButton);
        amountVisibilityButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                toggleAmountVisibility();
            }
        });
        renderAmountVisibility();

        todayTab.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                setMonthView(false);
            }
        });
        monthTab.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                setMonthView(true);
            }
        });
        applyTabState();

        updateUi();
    }

    private void bindViews() {
        todayPage = findViewById(R.id.todayPage);
        monthPage = findViewById(R.id.monthPage);
        todayTab = findViewById(R.id.todayTab);
        monthTab = findViewById(R.id.monthTab);
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

        monthAmountText = findViewById(R.id.monthAmountText);
        monthBasisTag = findViewById(R.id.monthBasisTag);
        monthDailyText = findViewById(R.id.monthDailyText);
        monthWorkdaysText = findViewById(R.id.monthWorkdaysText);
        monthElapsedText = findViewById(R.id.monthElapsedText);
        monthProgressPercentText = findViewById(R.id.monthProgressPercentText);
        monthProgressDaysText = findViewById(R.id.monthProgressDaysText);
        monthPaydayTitle = findViewById(R.id.monthPaydayTitle);
        monthPaydayDateText = findViewById(R.id.monthPaydayDateText);
        monthPaydayDaysText = findViewById(R.id.monthPaydayDaysText);
        monthPaydayUnitText = findViewById(R.id.monthPaydayUnitText);
        monthPaydayHintText = findViewById(R.id.monthPaydayHintText);
        monthProgressBar = findViewById(R.id.monthProgressBar);
    }

    @Override
    protected void onResume() {
        super.onResume();
        settings = SettingsRepository.load(this);
        amountsHidden = settings.isAmountsHidden();
        applyKeepScreenOn();
        renderAmountVisibility();
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

    private void setMonthView(boolean month) {
        monthView = month;
        todayPage.setVisibility(month ? View.GONE : View.VISIBLE);
        monthPage.setVisibility(month ? View.VISIBLE : View.GONE);
        applyTabState();
        updateUi();
    }

    private void applyTabState() {
        if (todayTab == null || monthTab == null) {
            return;
        }
        todayTab.setBackgroundResource(monthView
                ? android.R.color.transparent : R.drawable.bg_tab_active);
        monthTab.setBackgroundResource(monthView
                ? R.drawable.bg_tab_active : android.R.color.transparent);
        todayTab.setTextColor(monthView ? TEXT_MUTED : TEXT_ACCENT);
        monthTab.setTextColor(monthView ? TEXT_ACCENT : TEXT_MUTED);
        todayTab.setTypeface(Typeface.create("sans-serif",
                monthView ? Typeface.NORMAL : Typeface.BOLD));
        monthTab.setTypeface(Typeface.create("sans-serif",
                monthView ? Typeface.BOLD : Typeface.NORMAL));
    }

    @Override
    public void onBackPressed() {
        if (monthView) {
            setMonthView(false);
            return;
        }
        super.onBackPressed();
    }

    private void toggleAmountVisibility() {
        amountsHidden = !amountsHidden;
        settings = settings.copy();
        settings.setAmountsHidden(amountsHidden);
        if (!SettingsRepository.save(this, settings)) {
            Toast.makeText(this, "金额隐藏状态未能保存", Toast.LENGTH_SHORT).show();
        }
        renderAmountVisibility();
        updateUi();
    }

    private void renderAmountVisibility() {
        if (amountVisibilityButton == null) {
            return;
        }
        amountVisibilityButton.setImageResource(
                amountsHidden ? R.drawable.ic_eye_off : R.drawable.ic_eye);
        String description = getString(
                amountsHidden ? R.string.show_amount : R.string.hide_amount);
        amountVisibilityButton.setContentDescription(description);
        amountVisibilityButton.setTooltipText(description);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode == REQUEST_SETTINGS && resultCode == RESULT_OK) {
            settings = SettingsRepository.load(this);
            amountsHidden = settings.isAmountsHidden();
            applyKeepScreenOn();
            renderAmountVisibility();
            updateUi();
        }
    }

    private void updateUi() {
        LocalDateTime now = LocalDateTime.now();
        PaySnapshot snapshot = PayCalculator.calculate(settings, now);
        monthSnapshot = MonthCalculator.calculate(settings, now);

        if (amountsHidden) {
            amountText.setText(Formatters.HIDDEN_AMOUNT);
            amountText.setTextSize(TypedValue.COMPLEX_UNIT_SP, 48);
            dailyText.setText(Formatters.HIDDEN_MONEY);
            hourlyText.setText(Formatters.HIDDEN_MONEY);
            minuteText.setText(Formatters.HIDDEN_MONEY);
        } else {
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
        }

        dateText.setText(Formatters.dateHeader(now));
        clockText.setText(Formatters.timeOfDay(now));

        renderPhase(snapshot);
        renderBasis(snapshot);
        renderMonth(monthSnapshot);

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

    private void renderMonth(MonthSnapshot month) {
        if (month == null || monthAmountText == null) {
            return;
        }

        if (amountsHidden) {
            monthAmountText.setText(Formatters.HIDDEN_AMOUNT);
            monthAmountText.setTextSize(TypedValue.COMPLEX_UNIT_SP, 44);
            monthDailyText.setText(Formatters.HIDDEN_MONEY);
        } else {
            String amount = Formatters.moneyValue(month.getMonthEarned());
            monthAmountText.setText(amount);
            int amountSize;
            if (amount.length() > 12) {
                amountSize = 28;
            } else if (amount.length() > 9) {
                amountSize = 36;
            } else {
                amountSize = 44;
            }
            monthAmountText.setTextSize(TypedValue.COMPLEX_UNIT_SP, amountSize);
            monthDailyText.setText(Formatters.money(month.getDailyWage()));
        }

        monthBasisTag.setText(month.isActualWorkdays() ? "按实际工作日" : "按日薪累计");
        monthWorkdaysText.setText(month.getTotalWorkdays() + " 天");
        monthElapsedText.setText(Formatters.workdaysValue(month.getCompletedWorkdays()) + " 天");
        monthProgressDaysText.setText(Formatters.workdaysValue(month.getCompletedWorkdays())
                + " / " + month.getTotalWorkdays() + " 天");
        monthProgressPercentText.setText(month.getTotalWorkdays() > 0
                ? Formatters.progress(month.getProgress()) : "—");
        monthProgressBar.setProgress(month.getProgress());

        if (!month.isPaydayConfigured()) {
            monthPaydayTitle.setText("发薪日未设置");
            monthPaydayDateText.setText("");
            monthPaydayDaysText.setText("—");
            monthPaydayDaysText.setTypeface(Typeface.create("sans-serif", Typeface.NORMAL));
            monthPaydayDaysText.setTextSize(TypedValue.COMPLEX_UNIT_SP, 24);
            monthPaydayUnitText.setText("");
            monthPaydayHintText.setText("可在设置中填写");
        } else if (month.isPaydayToday()) {
            monthPaydayTitle.setText("今天发薪");
            monthPaydayDateText.setText(Formatters.monthDay(month.getNextPayday()));
            monthPaydayDaysText.setText("今天");
            monthPaydayDaysText.setTypeface(Typeface.create("sans-serif", Typeface.BOLD));
            monthPaydayDaysText.setTextSize(TypedValue.COMPLEX_UNIT_SP, 26);
            monthPaydayUnitText.setText("");
            monthPaydayHintText.setText("按自然日估算");
        } else {
            monthPaydayTitle.setText("距下次发薪");
            monthPaydayDateText.setText(Formatters.monthDay(month.getNextPayday()));
            monthPaydayDaysText.setText(String.valueOf(month.getDaysUntilPayday()));
            monthPaydayDaysText.setTypeface(Typeface.MONOSPACE);
            monthPaydayDaysText.setTextSize(TypedValue.COMPLEX_UNIT_SP, 34);
            monthPaydayUnitText.setText("天");
            monthPaydayHintText.setText("按自然日估算");
        }
    }

    private void renderBasis(PaySnapshot snapshot) {
        if (monthView && monthSnapshot != null) {
            if (monthSnapshot.isActualWorkdays()) {
                if (!monthSnapshot.isCalendarDataAvailable()) {
                    footerHint.setText("未收录 " + monthSnapshot.getYear()
                            + " 年节假日 · 按工作日估算");
                } else {
                    footerHint.setText("按本月 " + monthSnapshot.getTotalWorkdays()
                            + " 个工作日估算");
                }
            } else {
                footerHint.setText("日薪累计估算 · 月末可能不等于月薪");
            }
            updateNoteText.setText("本月实时累计");
            return;
        }

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
