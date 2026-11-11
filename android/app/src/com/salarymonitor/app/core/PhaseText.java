package com.salarymonitor.app.core;

/** User-facing labels shared by the dashboard and the core tests. */
public final class PhaseText {
    private PhaseText() {
    }

    public static String status(DayPhase phase) {
        switch (phase) {
            case REST_DAY:
                return "周末休息";
            case BEFORE_WORK:
                return "未上班";
            case MORNING_WORK:
                return "上午工作中";
            case LUNCH_BREAK:
                return "午休中";
            case AFTERNOON_WORK:
                return "下午工作中";
            case AFTER_WORK:
                return "今日已下班";
            default:
                return "";
        }
    }

    public static String status(DayPhase phase, DayKind dayKind) {
        if (phase == DayPhase.REST_DAY && dayKind == DayKind.HOLIDAY) {
            return "节假日休息";
        }
        return status(phase);
    }

    public static String restPayNote(DayKind dayKind) {
        return dayKind == DayKind.HOLIDAY ? "节假日不计薪" : "周末不计薪";
    }

    public static String countdownCaption(DayPhase phase) {
        switch (phase) {
            case REST_DAY:
                return "今日状态";
            case BEFORE_WORK:
                return "距上班";
            default:
                return "距离下班";
        }
    }

    public static String updateNote(DayPhase phase, DayKind dayKind) {
        if (phase == DayPhase.REST_DAY) {
            return restPayNote(dayKind);
        }
        return updateNote(phase);
    }

    public static String updateNote(DayPhase phase) {
        switch (phase) {
            case REST_DAY:
                return "周末不计薪";
            case BEFORE_WORK:
                return "每满 1 分钟更新工资";
            case LUNCH_BREAK:
                return "午休中 · 金额暂停";
            case AFTER_WORK:
                return "今日工资已封顶";
            default:
                return "每分钟更新工资";
        }
    }
}
