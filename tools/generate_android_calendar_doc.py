# -*- coding: utf-8 -*-
"""Generate the 2026-2027 workday calendar document used by the Android app."""

import calendar
import datetime as dt
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "docs" / "android" / "04-2026-2027日历与工作日.md"

WEEKDAYS = ["周一", "周二", "周三", "周四", "周五", "周六", "周日"]

HOLIDAYS = [
    ("元旦", dt.date(2026, 1, 1), dt.date(2026, 1, 3),
     [dt.date(2026, 1, 4)]),
    ("春节", dt.date(2026, 2, 15), dt.date(2026, 2, 23),
     [dt.date(2026, 2, 14), dt.date(2026, 2, 28)]),
    ("清明节", dt.date(2026, 4, 4), dt.date(2026, 4, 6), []),
    ("劳动节", dt.date(2026, 5, 1), dt.date(2026, 5, 5),
     [dt.date(2026, 5, 9)]),
    ("端午节", dt.date(2026, 6, 19), dt.date(2026, 6, 21), []),
    ("中秋节", dt.date(2026, 9, 25), dt.date(2026, 9, 27), []),
    ("国庆节", dt.date(2026, 10, 1), dt.date(2026, 10, 7),
     [dt.date(2026, 9, 20), dt.date(2026, 10, 10)]),
    ("元旦", dt.date(2027, 1, 1), dt.date(2027, 1, 3), []),
    ("春节", dt.date(2027, 2, 5), dt.date(2027, 2, 13),
     [dt.date(2027, 1, 31), dt.date(2027, 2, 14)]),
    ("清明节", dt.date(2027, 4, 3), dt.date(2027, 4, 5), []),
    ("劳动节", dt.date(2027, 5, 1), dt.date(2027, 5, 5),
     [dt.date(2027, 5, 8)]),
    ("端午节", dt.date(2027, 6, 9), dt.date(2027, 6, 9), []),
    ("中秋节", dt.date(2027, 9, 15), dt.date(2027, 9, 15), []),
    ("国庆节", dt.date(2027, 10, 1), dt.date(2027, 10, 7),
     [dt.date(2027, 9, 26), dt.date(2027, 10, 9)]),
]

MAKEUP = {day for _, _, _, days in HOLIDAYS for day in days}
HOLIDAY_DAYS = set()
for _, start, end, _ in HOLIDAYS:
    day = start
    while day <= end:
        HOLIDAY_DAYS.add(day)
        day += dt.timedelta(days=1)


def kind(day):
    if day in MAKEUP:
        return "班"
    if day in HOLIDAY_DAYS:
        return "休"
    if day.weekday() >= 5:
        return "·"
    return "工"


def is_workday(day):
    return kind(day) in ("工", "班")


def count_workdays(year, month):
    days = calendar.monthrange(year, month)[1]
    return sum(1 for d in range(1, days + 1)
               if is_workday(dt.date(year, month, d)))


def fmt_date(day):
    return f"{day.month}月{day.day}日（{WEEKDAYS[day.weekday()]}）"


def fmt_range(start, end):
    if start == end:
        return f"{fmt_date(start)}，1天"
    return f"{fmt_date(start)}—{fmt_date(end)}，{(end - start).days + 1}天"


def calendar_table(year, month):
    rows = ["| 周一 | 周二 | 周三 | 周四 | 周五 | 周六 | 周日 |",
            "|:---:|:---:|:---:|:---:|:---:|:---:|:---:|"]
    for week in calendar.monthcalendar(year, month):
        cells = []
        for day in week:
            if day == 0:
                cells.append("")
                continue
            date = dt.date(year, month, day)
            marker = kind(date)
            if marker == "工":
                cells.append(f"**{day}**")
            elif marker == "班":
                cells.append(f"**{day}班**")
            elif marker == "休":
                cells.append(f"{day}休")
            else:
                cells.append(f"{day}")
        rows.append("| " + " | ".join(cells) + " |")
    return "\n".join(rows)


def main():
    lines = [
        "# 2026—2027 年日历与工作日",
        "",
        "- 适用版本：Android v1.1.0",
        "- 整理日期：2026-11-11",
        "- 用途：为“按当月实际工作日计薪”提供日历数据",
        "- 数据来源：国务院办公厅 2026 年、2027 年部分节假日安排通知",
        "",
        "> 2027 年安排已于 2026-11-05 发布，本文按国务院办公厅通知整理。",
        "> 日历以周一为每周第一天；`**1**` 为工作日，`1` 为周末，`1休` 为法定节假日，`**1班**` 为调休上班。",
        "",
        "## 1. 官方通知",
        "",
        "| 年份 | 发文字号 | 发布日期 | 通知 |",
        "| --- | --- | --- | --- |",
        "| 2026 | 国办发明电〔2025〕7号 | 2025-11-04 | [国务院办公厅关于2026年部分节假日安排的通知](https://www.gov.cn/zhengce/zhengceku/202511/content_7047091.htm) |",
        "| 2027 | 国办发明电〔2026〕10号 | 2026-11-05 | [国务院办公厅关于2027年部分节假日安排的通知](https://www.gov.cn/zhengce/content/202611/content_7085101.htm) |",
        "",
        "## 2. 放假与调休安排",
        "",
    ]

    for year in (2026, 2027):
        lines += [f"### {year} 年", "",
                  "| 节日 | 放假安排 | 调休上班 |",
                  "| --- | --- | --- |"]
        for name, start, end, makeups in HOLIDAYS:
            if start.year != year:
                continue
            makeup_text = "、".join(fmt_date(day) for day in makeups) if makeups else "无"
            lines.append(f"| {name} | {fmt_range(start, end)} | {makeup_text} |")
        lines.append("")

    lines += [
        "## 3. 每月实际工作日",
        "",
        "计算口径：周一至周五为工作日；法定节假日（含落在周末的假期）不计；"
        "国务院安排的周末调休上班日计入。",
        "",
        "| 月份 | 2026 年 | 2027 年 |",
        "| --- | ---: | ---: |",
    ]
    total_2026 = 0
    total_2027 = 0
    for month in range(1, 13):
        c26 = count_workdays(2026, month)
        c27 = count_workdays(2027, month)
        total_2026 += c26
        total_2027 += c27
        lines.append(f"| {month} 月 | {c26} 天 | {c27} 天 |")
    lines += [
        f"| **全年** | **{total_2026} 天** | **{total_2027} 天** |",
        "",
        "2026 年和 2027 年均为 365 天，全年实际工作日均为 248 天。",
        "",
        "## 4. 完整日历",
        "",
    ]

    for year in (2026, 2027):
        lines += [f"### {year} 年", ""]
        for month in range(1, 13):
            lines += [
                f"#### {year} 年 {month} 月（{count_workdays(year, month)} 个工作日）",
                "",
                calendar_table(year, month),
                "",
            ]

    lines += [
        "## 5. 使用说明",
        "",
        "- Android 设置页打开“按当月实际工作日计薪”后，日薪 = 月薪 ÷ 当月实际工作日。",
        "- 默认仍按 21.75 天平均计算；两种方式可在设置页切换。",
        "- 实际工作日模式会同时识别法定节假日和调休上班日；节假日当天不计薪，"
        "调休上班日按工作日计薪。",
        "- 当前内置数据覆盖 2026—2027 年；其他年份按周一至周五回退计算，"
        "界面会提示“未收录该年节假日”。",
        "- 本文只用于薪资展示估算，不替代公司考勤、发薪或法律口径。",
        "",
    ]

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text("\n".join(lines), encoding="utf-8")
    print(f"wrote {OUT} ({OUT.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
