"""Generate the printable mock-up panel for a machine from the Unity machine catalog.

The buttons are drawn at the catalog's hotspot coordinates, so the print, the image-tracking
reference image and the AR arrows all share one source of truth.

ARCore only tracks images with many distinct corners spread across the whole image, so flat
areas are avoided: the background is a multi-scale polygon mosaic, controls are drawn
semi-transparent over it, and labels are drawn crisp on top. Check the score with --score.

Usage (from the repo root):
    python3 tools/mockup/generate_panel.py [machine-id] [--score]

Writes:
    unity-client/Assets/Machines/ReferenceImages/<referenceImage>.png   (image-tracking reference)
    documentation/mockups/<referenceImage>-print.pdf                  (A4 landscape, print at 100%)
"""

import glob
import json
import random
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "unity-client/Assets/Resources/Machines/machines.json"
IMAGE_DIR = ROOT / "unity-client/Assets/Machines/ReferenceImages"
PRINT_DIR = ROOT / "documentation/mockups"
ARCOREIMG_GLOB = str(ROOT / "unity-client/Library/PackageCache/com.unity.xr.arcore@*/Tools~/MacOS/arcoreimg")

FONT_BOLD = "/System/Library/Fonts/HelveticaNeue.ttc"
FONT_MONO = "/System/Library/Fonts/SFNSMono.ttf"

BUTTON_COLORS = {
    "power": (40, 160, 70),
    "start": (30, 150, 60),
    "stop": (200, 40, 40),
    "mode": (40, 90, 170),
    "ok": (230, 160, 20),
}
DEFAULT_BUTTON = (60, 60, 70)
CONTROL_ALPHA = 130  # 0-255: lower lets more texture through (better tracking, less solid look)
MIN_SCORE = 75       # Google's recommended minimum
GOOD_SCORE = 85      # stop searching variants once reached
SEARCH_VARIANTS = 8

A4_LANDSCAPE_MM = (297.0, 210.0)
PRINT_DPI = 300


def font(path, size, index=0):
    try:
        return ImageFont.truetype(path, size, index=index)
    except OSError:
        return ImageFont.load_default(size)


def bbox(hotspot, width, height):
    cx, cy = hotspot["x"] * width, hotspot["y"] * height
    hw, hh = hotspot["w"] * width / 2, hotspot["h"] * height / 2
    return [cx - hw, cy - hh, cx + hw, cy + hh]


def mosaic(width, height, seed):
    """Multi-scale, high-contrast polygon mosaic in steel greys with some blue/green tints:
    lots of unique corners at every scale (tuned against arcoreimg: 75-90/100)."""
    rng = random.Random(seed)
    img = Image.new("RGB", (width, height), (150, 150, 155))
    draw = ImageDraw.Draw(img)
    for _ in range(3000):
        size = rng.choice([300, 160, 80, 40, 20])
        cx, cy = rng.randrange(width), rng.randrange(height)
        points = [(cx + rng.uniform(-size, size), cy + rng.uniform(-size, size)) for _ in range(rng.randrange(3, 6))]
        v = rng.randrange(50, 240)
        if rng.random() < 0.35:
            color = (max(0, v - rng.randrange(40)), v, min(255, v + rng.randrange(60)))
        else:
            color = (v, v, v + 6)
        draw.polygon(points, fill=color)
    return img


def text_c(draw, center, text, f, fill=(255, 255, 255), stroke=(15, 15, 15), sw=4):
    tw = draw.textlength(text, font=f)
    draw.text((center[0] - tw / 2, center[1] - f.size * 0.55), text, font=f, fill=fill, stroke_width=sw, stroke_fill=stroke)


