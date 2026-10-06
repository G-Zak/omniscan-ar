"""Generate the printable mock-up panel for a machine from the Unity machine catalog.

The buttons are drawn at the catalog's hotspot coordinates, so the print, the image-tracking
reference image and the AR arrows all share one source of truth.

Usage (from the repo root):
    python3 tools/mockup/generate_panel.py [machine-id]

Writes:
    unity-client/Assets/Machines/ReferenceImages/<referenceImage>.png   (image-tracking reference)
    documentation/mockups/<referenceImage>-print.pdf                  (A4 landscape, print at 100%)
"""

import json
import random
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "unity-client/Assets/Resources/Machines/machines.json"
IMAGE_DIR = ROOT / "unity-client/Assets/Machines/ReferenceImages"
PRINT_DIR = ROOT / "documentation/mockups"

FONT_BOLD = "/System/Library/Fonts/HelveticaNeue.ttc"
FONT_MONO = "/System/Library/Fonts/SFNSMono.ttf"

KIND_COLORS = {
    "power": (40, 160, 70),
    "start": (30, 150, 60),
    "stop": (200, 40, 40),
    "mode": (40, 90, 170),
    "ok": (230, 160, 20),
    "menu": (70, 70, 80),
    "up": (70, 70, 80),
    "down": (70, 70, 80),
}

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


def brushed_metal(width, height, seed):
    """Non-repeating textured background: image tracking needs lots of distinct features."""
    rng = random.Random(seed)
    base = Image.new("L", (width, height), 175)
    noise = Image.effect_noise((width, height), 28).filter(ImageFilter.BoxBlur(1))
    metal = Image.blend(base, noise, 0.35).convert("RGB")
    draw = ImageDraw.Draw(metal)
    for _ in range(900):
        y = rng.randrange(height)
        x0 = rng.randrange(width)
        length = rng.randrange(80, 400)
        shade = rng.randrange(140, 210)
        draw.line([(x0, y), (x0 + length, y)], fill=(shade, shade, shade + 4), width=1)
    return metal


def draw_screw(draw, cx, cy, r):
    draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(120, 120, 125), outline=(60, 60, 60), width=3)
    draw.line([cx - r * 0.6, cy - r * 0.2, cx + r * 0.6, cy + r * 0.2], fill=(50, 50, 50), width=4)


def draw_screen(draw, box, machine):
    x0, y0, x1, y1 = box
    draw.rounded_rectangle([x0 - 14, y0 - 14, x1 + 14, y1 + 14], radius=18, fill=(45, 45, 50))
    draw.rectangle(box, fill=(18, 40, 32))
    mono = font(FONT_MONO, 46)
    small = font(FONT_MONO, 34)
    lines = [
        (f"{machine['name'].upper()}", mono),
        ("STATUS : READY", mono),
        ("MODE   : MANUAL", mono),
        ("PRESS. : 4.2 bar", mono),
        ("TEMP.  : 23.5 C", mono),
        ("CYCLES : 001284", small),
        ("FW v3.4.1   NO FAULTS", small),
    ]
    y = y0 + 30
    for text, f in lines:
        draw.text((x0 + 36, y), text, font=f, fill=(90, 230, 140))
        y += 72 if f is mono else 58
    # pressure bar graph, adds unique structure for tracking
    bx0, by = x1 - 230, y0 + 60
    for i in range(10):
        h = 22 + i * 18
        fill = (90, 230, 140) if i < 6 else (40, 90, 60)
        draw.rectangle([bx0 + i * 20, by + 220 - h, bx0 + i * 20 + 12, by + 220], fill=fill)


def draw_button(draw, hotspot, box, label_font):
    x0, y0, x1, y1 = box
    color = KIND_COLORS.get(hotspot["id"], (70, 70, 80))
    if hotspot["shape"] == "circle":
        draw.ellipse([x0 - 10, y0 - 10, x1 + 10, y1 + 10], fill=(40, 40, 40))
        draw.ellipse(box, fill=color, outline=(20, 20, 20), width=6)
        inset = (x1 - x0) * 0.18
        draw.ellipse([x0 + inset, y0 + inset, x1 - inset, y1 - inset], outline=(255, 255, 255), width=5)
    else:
        draw.rounded_rectangle([x0 - 8, y0 - 8, x1 + 8, y1 + 8], radius=22, fill=(35, 35, 35))
        draw.rounded_rectangle(box, radius=18, fill=color, outline=(20, 20, 20), width=5)
    label = hotspot["label"]
    tw = draw.textlength(label, font=label_font)
    if hotspot["shape"] == "circle":
        draw.text(((x0 + x1 - tw) / 2, y1 + 18), label, font=label_font, fill=(25, 25, 25))
        return
    arrow = {"up": -1, "down": 1}.get(hotspot["id"])
    tri = 30 if arrow else 0
    tx = (x0 + x1 - tw - tri - (12 if arrow else 0)) / 2
    cy = (y0 + y1) / 2
    if arrow:  # drawn triangle: system fonts lack the arrow glyphs
        ax = tx + tri / 2
        tip, base = cy + arrow * 15, cy - arrow * 15
        draw.polygon([(ax - tri / 2, base), (ax + tri / 2, base), (ax, tip)], fill=(255, 255, 255))
        tx += tri + 12
    draw.text((tx, cy - 22), label, font=label_font, fill=(255, 255, 255))


