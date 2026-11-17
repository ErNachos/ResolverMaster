"""Render the Google Material Symbols (Rounded) used by Resolver Master into white PNG masks.

License: Material Symbols are Apache License 2.0 (see docs/crediti.md).
The variable font is downloaded once into Tools/Art/.cache/ (ignored by git).
Output: Assets/Art/Icons/<name>.png, white glyph on a transparent background, tinted at runtime by the UI.

Usage (from the project root):  python Tools/Art/make_icons.py
To add an icon: add a line to ICONS (asset name -> Material Symbols name) and run the script again.
"""
import os
import sys
import urllib.request

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CACHE = os.path.join(ROOT, "Tools", "Art", ".cache")
OUT = os.path.join(ROOT, "Assets", "Art", "Icons")
BASE = "https://raw.githubusercontent.com/google/material-design-icons/master/variablefont/"
FONT_FILE = "MaterialSymbolsRounded[FILL,GRAD,opsz,wght].ttf"
CODEPOINTS_FILE = "MaterialSymbolsRounded[FILL,GRAD,opsz,wght].codepoints"

SIZE = 128          # output texture size (px)
GLYPH = 112         # glyph em size inside the texture (px)
FILL = 1            # 1 = filled symbols (read better at small sizes)
WEIGHT = 500

# asset name -> Material Symbols name
ICONS = {
    "back": "arrow_back",
    "settings": "settings",
    "undo": "undo",
    "restart": "restart_alt",
    "help": "help",
    "star": "star",
    "lock": "lock",
    "sound": "volume_up",
    "vibration": "vibration",
    "language": "language",
    "story": "menu_book",
    "challenge": "bolt",
    "game_plumber": "plumbing",
    "game_trafficjam": "directions_car",
    # Plumber: chapter themes, "Open the water" button
    "water_drop": "water_drop",
    "fork": "call_split",
    "palette": "palette",
    "faucet": "faucet",
}


def fetch(name):
    os.makedirs(CACHE, exist_ok=True)
    path = os.path.join(CACHE, name)
    if not os.path.exists(path):
        url = BASE + urllib.request.quote(name)
        print("download", url)
        urllib.request.urlretrieve(url, path)
    return path


def main():
    font_path = fetch(FONT_FILE)
    codepoints = {}
    with open(fetch(CODEPOINTS_FILE), encoding="utf-8") as f:
        for line in f:
            parts = line.split()
            if len(parts) == 2:
                codepoints[parts[0]] = int(parts[1], 16)

    font = ImageFont.truetype(font_path, GLYPH)
    font.set_variation_by_axes([FILL, 0, 48, WEIGHT])  # Fill, Grade, Optical size, Weight
    os.makedirs(OUT, exist_ok=True)
    missing = []
    for asset, symbol in ICONS.items():
        if symbol not in codepoints:
            missing.append(symbol)
            continue
        ch = chr(codepoints[symbol])
        # draw on a large scratch canvas, then centre the ink (not the font box) in the output
        scratch = Image.new("L", (SIZE * 2, SIZE * 2), 0)
        ImageDraw.Draw(scratch).text((SIZE // 2, SIZE // 2), ch, fill=255, font=font)
        ink = scratch.crop(scratch.getbbox())
        img = Image.new("L", (SIZE, SIZE), 0)
        img.paste(ink, ((SIZE - ink.width) // 2, (SIZE - ink.height) // 2))
        rgba = Image.new("RGBA", (SIZE, SIZE), (255, 255, 255, 0))
        rgba.putalpha(img)
        rgba.save(os.path.join(OUT, asset + ".png"))
        print("icon", asset, "<-", symbol)
    if missing:
        print("MISSING symbols:", ", ".join(missing))
        sys.exit(1)


if __name__ == "__main__":
    main()