def draw_controls(layer, machine, width, height):
    """Control shapes only, drawn on a transparent layer that is composited semi-transparent."""
    d = ImageDraw.Draw(layer)
    a = CONTROL_ALPHA
    for h in machine["hotspots"]:
        x0, y0, x1, y1 = box = bbox(h, width, height)
        if h["kind"] == "screen":
            d.rounded_rectangle([x0 - 14, y0 - 14, x1 + 14, y1 + 14], radius=18, fill=(40, 40, 45, a))
            d.rectangle(box, fill=(10, 35, 25, a))
        elif h["kind"] == "emergency":
            pad = (x1 - x0) * 0.22
            d.ellipse([x0 - pad, y0 - pad, x1 + pad, y1 + pad], fill=(245, 200, 20, a), outline=(20, 20, 20, 255), width=6)
            d.ellipse(box, fill=(205, 25, 25, min(255, a + 30)), outline=(60, 0, 0, 255), width=8)
        elif h["kind"] == "indicator":
            cy, r = (y0 + y1) / 2, (y1 - y0) / 2
            for i, c in enumerate([(40, 200, 70), (240, 170, 20), (220, 40, 40)]):
                cx = x0 + r + i * (x1 - x0 - 2 * r) / 2
                d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=c + (255,), outline=(20, 20, 20, 255), width=4)
        else:
            color = BUTTON_COLORS.get(h["id"], DEFAULT_BUTTON)
            if h["shape"] == "circle":
                d.ellipse([x0 - 10, y0 - 10, x1 + 10, y1 + 10], outline=(20, 20, 20, 255), width=8)
                d.ellipse(box, fill=color + (a,), outline=(20, 20, 20, 255), width=6)
            else:
                d.rounded_rectangle([x0 - 8, y0 - 8, x1 + 8, y1 + 8], radius=22, outline=(20, 20, 20, 255), width=8)
                d.rounded_rectangle(box, radius=18, fill=color + (a,), outline=(20, 20, 20, 255), width=4)


def draw_labels(img, machine, width, height):
    d = ImageDraw.Draw(img)
    label = font(FONT_BOLD, 42, index=1)
    small = font(FONT_BOLD, 30, index=1)
    mono = font(FONT_MONO, 40)
    mono_small = font(FONT_MONO, 28)

    serial = next((s["value"] for s in machine["specs"] if s["label"] == "Serial number"), "")
    d.text((50, 30), machine["name"].upper(), font=font(FONT_BOLD, 70, index=1), fill=(255, 255, 255),
           stroke_width=6, stroke_fill=(20, 30, 60))
    d.text((width - 640, 50), f"S/N {serial}", font=small, fill=(255, 255, 255), stroke_width=3, stroke_fill=(20, 20, 20))

    for h in machine["hotspots"]:
        x0, y0, x1, y1 = bbox(h, width, height)
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
        if h["kind"] == "screen":
            lines = [
                machine["name"].upper(),
                "STATUS : READY      MODE : MANUAL",
                "PRESS. : 4.2 bar    TEMP : 23.5 C",
                "CYCLES : 001284     FW   : v3.4.1",
                "LAST   : E-12 cleared 18/09",
                "NEXT   : maintenance in 37 d",
            ]
            y = y0 + 26
            for i, t in enumerate(lines):
                d.text((x0 + 30, y), t, font=mono if i < 4 else mono_small, fill=(110, 240, 160))
                y += 58 if i < 4 else 46
            # pressure trend chart: irregular polyline gives unique corners
            rng = random.Random("trend")
            gx0, gy0, gx1, gy1 = x0 + 30, y1 - 150, x1 - 30, y1 - 24
            d.rectangle([gx0, gy0, gx1, gy1], outline=(70, 160, 110), width=2)
            pts, v = [], 0.5
            for i in range(40):
                v = min(0.95, max(0.05, v + rng.uniform(-0.2, 0.2)))
                pts.append((gx0 + i * (gx1 - gx0) / 39, gy1 - v * (gy1 - gy0)))
            d.line(pts, fill=(130, 255, 180), width=4)
        elif h["kind"] == "emergency":
            text_c(d, (cx, cy), "STOP", font(FONT_BOLD, 54, index=1))
            text_c(d, (cx, y1 + (x1 - x0) * 0.22 + 34), "EMERGENCY STOP", small, fill=(255, 230, 60))
        elif h["kind"] == "indicator":
            r = (y1 - y0) / 2
            for i, name in enumerate(["RUN", "WARN", "FAULT"]):
                lx = x0 + r + i * (x1 - x0 - 2 * r) / 2
                d.text((lx + r + 10, cy - 18), name, font=small, fill=(255, 255, 255), stroke_width=3, stroke_fill=(20, 20, 20))
        elif h["shape"] == "circle":
            text_c(d, (cx, cy), "I/O", label)
            text_c(d, (cx, y1 + 40), h["label"], small)
        else:
            arrow = {"up": -1, "down": 1}.get(h["id"])
            if arrow:  # drawn triangle: system fonts lack the arrow glyphs
                ty = cy - 44
                d.polygon([(cx - 18, ty - arrow * 14), (cx + 18, ty - arrow * 14), (cx, ty + arrow * 14)],
                          fill=(255, 255, 255), outline=(15, 15, 15))
                text_c(d, (cx, cy + 14), h["label"], label)
            else:
                text_c(d, (cx, cy), h["label"], label)

    d.rectangle([90, height - 140, 620, height - 72], fill=(245, 200, 20), outline=(20, 20, 20), width=4)
    d.text((110, height - 124), "HIGH PRESSURE  6.5 bar MAX", font=small, fill=(20, 20, 20))


