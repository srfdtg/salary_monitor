# -*- coding: utf-8 -*-
"""Generate the in-app header icon for the Android client.

The launcher icon is an adaptive icon.  Drawing that adaptive icon directly
inside the dashboard header adds its square background layer around the
rounded artwork (visible as dark/black corners).  This script exports a plain
transparent-corner bitmap for the header instead.
"""
import os

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(ROOT, "assets", "app-icon.png")
RES = os.path.join(ROOT, "android", "app", "res")
SIZES = {
    "mdpi": 48,
    "hdpi": 72,
    "xhdpi": 96,
    "xxhdpi": 144,
    "xxxhdpi": 192,
}


def main():
    source = Image.open(SOURCE).convert("RGBA")
    pixels = np.asarray(source, dtype=np.float32)
    alpha = pixels[:, :, 3:4] / 255.0
    premultiplied = np.concatenate(
        [pixels[:, :, :3] * alpha, pixels[:, :, 3:4]], axis=2
    )
    premultiplied = Image.fromarray(
        np.clip(premultiplied + 0.5, 0, 255).astype(np.uint8), "RGBA"
    )

    for density, size in SIZES.items():
        resized = premultiplied.resize((size, size), Image.Resampling.LANCZOS)
        values = np.asarray(resized, dtype=np.float32)
        alpha = values[:, :, 3:4]
        # Undo the premultiplication.  Keeping a true zero in the transparent
        # corners prevents a dark fringe when the icon is scaled on screen.
        rgb = np.divide(
            values[:, :, :3],
            alpha / 255.0,
            out=np.zeros_like(values[:, :, :3]),
            where=alpha > 0.0,
        )
        rgba = np.concatenate(
            [np.clip(rgb + 0.5, 0, 255), alpha], axis=2
        ).astype(np.uint8)
        output_dir = os.path.join(RES, "drawable-" + density)
        os.makedirs(output_dir, exist_ok=True)
        output = os.path.join(output_dir, "header_icon.png")
        Image.fromarray(rgba, "RGBA").save(output, optimize=True)
        print("WROTE", output)


if __name__ == "__main__":
    main()