def draw_estop(draw, box, label_font):
    x0, y0, x1, y1 = box
    pad = (x1 - x0) * 0.22
    draw.ellipse([x0 - pad, y0 - pad, x1 + pad, y1 + pad], fill=(245, 200, 20), outline=(30, 30, 30), width=6)
    draw.ellipse(box, fill=(205, 25, 25), outline=(60, 0, 0), width=8)
    draw.ellipse([x0 + 30, y0 + 30, x1 - 30, y1 - 30], outline=(255, 120, 120), width=6)
    text = "EMERGENCY STOP"
    tw = draw.textlength(text, font=label_font)
    draw.text(((x0 + x1 - tw) / 2, y1 + pad + 6), text, font=label_font, fill=(25, 25, 25))


def draw_leds(draw, box, label_font):
    x0, y0, x1, y1 = box
    cy = (y0 + y1) / 2
    r = (y1 - y0) / 2
    for i, (color, name) in enumerate([((40, 200, 70), "RUN"), ((240, 170, 20), "WARN"), ((220, 40, 40), "FAULT")]):
        cx = x0 + r + i * (x1 - x0 - 2 * r) / 2
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=color, outline=(30, 30, 30), width=4)
        draw.text((cx + r + 12, cy - 18), name, font=label_font, fill=(25, 25, 25))


def render(machine):
    width, height = machine["imagePixelWidth"], machine["imagePixelHeight"]
    img = brushed_metal(width, height, seed=machine["id"])
    draw = ImageDraw.Draw(img)

    title = font(FONT_BOLD, 64, index=1)
    label_font = font(FONT_BOLD, 40, index=1)
    small = font(FONT_BOLD, 30)

    # header strip with brand and serial: asymmetric detail helps tracking lock orientation
    draw.rectangle([0, 0, width, 120], fill=(30, 45, 80))
    draw.text((50, 26), machine["name"].upper(), font=title, fill=(255, 255, 255))
    serial = next((s["value"] for s in machine["specs"] if s["label"] == "Serial number"), "")
    draw.text((width - 700, 44), f"S/N {serial}", font=small, fill=(200, 210, 230))
    for i in range(24):  # hazard stripes on the right of the header
        x = width - 1560 + i * 28
        draw.polygon([(x, 120), (x + 14, 120), (x + 44, 0), (x + 30, 0)], fill=(245, 200, 20) if i % 2 else (30, 45, 80))

    for cx, cy in [(40, 160), (width - 40, 160), (40, height - 40), (width - 40, height - 40)]:
        draw_screw(draw, cx, cy, 20)

    for hotspot in machine["hotspots"]:
        box = bbox(hotspot, width, height)
        if hotspot["kind"] == "screen":
            draw_screen(draw, box, machine)
        elif hotspot["kind"] == "emergency":
            draw_estop(draw, box, label_font)
        elif hotspot["kind"] == "indicator":
            draw_leds(draw, box, small)
        else:
            draw_button(draw, hotspot, box, label_font)

    # warning sticker: more unique features for tracking
    draw.rectangle([90, height - 140, 620, height - 72], fill=(245, 200, 20), outline=(20, 20, 20), width=4)
    draw.text((110, height - 124), "HIGH PRESSURE  6.5 bar MAX", font=small, fill=(20, 20, 20))
    return img


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
    catalog = json.loads(CATALOG.read_text())
    wanted = sys.argv[1] if len(sys.argv) > 1 else None
    machines = [m for m in catalog["machines"] if wanted in (None, m["id"])]
    if not machines:
        sys.exit(f"No machine '{wanted}' in {CATALOG}")

    IMAGE_DIR.mkdir(parents=True, exist_ok=True)
    PRINT_DIR.mkdir(parents=True, exist_ok=True)
    for machine in machines:
        img = render(machine)
        png = IMAGE_DIR / f"{machine['referenceImage']}.png"
        pdf = PRINT_DIR / f"{machine['referenceImage']}-print.pdf"
        img.save(png)
        write_print_pdf(img, machine, pdf)
        print(f"{machine['id']}: {png.relative_to(ROOT)}, {pdf.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
