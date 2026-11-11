package com.salarymonitor.app.core;

/** Calendar classification used when the actual-workday mode is enabled. */
public enum DayKind {
    WORKDAY(true),
    MAKEUP_WORKDAY(true),
    WEEKEND(false),
    HOLIDAY(false);

    private final boolean workday;

    DayKind(boolean workday) {
        this.workday = workday;
    }

    public boolean isWorkday() {
        return workday;
    }

    public boolean isMakeupWorkday() {
        return this == MAKEUP_WORKDAY;
    }
}
