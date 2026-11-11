# -*- coding: utf-8 -*-
"""Render the static UI concept board for the Salary Monitor design."""
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

OUT_DIR = os.path.dirname(os.path.abspath(__file__))
U = 2  # supersampling: 1600x1080 design units -> 3200x2160 output
W, H = 1600 * U, 1080 * U

FONT_DIR = r"C:\Windows\Fonts"
FONT_PATHS = {
    "cn": os.path.join(FONT_DIR, "msyh.ttc"),
    "cnb": os.path.join(FONT_DIR, "msyhbd.ttc"),
    "ui": os.path.join(FONT_DIR, "segoeui.ttf"),
    "uisb": os.path.join(FONT_DIR, "seguisb.ttf"),
    "uib": os.path.join(FONT_DIR, "segoeuib.ttf"),
    "mono": os.path.join(FONT_DIR, "consola.ttf"),
    "monob": os.path.join(FONT_DIR, "consolab.ttf"),
}
_font_cache = {}


def F(style, size):
    size = max(1, int(round(size)))
    key = (style, size)
    if key not in _font_cache:
        _font_cache[key] = ImageFont.truetype(FONT_PATHS[style], size, index=0)
    return _font_cache[key]


def rgba(hex_value, alpha=255):
    h = hex_value.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4)) + (alpha,)


def mix(c1, c2, t):
    return tuple(int(c1[i] + (c2[i] - c1[i]) * t) for i in range(4))


def vertical_gradient(size, c1, c2):
    w, h = size
    arr = np.zeros((h, w, 4), dtype=np.uint8)
    for y in range(h):
        t = y / max(1, h - 1)
        arr[y, :, :] = mix(c1, c2, t)
    return Image.fromarray(arr, "RGBA")


def diagonal_gradient(size, c1, c2):
    w, h = size
    yy, xx = np.mgrid[0:h, 0:w]
    t = (xx / max(1, w - 1) * 0.28 + yy / max(1, h - 1) * 0.72)
    arr = np.zeros((h, w, 4), dtype=np.uint8)
    for i in range(4):
        arr[:, :, i] = (c1[i] + (c2[i] - c1[i]) * t).astype(np.uint8)
    return Image.fromarray(arr, "RGBA")


def rounded_mask(size, radius):
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, size[0] - 1, size[1] - 1],
                                        radius=radius, fill=255)
    return m


def draw_tracked(draw, xy, text, font, fill, tracking=0, anchor="lt"):
    """Draw text with extra letter spacing (for the small uppercase labels)."""
    if not text:
        return
    # anchor only supports left here; callers use it for left labels
    x, y = xy
    for ch in text:
        draw.text((x, y), ch, font=font, fill=fill, anchor="lt")
        x += font.getlength(ch) + tracking


def create_card(w, h, radius, glow_a=(34, 211, 238, 46), glow_b=(99, 102, 241, 38)):
    card = diagonal_gradient((w, h), rgba("#101c2f"), rgba("#0a1421"))
    glow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse([int(-w * 0.25), int(-h * 0.3), int(w * 0.55), int(h * 0.45)],
               fill=glow_a)
    gd.ellipse([int(w * 0.55), int(h * 0.55), int(w * 1.2), int(h * 1.15)],
               fill=glow_b)
    glow = glow.filter(ImageFilter.GaussianBlur(70 * U))
    card = Image.alpha_composite(card, glow)
    return card


