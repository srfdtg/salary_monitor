# -*- coding: utf-8 -*-
"""Generate the application icon (multi-size .ico) for Salary Monitor."""
import os
from PIL import Image, ImageDraw, ImageFilter, ImageFont
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSETS = os.path.join(ROOT, "assets")
os.makedirs(ASSETS, exist_ok=True)
SIZE = 1024
img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))

# Rounded gradient base.
base = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
pixels = np.zeros((SIZE, SIZE, 4), dtype=np.uint8)
yy, xx = np.mgrid[0:SIZE, 0:SIZE]
t = np.clip((xx / SIZE) * 0.40 + (yy / SIZE) * 0.60, 0, 1)
c1 = np.array([15, 30, 49], dtype=float)
c2 = np.array([6, 18, 30], dtype=float)
for i in range(3):
    pixels[:, :, i] = (c1[i] + (c2[i] - c1[i]) * t).astype(np.uint8)
pixels[:, :, 3] = 255
base = Image.fromarray(pixels, "RGBA")

mask = Image.new("L", (SIZE, SIZE), 0)
mask_draw = ImageDraw.Draw(mask)
margin = 18
mask_draw.rounded_rectangle([margin, margin, SIZE - margin, SIZE - margin],
                            radius=230, fill=255)
base.putalpha(mask)

# Soft internal glow.
glow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
gd = ImageDraw.Draw(glow)
gd.ellipse([-240, -230, 720, 580], fill=(34, 211, 238, 84))
gd.ellipse([520, 520, 1220, 1250], fill=(99, 102, 241, 82))
glow = glow.filter(ImageFilter.GaussianBlur(120))
glow.putalpha(Image.composite(glow.getchannel("A"), Image.new("L", (SIZE, SIZE), 0), mask))
base = Image.alpha_composite(base, glow)

d = ImageDraw.Draw(base)

# Thin rim.
d.rounded_rectangle([margin, margin, SIZE - margin, SIZE - margin],
                    radius=230, outline=(150, 243, 252, 80), width=6)

# Ring + tick mark.
ring = [278, 260, 746, 728]
d.arc(ring, start=205, end=335, fill=(34, 211, 238, 150), width=22)
d.arc(ring, start=338, end=520, fill=(45, 212, 191, 110), width=22)
d.ellipse([700, 260, 752, 312], fill=(103, 232, 249, 255))
d.ellipse([718, 278, 734, 294], fill=(231, 255, 255, 255))

# Yen glyph.
font_path = r"C:\Windows\Fonts\msyhbd.ttc"
font = ImageFont.truetype(font_path, 430, index=0)
text = "¥"
bbox = d.textbbox((0, 0), text, font=font)
tw = bbox[2] - bbox[0]
th = bbox[3] - bbox[1]
tx = (SIZE - tw) / 2 - bbox[0]
ty = 280 - bbox[1] + (470 - th) / 2

# Keep a faint glow behind the glyph.
glow_text = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
gtd = ImageDraw.Draw(glow_text)
gtd.text((tx, ty), text, font=font, fill=(103, 232, 249, 190))
glow_text = glow_text.filter(ImageFilter.GaussianBlur(26))
base = Image.alpha_composite(base, glow_text)
d = ImageDraw.Draw(base)
d.text((tx, ty), text, font=font, fill=(239, 253, 255, 255))

# Baseline coin/clock accents.
d.rounded_rectangle([330, 770, 694, 790], radius=10, fill=(103, 232, 249, 60))
d.ellipse([330, 768, 354, 792], fill=(45, 212, 191, 230))
d.ellipse([670, 768, 694, 792], fill=(129, 140, 248, 230))

# Preserve transparent corners.
base.putalpha(mask)

out_png = os.path.join(ASSETS, "app-icon.png")
out_ico = os.path.join(ASSETS, "app.ico")
base.save(out_png)
base.save(out_ico, format="ICO", sizes=[(16, 16), (24, 24), (32, 32),
                                        (48, 48), (64, 64), (128, 128), (256, 256)])
print("WROTE", out_png)
print("WROTE", out_ico)