def render(machine, variant=0):
    width, height = machine["imagePixelWidth"], machine["imagePixelHeight"]
    img = mosaic(width, height, seed=f"{machine['id']}-{variant}").convert("RGBA")
    layer = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    draw_controls(layer, machine, width, height)
    img = Image.alpha_composite(img, layer).convert("RGB")
    draw_labels(img, machine, width, height)
    return img


def score(png):
    tools = glob.glob(ARCOREIMG_GLOB)
    if not tools:
        return None
    out = subprocess.run([tools[0], "eval-img", f"--input_image_path={png}"], capture_output=True, text=True)
    try:
        return int(out.stdout.strip().splitlines()[-1])
    except (ValueError, IndexError):
        return None


def write_print_pdf(img, machine, path):
    page_w = round(A4_LANDSCAPE_MM[0] / 25.4 * PRINT_DPI)
    page_h = round(A4_LANDSCAPE_MM[1] / 25.4 * PRINT_DPI)
    page = Image.new("RGB", (page_w, page_h), "white")

    target_w = round(machine["physicalWidthMeters"] * 1000 / 25.4 * PRINT_DPI)
    target_h = round(machine["physicalHeightMeters"] * 1000 / 25.4 * PRINT_DPI)
    panel = img.resize((target_w, target_h), Image.LANCZOS)
    ox, oy = (page_w - target_w) // 2, (page_h - target_h) // 2 - 60
    page.paste(panel, (ox, oy))

    draw = ImageDraw.Draw(page)
    note = font(FONT_BOLD, 34)
    mm = PRINT_DPI / 25.4
    # 100 mm scale bar to check the print size
    sx, sy = ox, oy + target_h + 60
    draw.line([sx, sy, sx + 100 * mm, sy], fill="black", width=6)
    for t in range(11):
        x = sx + t * 10 * mm
        draw.line([x, sy - (24 if t % 5 == 0 else 12), x, sy], fill="black", width=4)
    draw.text((sx, sy + 16), "100 mm: measure this. If it is not 100 mm, reprint at 100% / actual size.", font=note, fill="black")
    draw.text(
        (sx, sy + 66),
        f"{machine['name']} mock-up. Panel must be {machine['physicalWidthMeters'] * 1000:.0f} x "
        f"{machine['physicalHeightMeters'] * 1000:.1f} mm. Print in colour, matte paper, no scaling.",
        font=note,
        fill="black",
    )
    page.save(path, "PDF", resolution=PRINT_DPI)


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    check = "--score" in sys.argv
    catalog = json.loads(CATALOG.read_text())
    wanted = args[0] if args else None
    machines = [m for m in catalog["machines"] if wanted in (None, m["id"])]
    if not machines:
        sys.exit(f"No machine '{wanted}' in {CATALOG}")

    IMAGE_DIR.mkdir(parents=True, exist_ok=True)
    PRINT_DIR.mkdir(parents=True, exist_ok=True)
    failed = False
    for machine in machines:
        png = IMAGE_DIR / f"{machine['referenceImage']}.png"
        pdf = PRINT_DIR / f"{machine['referenceImage']}-print.pdf"

        # Try a few background variants and keep the best ARCore score (deterministic for a given catalog).
        best_img, best_score, best_variant = None, None, 0
        for variant in range(SEARCH_VARIANTS if check else 1):
            img = render(machine, variant)
            img.save(png)
            s = score(png) if check else None
            if best_img is None or (s is not None and (best_score is None or s > best_score)):
                best_img, best_score, best_variant = img, s, variant
            if s is not None and s >= GOOD_SCORE:
                break

        best_img.save(png)
        write_print_pdf(best_img, machine, pdf)
        line = f"{machine['id']}: {png.relative_to(ROOT)}, {pdf.relative_to(ROOT)}"
        if check:
            line += f", variant {best_variant}, ARCore score {best_score if best_score is not None else 'n/a (arcoreimg not found)'}/100"
            failed |= best_score is not None and best_score < MIN_SCORE
        print(line)
    if failed:
        sys.exit(f"ARCore score below {MIN_SCORE}: tracking will be unreliable.")


if __name__ == "__main__":
    main()