def paste_card(board, card, x, y, radius, shadow_alpha=180):
    sw = card.width + 220 * U
    sh = card.height + 220 * U
    shadow = Image.new("RGBA", (sw, sh), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    sd.rounded_rectangle(
        [110 * U, 118 * U, 110 * U + card.width, 118 * U + card.height],
        radius=radius, fill=(0, 0, 0, shadow_alpha)
    )
    shadow = shadow.filter(ImageFilter.GaussianBlur(42 * U))
    board.alpha_composite(shadow, (x - 110 * U, y - 110 * U))
    board.alpha_composite(card, (x, y))
    # 1px light edge
    edge = Image.new("RGBA", (card.width, card.height), (0, 0, 0, 0))
    ImageDraw.Draw(edge).rounded_rectangle(
        [0, 0, card.width - 1, card.height - 1],
        radius=radius, outline=(255, 255, 255, 30), width=max(2, int(1.1 * U))
    )
    board.alpha_composite(edge, (x, y))


def draw_text(draw, xy, text, font, fill, anchor="lt", spacing=0):
    if spacing:
        draw_tracked(draw, xy, text, font, fill, tracking=spacing, anchor=anchor)
    else:
        draw.text(xy, text, font=font, fill=fill, anchor=anchor)


def gradient_text(layer, x, y, text, font, c1, c2, pad=8):
    """Paste a horizontal-gradient version of text onto an RGBA layer."""
    tb = font.getbbox(text, anchor="lt")
    tw = int(math.ceil(font.getlength(text))) + pad * 2
    th = int(math.ceil(tb[3] - tb[1])) + pad * 2
    mask = Image.new("L", (tw, th), 0)
    md = ImageDraw.Draw(mask)
    md.text((pad, pad - tb[1]), text, font=font, fill=255, anchor="lt")
    grad = Image.new("RGBA", (tw, th))
    arr = np.zeros((th, tw, 4), dtype=np.uint8)
    yy, xx = np.mgrid[0:th, 0:tw]
    t = np.clip(xx / max(1, tw - 1), 0, 1)
    for i in range(3):
        arr[:, :, i] = (c1[i] + (c2[i] - c1[i]) * t).astype(np.uint8)
    arr[:, :, 3] = 255
    grad = Image.fromarray(arr, "RGBA")
    grad.putalpha(mask)
    layer.alpha_composite(grad, (int(x - pad), int(y - pad)))


def icon_pin(d, cx, cy, s, color, width):
    # simple push-pin
    d.line([(cx, cy + 0.15 * s), (cx, cy + 0.46 * s)], fill=color, width=width)
    d.polygon([
        (cx - 0.16 * s, cy - 0.43 * s),
        (cx + 0.16 * s, cy - 0.43 * s),
        (cx + 0.16 * s, cy - 0.10 * s),
        (cx + 0.38 * s, cy + 0.12 * s),
        (cx + 0.30 * s, cy + 0.24 * s),
        (cx - 0.30 * s, cy + 0.24 * s),
        (cx - 0.38 * s, cy + 0.12 * s),
        (cx - 0.16 * s, cy - 0.10 * s),
    ], outline=color, width=width)


def icon_settings(d, cx, cy, s, color, w):
    for yy, knob in ((-0.36, 0.22), (0.0, -0.26), (0.36, 0.12)):
        y = cy + yy * s
        d.line([(cx - 0.45 * s, y), (cx + 0.45 * s, y)], fill=color, width=w)
        kx = cx + knob * s
        r = 0.10 * s
        d.ellipse([kx - r, y - r, kx + r, y + r], fill=(10, 20, 34, 255),
                  outline=color, width=w)


def icon_minus(d, cx, cy, s, color, w):
    d.line([(cx - 0.35 * s, cy), (cx + 0.35 * s, cy)], fill=color, width=w)


def icon_close(d, cx, cy, s, color, w):
    d.line([(cx - 0.33 * s, cy - 0.33 * s), (cx + 0.33 * s, cy + 0.33 * s)],
           fill=color, width=w)
    d.line([(cx + 0.33 * s, cy - 0.33 * s), (cx - 0.33 * s, cy + 0.33 * s)],
           fill=color, width=w)


def draw_main_window():
    scale = 1.2
    base_w, base_h = 460, 480
    w, h = int(round(base_w * scale * U)), int(round(base_h * scale * U))
    radius = int(round(26 * scale * U))
    card = create_card(w, h, radius)
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    S = scale * U

    def P(x, y):
        return (x * S, y * S)

    def fs(px):
        return px * S

    def panel(x, y, pw, ph, r, fill=(255, 255, 255, 11),
              outline=(255, 255, 255, 20), width=1.0):
        d.rounded_rectangle(
            [P(x, y), P(x + pw, y + ph)],
            radius=r * S, fill=fill, outline=outline, width=max(1, int(width * S))
        )

    # top edge highlight
    top = Image.new("RGBA", (w, int(3 * S)), (0, 0, 0, 0))
    td = ImageDraw.Draw(top)
    td.rectangle([0, 0, w, int(2.3 * S)], fill=(0, 0, 0, 0))
    for i in range(0, w, max(1, int(2 * S))):
        t = i / max(1, w - 1)
        alpha = int(70 * math.sin(math.pi * min(1, max(0, t))) ** 1.4)
        td.line([(i, 0), (i, int(1.1 * S))], fill=(103, 232, 249, alpha))

    # ---------- header ----------
    mark_x, mark_y, mark_s = 24, 22, 31
    mark_box = [P(mark_x, mark_y), P(mark_x + mark_s, mark_y + mark_s)]
    d.rounded_rectangle(mark_box, radius=10 * S, fill=(22, 55, 72, 210),
                        outline=(165, 243, 252, 45), width=max(1, int(1.2 * S)))
    d.text(P(mark_x + mark_s / 2, mark_y + mark_s / 2 - 0.5),
           "¥", font=F("monob", fs(15)), fill=rgba("#ECFEFF"), anchor="mm")
    d.ellipse([P(mark_x + mark_s - 8, mark_y + 5),
               P(mark_x + mark_s - 3, mark_y + 10)],
              fill=rgba("#67E8F9"))
    d.text(P(65, 24), "薪资显示器", font=F("cnb", fs(12)), fill=rgba("#F8FAFC"))
    draw_tracked(d, P(65, 41), "SALARY MONITOR", F("ui", fs(7)),
                 rgba("#54667c"), tracking=1.3 * S)

    actions = [
        ("pin", 316, True),
        ("settings", 348, False),
        ("minus", 380, False),
        ("close", 412, False),
    ]
    for kind, ax, active in actions:
        bx = P(ax, 24)
        bw = fs(27)
        if active:
            d.rounded_rectangle([bx[0], bx[1], bx[0] + bw, bx[1] + bw],
                                radius=fs(8), fill=(34, 211, 238, 30),
                                outline=(103, 232, 249, 42), width=max(1, int(S)))
        cx, cy = ax + 13.5, 24 + 13.5
        col = rgba("#A5F3FC") if active else rgba("#7C8EA5")
        wcol = rgba("#FCA5A5") if kind == "close" else col
        if kind == "pin":
            icon_pin(d, *P(cx, cy), fs(13), col, max(1, int(1.4 * S)))
        elif kind == "settings":
            icon_settings(d, *P(cx, cy), fs(13), col, max(1, int(1.4 * S)))
        elif kind == "minus":
            icon_minus(d, *P(cx, cy), fs(13), col, max(1, int(1.4 * S)))
        else:
            icon_close(d, *P(cx, cy), fs(13), wcol, max(1, int(1.4 * S)))

    # ---------- status row ----------
    panel(24, 73, 93, 23, 11.5, fill=(45, 212, 191, 19),
          outline=(45, 212, 191, 32))
    dx, dy = P(35, 84.5)
    d.ellipse([dx - 2.5 * S, dy - 2.5 * S, dx + 2.5 * S, dy + 2.5 * S],
              fill=rgba("#2DD4BF"))
    d.ellipse([dx - 5.2 * S, dy - 5.2 * S, dx + 5.2 * S, dy + 5.2 * S],
              outline=(45, 212, 191, 90), width=max(1, int(0.9 * S)))
    d.text(P(46, 78), "下午工作中", font=F("cn", fs(9)), fill=rgba("#99F6E4"))

    time_font = F("mono", fs(9))
    time_text = "16:28:35"
    time_w = time_font.getlength(time_text) / S
    d.text(P(436, 78), time_text, font=time_font, fill=rgba("#A8B7C9"), anchor="rt")
    d.text(P(436 - time_w - 8, 78), "11月5日 · 周四", font=F("cn", fs(9)),
           fill=rgba("#76869B"), anchor="rt")

    # ---------- hero ----------
    d.text(P(24, 102), "今日已赚", font=F("cn", fs(10)), fill=rgba("#91A2B6"))
    mini_text = "按分钟累计"
    mf = F("cn", fs(7))
    mini_w = mf.getlength(mini_text) / S
    panel(74, 99.5, mini_w + 15, 17, 8.5, fill=(129, 140, 248, 20),
          outline=(129, 140, 248, 36))
    d.text(P(81.5, 103), mini_text, font=mf, fill=rgba("#C7D2FE"))

    d.text(P(24, 155), "¥", font=F("ui", fs(23)), fill=rgba("#B9F5F8"))
    gradient_text(layer, P(45, 128)[0], P(45, 128)[1], "1,052.49",
                  F("uisb", fs(52)), (255, 255, 255), (165, 243, 252), pad=int(4 * S))

    # ---------- rate strip ----------
    panel(24, 206, 412, 49, 13, fill=(255, 255, 255, 10),
          outline=(255, 255, 255, 17))
    for i in range(1, 3):
        xx = 24 + i * 412 / 3
        d.line([P(xx, 215), P(xx, 246)], fill=(255, 255, 255, 16),
               width=max(1, int(0.8 * S)))
    labels = [("日薪", "¥1,540.23"), ("时薪", "¥192.53"), ("分钟薪", "¥3.21")]
    for i, (lab, val) in enumerate(labels):
        lx = 24 + i * 412 / 3 + 13
        d.text(P(lx, 215), lab, font=F("cn", fs(8)), fill=rgba("#7F91A7"))
        d.text(P(lx, 230), val, font=F("mono", fs(12)), fill=rgba("#DBEAFE"))

    # divider
    d.line([P(24, 266), P(436, 266)], fill=(255, 255, 255, 18),
           width=max(1, int(0.8 * S)))

    # ---------- countdown ----------
    d.rounded_rectangle([P(24, 279), P(436, 361)], radius=15 * S,
                        fill=(14, 35, 48, 150),
                        outline=(103, 232, 249, 28), width=max(1, int(S)))
    # left accent glow
    accent = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    ad = ImageDraw.Draw(accent)
    ad.ellipse([P(-30, 297), P(120, 352)], fill=(34, 211, 238, 18))
    accent = accent.filter(ImageFilter.GaussianBlur(int(18 * S)))
    layer.alpha_composite(accent)
    d = ImageDraw.Draw(layer)

    # clock glyph
    ccx, ccy = P(43, 296)
    d.arc([ccx - 6 * S, ccy - 6 * S, ccx + 6 * S, ccy + 6 * S],
          start=0, end=360, fill=rgba("#67E8F9"), width=max(1, int(1.2 * S)))
    d.line([ccx, ccy, ccx, ccy - 3 * S], fill=rgba("#67E8F9"), width=max(1, int(1.2 * S)))
    d.line([ccx, ccy, ccx + 2.8 * S, ccy + 1.6 * S], fill=rgba("#67E8F9"),
           width=max(1, int(1.2 * S)))
    d.text(P(54, 291), "距离下班", font=F("cn", fs(10)), fill=rgba("#CBD5E1"))
    d.text(P(422, 291), "下班时间 19:00", font=F("cn", fs(8)),
           fill=rgba("#718399"), anchor="rt")

    # timer with accent colons
    timer_font = F("mono", fs(34))
    tx = 38
    ty = 308
    for piece in ["02", ":", "31", ":", "25"]:
        color = rgba("#67E8F9") if piece == ":" else rgba("#FFFFFF")
        d.text(P(tx, ty), piece, font=timer_font, fill=color, anchor="lt")
        tx += timer_font.getlength(piece) / S - (1.2 if piece != ":" else 0.2)

    d.text(P(422, 310), "剩余有效工时", font=F("cn", fs(8)),
           fill=rgba("#7F91A7"), anchor="rt")
    d.text(P(422, 323), "2 小时 32 分", font=F("cn", fs(11)),
           fill=rgba("#C9F7F7"), anchor="rt")

    # ---------- progress ----------
    d.text(P(24, 374), "今日进度 68.3%", font=F("cn", fs(8)), fill=rgba("#7F91A7"))
    d.text(P(436, 374), "328 / 480 分钟", font=F("cn", fs(8)),
           fill=rgba("#B8C6D8"), anchor="rt")
    d.rounded_rectangle([P(24, 387), P(436, 393)], radius=3 * S,
                        fill=(255, 255, 255, 18))
    fill_w = 412 * 0.683
    grad_w = int(round(fill_w * S))
    grad_h = int(round(6 * S))
    bar = Image.new("RGBA", (max(2, grad_w), max(2, grad_h)))
    arr = np.zeros((grad_h, grad_w, 4), dtype=np.uint8)
    for x in range(grad_w):
        t = x / max(1, grad_w - 1)
        if t < 0.52:
            c = mix(rgba("#22D3EE"), rgba("#2DD4BF"), t / 0.52)
        else:
            c = mix(rgba("#2DD4BF"), rgba("#818CF8"), (t - 0.52) / 0.48)
        arr[:, x, :] = c
    bar = Image.fromarray(arr, "RGBA")
    bar.putalpha(rounded_mask((bar.width, bar.height), radius=int(3 * S)))
    glow_dot = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    gdd = ImageDraw.Draw(glow_dot)
    gdd.ellipse([P(24 + fill_w - 4, 387), P(24 + fill_w + 4, 393)],
                fill=(214, 255, 251, 230))
    glow_dot = glow_dot.filter(ImageFilter.GaussianBlur(int(4 * S)))
    layer.alpha_composite(glow_dot)
    layer.alpha_composite(bar, (int(24 * S), int(387 * S)))
    d.ellipse([P(24 + fill_w - 2.3, 389.3), P(24 + fill_w + 2.3, 394.7)],
              fill=rgba("#D6FFFB"))

    # ---------- footer ----------
    d.text(P(24, 451), "按设置估算 · 以实际薪资为准", font=F("cn", fs(7)),
           fill=rgba("#536479"))
    d.ellipse([P(362, 452.5), P(366.5, 457)], fill=rgba("#2DD4BF"))
    d.text(P(372, 451), "每分钟更新工资", font=F("cn", fs(7)), fill=rgba("#6E8298"))

    layer.alpha_composite(top, (0, 0))
    card = Image.alpha_composite(card, layer)
    card.putalpha(rounded_mask((w, h), radius))
    return card, int(142 * U), int(182 * U), radius


def draw_settings_window():
    scale = 1.2
    base_w, base_h = 560, 620
    w, h = int(round(base_w * scale * U)), int(round(base_h * scale * U))
    radius = int(round(26 * scale * U))
    card = create_card(w, h, radius, glow_a=(34, 211, 238, 34),
                       glow_b=(99, 102, 241, 34))
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    S = scale * U

    def P(x, y):
        return (x * S, y * S)

    def fs(px):
        return px * S

    def panel(x, y, pw, ph, r, fill=(255, 255, 255, 10),
              outline=(255, 255, 255, 19), width=0.9):
        d.rounded_rectangle([P(x, y), P(x + pw, y + ph)], radius=r * S,
                             fill=fill, outline=outline,
                             width=max(1, int(width * S)))

    # header
    draw_tracked(d, P(26, 24), "PREFERENCES", F("ui", fs(8)),
                 rgba("#67E8F9"), tracking=2.4 * S)
    d.text(P(26, 43), "设置", font=F("cnb", fs(27)), fill=rgba("#F8FAFC"))
    d.text(P(26, 81), "参数保存后立即生效，今日工资将按新规则重新估算",
           font=F("cn", fs(9)), fill=rgba("#7F91A7"))
    # close button
    d.rounded_rectangle([P(507, 24), P(534, 51)], radius=8 * S,
                        fill=(255, 255, 255, 5), outline=(255, 255, 255, 16),
                        width=max(1, int(0.8 * S)))
    icon_close(d, *P(520.5, 37.5), fs(13), rgba("#7F91A7"),
               max(1, int(1.4 * S)))

    # ---------------- section 1 ----------------
    d.text(P(26, 111), "收入参数", font=F("cn", fs(10)), fill=rgba("#C5D2E1"))
    draw_tracked(d, P(84, 113), "INCOME", F("ui", fs(7)),
                 rgba("#536479"), tracking=1.2 * S)
    panel(26, 128, 508, 104, 13, fill=(255, 255, 255, 9))

    # tinted calculation footer
    d.rounded_rectangle([P(26, 187), P(534, 232)], radius=13 * S,
                        corners=(False, False, True, True),
                        fill=(34, 211, 238, 15))

    # field row
    d.line([P(280, 128), P(280, 187)], fill=(255, 255, 255, 16),
           width=max(1, int(0.8 * S)))
    # shells first, then the values on top
    d.rounded_rectangle([P(40, 151), P(266, 181)], radius=8 * S,
                        fill=(3, 8, 17, 90), outline=(103, 232, 249, 65),
                        width=max(1, int(1.0 * S)))
    d.rounded_rectangle([P(294, 151), P(520, 181)], radius=8 * S,
                        fill=(3, 8, 17, 90), outline=(255, 255, 255, 18),
                        width=max(1, int(0.8 * S)))
    d.text(P(40, 140), "月薪", font=F("cn", fs(8)), fill=rgba("#7F91A7"))
    d.text(P(40, 154), "33,500.00", font=F("mono", fs(12)), fill=rgba("#F1F7FB"))
    d.text(P(254, 154), "元 / 月", font=F("cn", fs(8)), fill=rgba("#60748A"), anchor="rt")
    d.text(P(294, 140), "月计薪天数", font=F("cn", fs(8)), fill=rgba("#7F91A7"))
    d.text(P(294, 154), "21.75", font=F("mono", fs(12)), fill=rgba("#F1F7FB"))
    d.text(P(508, 154), "天", font=F("cn", fs(8)), fill=rgba("#60748A"), anchor="rt")

    # calc strip text and borders
    d.line([P(26, 187), P(534, 187)], fill=(103, 232, 249, 24),
           width=max(1, int(0.8 * S)))
    d.rounded_rectangle([P(26, 128), P(534, 232)], radius=13 * S,
                        outline=(255, 255, 255, 17), width=max(1, int(0.8 * S)))
    d.text(P(40, 202), "日薪 ¥1,540.23　·　分钟薪 ¥3.21",
           font=F("cn", fs(8)), fill=rgba("#86A3B6"))
    d.text(P(520, 202), "自动计算", font=F("cn", fs(8)), fill=rgba("#64748B"), anchor="rt")

    # ---------------- section 2 ----------------
    d.text(P(26, 254), "工作时间", font=F("cn", fs(10)), fill=rgba("#C5D2E1"))
    draw_tracked(d, P(84, 256), "SCHEDULE", F("ui", fs(7)),
                 rgba("#536479"), tracking=1.2 * S)
    panel(26, 272, 508, 160, 13, fill=(255, 255, 255, 9))
    # tinted calculation footer
    d.rounded_rectangle([P(26, 394), P(534, 432)], radius=13 * S,
                        corners=(False, False, True, True),
                        fill=(22, 56, 68, 190))
    # grid
    d.line([P(280, 272), P(280, 394)], fill=(255, 255, 255, 15),
           width=max(1, int(0.8 * S)))
    d.line([P(26, 333), P(534, 333)], fill=(255, 255, 255, 13),
           width=max(1, int(0.8 * S)))

    def field(x, y, label, value, unit):
        d.rounded_rectangle([P(x, y + 10), P(x + 226, y + 42)], radius=8 * S,
                            fill=(3, 8, 17, 90), outline=(255, 255, 255, 17),
                            width=max(1, int(0.8 * S)))
        d.text(P(x, y), label, font=F("cn", fs(8)), fill=rgba("#7F91A7"))
        d.text(P(x, y + 14), value, font=F("mono", fs(12)), fill=rgba("#F1F7FB"))
        d.text(P(x + 214, y + 14), unit, font=F("cn", fs(8)),
               fill=rgba("#60748A"), anchor="rt")

    field(40, 284, "上班时间", "09:30", "上午")
    field(294, 284, "下班时间", "19:00", "晚上")
    field(40, 345, "午休开始", "12:30", "中午")
    field(294, 345, "午休结束", "14:00", "下午")

    # calc strip text and border
    d.line([P(26, 394), P(534, 394)], fill=(255, 255, 255, 16),
           width=max(1, int(0.8 * S)))
    d.rounded_rectangle([P(26, 272), P(534, 432)], radius=13 * S,
                        outline=(255, 255, 255, 17), width=max(1, int(0.8 * S)))
    d.text(P(40, 408), "每日有效工时 8 小时 0 分", font=F("cn", fs(8)),
           fill=rgba("#B5F5F4"))
    d.text(P(520, 408), "仅周六、周日不计薪", font=F("cn", fs(8)),
           fill=rgba("#64748B"), anchor="rt")

    # ---------------- section 3 ----------------
    d.text(P(26, 452), "窗口", font=F("cn", fs(10)), fill=rgba("#C5D2E1"))
    draw_tracked(d, P(66, 454), "WINDOW", F("ui", fs(7)),
                 rgba("#536479"), tracking=1.2 * S)
    panel(26, 470, 508, 58, 13, fill=(255, 255, 255, 9))
    d.text(P(40, 484), "窗口置顶", font=F("cn", fs(9)), fill=rgba("#B9C7D7"))
    d.text(P(40, 500), "让薪资卡片始终浮在其他窗口上方",
           font=F("cn", fs(8)), fill=rgba("#66788E"))
    # toggle
    d.rounded_rectangle([P(480, 489), P(514, 508)], radius=10 * S,
                        fill=(13, 148, 136, 255), outline=(45, 212, 191, 90),
                        width=max(1, int(0.9 * S)))
    d.ellipse([P(496, 491), P(512, 506)], fill=rgba("#F0FDFA"))

    # footer buttons
    d.line([P(26, 548), P(534, 548)], fill=(255, 255, 255, 15),
           width=max(1, int(0.8 * S)))
    d.text(P(26, 574), "恢复默认值", font=F("cn", fs(9)), fill=rgba("#718399"))
    # cancel ghost
    d.rounded_rectangle([P(376, 564), P(448, 597)], radius=9 * S,
                        fill=(255, 255, 255, 6), outline=(255, 255, 255, 18),
                        width=max(1, int(0.8 * S)))
    d.text(P(412, 580.5), "取消", font=F("cn", fs(9)), fill=rgba("#94A3B8"), anchor="mm")
    # save primary
    d.rounded_rectangle([P(458, 564), P(534, 597)], radius=9 * S,
                        fill=(45, 212, 191, 235), outline=(165, 243, 252, 70),
                        width=max(1, int(0.9 * S)))
    d.text(P(496, 580.5), "保存设置", font=F("cnb", fs(9)), fill=rgba("#042A31"), anchor="mm")

    # header hairline
    top = Image.new("RGBA", (w, int(3 * S)), (0, 0, 0, 0))
    td = ImageDraw.Draw(top)
    for x in range(w):
        t = x / max(1, w - 1)
        alpha = int(70 * math.sin(math.pi * t) ** 1.5)
        td.line([(x, 0), (x, max(1, int(1.1 * S)))],
                fill=(103, 232, 249, alpha))
    layer.alpha_composite(top, (0, 0))

    card = Image.alpha_composite(card, layer)
    card.putalpha(rounded_mask((w, h), radius))
    return card, int(786 * U), int(182 * U), radius


def build_board():
    # background: gradient + soft colour fields
    bg = vertical_gradient((W, H), rgba("#060c16"), rgba("#050a13"))
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse([int(-0.22 * W), int(0.38 * H), int(0.35 * W), int(1.02 * H)],
               fill=(8, 145, 178, 39))
    gd.ellipse([int(0.68 * W), int(-0.24 * H), int(1.22 * W), int(0.52 * H)],
               fill=(79, 70, 229, 44))
    gd.ellipse([int(0.30 * W), int(0.65 * H), int(0.82 * W), int(1.12 * H)],
               fill=(13, 148, 136, 25))
    glow = glow.filter(ImageFilter.GaussianBlur(int(180 * U)))
    bg = Image.alpha_composite(bg, glow)

    # subtle grid
    grid = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    gdr = ImageDraw.Draw(grid)
    step = 56 * U
    for x in range(0, W, step):
        gdr.line([(x, 0), (x, H)], fill=(148, 163, 184, 7), width=1)
    for y in range(0, H, step):
        gdr.line([(0, y), (W, y)], fill=(148, 163, 184, 7), width=1)
    # fade grid at edges
    fade = Image.new("L", (W, H), 0)
    fd = ImageDraw.Draw(fade)
    fd.ellipse([int(0.02 * W), int(0.03 * H), int(0.98 * W), int(0.98 * H)],
               fill=255)
    fade = fade.filter(ImageFilter.GaussianBlur(int(110 * U)))
    grid.putalpha(Image.composite(grid.getchannel("A"), Image.new("L", (W, H), 0), fade))
    bg = Image.alpha_composite(bg, grid)

    # ---------- board typography ----------
    d = ImageDraw.Draw(bg)
    margin = int(82 * U)
    draw_tracked(d, (margin, int(48 * U)), "SALARY MONITOR  ·  UI CONCEPT 01",
                 F("ui", 11 * U), rgba("#67E8F9"), tracking=3.0 * U)
    # small rule
    d.line([(margin, int(68 * U)), (margin + int(23 * U), int(68 * U))],
           fill=(103, 232, 249, 120), width=max(1, int(1 * U)))
    d.text((margin, int(78 * U)), "薪资显示器", font=F("cnb", 38 * U),
           fill=rgba("#F8FAFC"))
    d.text((margin, int(127 * U)),
           "深色玻璃卡片 · 青蓝渐变 · 桌面常驻 · 一眼看清今天赚了多少、还有多久下班",
           font=F("cn", 13 * U), fill=rgba("#94A3B8"))

    # meta chips, right aligned; opaque fills because the board is the final canvas
    chip_font = F("cn", 10 * U)
    chip_mix_font = F("cn", 9 * U)
    chip_sub_font = F("mono", 10 * U)
    gap = int(10 * U)
    chip_y = int(48 * U)
    chip_h = int(34 * U)
    right = int(1516 * U)
    widths = [int(222 * U), int(166 * U), int(138 * U)]
    c3_x = right - widths[2]
    c2_x = c3_x - gap - widths[1]
    c1_x = c2_x - gap - widths[0]
    for cx, cw, label in ((c1_x, widths[0], "默认排班"), (c2_x, widths[1], "¥33,500"), (c3_x, widths[2], "ALWAYS ON TOP")):
        d.rounded_rectangle([cx, chip_y, cx + cw, chip_y + chip_h],
                            radius=10 * U, fill=(13, 22, 36, 255),
                            outline=(38, 51, 68, 255), width=1)
    # chip 1
    dot_y = chip_y + chip_h // 2
    d.ellipse([c1_x + 14 * U, dot_y - 3 * U, c1_x + 20 * U, dot_y + 3 * U],
              fill=rgba("#2DD4BF"))
    d.text((c1_x + 28 * U, chip_y + 10 * U), "默认排班", font=chip_font,
           fill=rgba("#94A3B8"))
    d.text((c1_x + 28 * U + chip_font.getlength("默认排班") + 8 * U, chip_y + 10 * U),
           "09:30 → 19:00", font=chip_sub_font, fill=rgba("#E2E8F0"))
    # chip 2
    d.text((c2_x + 14 * U, chip_y + 10 * U), "¥33,500", font=chip_mix_font,
           fill=rgba("#E2E8F0"))
    d.text((c2_x + 14 * U + chip_mix_font.getlength("¥33,500") + 8 * U, chip_y + 10 * U),
           "/ 21.75 天", font=chip_mix_font, fill=rgba("#94A3B8"))
    # chip 3
    d.text((c3_x + widths[2] // 2, dot_y), "ALWAYS ON TOP", font=F("ui", 8.5 * U),
           fill=rgba("#A5F3FC"), anchor="mm")

    main_card, mx, my, mr = draw_main_window()
    settings_card, sx, sy, sr = draw_settings_window()
    paste_card(bg, main_card, mx, my, mr)
    paste_card(bg, settings_card, sx, sy, sr)

    d = ImageDraw.Draw(bg)
    # captions
    cap_y = int(782 * U)
    d.text((int(142 * U), cap_y), "主界面", font=F("cn", 11 * U), fill=rgba("#CBD5E1"))
    draw_tracked(d, (int(196 * U), cap_y + int(2 * U)), "MAIN · 460 × 480 DIP",
                 F("ui", 8 * U), rgba("#536479"), tracking=1.6 * U)
    draw_tracked(d, (int(658 * U), cap_y), "01", F("ui", 9 * U),
                 rgba("#67E8F9"), tracking=2.0 * U)

    cap2_y = int(952 * U)
    d.text((int(786 * U), cap2_y), "设置页", font=F("cn", 11 * U), fill=rgba("#CBD5E1"))
    draw_tracked(d, (int(840 * U), cap2_y + int(2 * U)), "SETTINGS · 560 × 620 DIP",
                 F("ui", 8 * U), rgba("#536479"), tracking=1.6 * U)
    draw_tracked(d, (int(1424 * U), cap2_y), "02", F("ui", 9 * U),
                 rgba("#67E8F9"), tracking=2.0 * U)

    # board footer
    footer_y = int(1037 * U)
    d.line([(margin, footer_y), (W - margin, footer_y)],
           fill=(30, 42, 57, 255), width=1)
    d.text((margin, footer_y + int(14 * U)), "CONCEPT PREVIEW  ·  方案稿，待确认后进入编码",
           font=F("cn", 9 * U), fill=rgba("#536479"))
    legend_x = int(820 * U)
    legend_items = [
        "今日已赚按完整计薪分钟累计",
        "午休金额暂停 / 倒计时继续",
        "周末显示休息日",
    ]
    for item in legend_items:
        d.ellipse([legend_x, footer_y + int(18 * U),
                   legend_x + int(5 * U), footer_y + int(23 * U)],
                  fill=rgba("#2DD4BF"))
        d.text((legend_x + int(10 * U), footer_y + int(12 * U)), item,
               font=F("cn", 8 * U), fill=rgba("#7F91A7"))
        legend_x += int(198 * U)

    return bg, (mx, my, main_card.width, main_card.height), (sx, sy, settings_card.width, settings_card.height)


def main():
    board, main_box, settings_box = build_board()
    # Keep a crisp 2x master and a friendlier 2400px-wide board.
    board_path = os.path.join(OUT_DIR, "ui-concept.png")
    board_hd_path = os.path.join(OUT_DIR, "ui-concept@2x.png")
    board.save(board_hd_path)
    preview = board.resize((2400, int(round(2400 * H / W))), Image.LANCZOS)
    preview.save(board_path, optimize=True)

    # Detail crops
    def crop(box, name, width=1800):
        x, y, w, h = box
        pad = int(22 * U)
        im = board.crop((max(0, x - pad), max(0, y - pad),
                         min(board.width, x + w + pad), min(board.height, y + h + pad)))
        if im.width > width:
            im = im.resize((width, int(round(im.height * width / im.width))), Image.LANCZOS)
        im.save(os.path.join(OUT_DIR, name), optimize=True)

    crop(main_box, "ui-main.png")
    crop(settings_box, "ui-settings.png")
    print("WROTE", board_path)
    print("WROTE", board_hd_path)
    print("WROTE", os.path.join(OUT_DIR, "ui-main.png"))
    print("WROTE", os.path.join(OUT_DIR, "ui-settings.png"))
    print("SIZE", board.size)


if __name__ == "__main__":
    main()
