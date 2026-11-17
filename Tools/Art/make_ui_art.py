"""Cut the Gemini UI sheets into single sprites for the game, and prepare the Home card illustrations.

Input (read only, never modified):
    ArtSource/UI/ui_1_pulsanti.png ... ui_8_icone_speciali.png   (several elements per sheet, on a white background)
    ArtSource/Cards/*.png                                        (one illustration per minigame, on a white background)
Output:
    Assets/Art/UI/<name>.png          one PNG per element (btn_green, panel, tile_level, icon_back, star_full, ...)
    Assets/Art/UI/ui_art.json         size, source sheet and 9-slice border (left, bottom, right, top) of every element
    Assets/Art/Cards/card_<id>.png    Home card illustrations

For every sheet:
  1. the white background is removed starting from the borders (flood fill); white holes fully closed by the black outline
     (inside of the empty star, hole of the gear, shackle of the lock...) become transparent too, while the light highlights
     (surrounded by the body color, not by the outline) stay;
  2. the elements are the groups of visible pixels separated by white space (pieces closer than MERGE_GAP px belong to the
     same element: the waves of the speaker, the X of "sound off"); tiny specks are ignored;
  3. elements are named in reading order (rows from the top, then left to right) with the names of SHEETS;
  4. each element is cropped with a small transparent margin and saved at its own resolution (never scaled up or down).
If the number of elements of a sheet differs from SHEETS the script stops with an error (nothing is half-written: the
elements of a sheet are written only after the whole sheet was checked).

Writes are safe (temporary file, then replace); the script can be run again at any time.
Usage (from the project root):  python Tools/Art/make_ui_art.py
"""
import glob
import json
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC_UI = os.path.join(ROOT, "ArtSource", "UI")
SRC_CARDS = os.path.join(ROOT, "ArtSource", "Cards")
OUT_UI = os.path.join(ROOT, "Assets", "Art", "UI")
OUT_CARDS = os.path.join(ROOT, "Assets", "Art", "Cards")

WHITE_MIN = 232      # a background pixel has every channel >= this (Gemini whites are a little noisy)
FRINGE_MAX = 200     # light pixels touching the background get a partial alpha (anti-aliased outline)
MERGE_GAP = 26       # pieces closer than this (px) are one element
MIN_PIXELS = 400     # smaller groups are noise
PAD = 3              # transparent margin around a cropped element (px)
HOLE_MIN = 150       # enclosed white areas at least this big, ringed by the black outline, become transparent
OUTLINE_LUM = 75     # luminance below this = black outline
CARD_MAX = 1024      # Home card illustrations: at most this side (only scaled down)

# sheet -> element names in reading order. "slice" = how the game stretches it:
#   "pill": 9-slice, the round ends stay intact, only the middle stretches (the game keeps the height = no vertical stretch)
#   "box":  9-slice for rounded rectangles (corners and the bottom band intact)
#   None:   never sliced (icons, round buttons, the ribbon: scaled uniformly)
SHEETS = {
    "ui_1_pulsanti.png": [("btn_green", "pill"), ("btn_cream", "pill"), ("btn_round_cream", None), ("btn_round_yellow", None)],
    "ui_2_pannelli.png": [("panel", "box"), ("ribbon", None), ("banner_chapter", "box")],
    "ui_3_caselle.png": [("tile_level", "box"), ("tile_done", "box"), ("tile_locked", "box")],
    "ui_4_icone_navigazione.png": [("icon_back", None), ("icon_next", None), ("icon_home", None), ("icon_close", None)],
    "ui_5_icone_gioco.png": [("icon_undo", None), ("icon_restart", None), ("icon_help", None), ("icon_check", None)],
    "ui_6_icone_stelle.png": [("star_full", None), ("star_empty", None), ("icon_lock", None), ("icon_settings", None)],
    "ui_7_icone_impostazioni.png": [("icon_sound_on", None), ("icon_sound_off", None), ("icon_vibration", None), ("icon_language", None)],
    "ui_8_icone_speciali.png": [("icon_faucet", None), ("icon_trophy", None), ("icon_bolt", None), ("icon_calendar", None)],
}

# Home cards: (file name prefix in ArtSource/Cards, case-insensitive) -> output name (= GameInfo.CardImage)
CARDS = [("idraul", "card_plumber"), ("traf", "card_trafficjam")]


def remove_background(rgb):
    """rgb: HxWx3 float32. Returns (alpha 0..1, fringe mask, holes made transparent)."""
    lum = rgb.mean(axis=2)
    whiteish = np.all(rgb >= WHITE_MIN, axis=2)
    labels, count = ndimage.label(whiteish)
    border = np.unique(np.concatenate([labels[0, :], labels[-1, :], labels[:, 0], labels[:, -1]]))
    border = border[border != 0]
    background = np.isin(labels, border)

    # closed white holes: ringed (mostly) by the black outline
    holes = 0
    sizes = ndimage.sum(np.ones_like(lum), labels, index=np.arange(1, count + 1))
    for lab in range(1, count + 1):
        if lab in border or sizes[lab - 1] < HOLE_MIN:
            continue
        region = labels == lab
        ring = ndimage.binary_dilation(region, iterations=3) & ~region
        if ring.any() and (lum[ring] < OUTLINE_LUM).mean() > 0.55:
            background |= region
            holes += 1

    alpha = np.where(background, 0.0, 1.0).astype(np.float32)
    near_bg = ndimage.binary_dilation(background, iterations=2) & ~background
    fringe = near_bg & (lum > FRINGE_MAX)
    alpha[fringe] = np.clip((255.0 - lum[fringe]) / (255.0 - FRINGE_MAX), 0.0, 1.0)
    return alpha, fringe, holes


def find_elements(alpha):
    """Groups of visible pixels separated by white space, in reading order. Returns a list of (y0, y1, x0, x1, mask)."""
    visible = alpha > 0.02
    grown = ndimage.binary_dilation(visible, iterations=MERGE_GAP // 2)
    labels, count = ndimage.label(grown)
    found = []
    for lab in range(1, count + 1):
        mask = (labels == lab) & visible
        if mask.sum() < MIN_PIXELS:
            continue
        ys, xs = np.nonzero(mask)
        found.append((ys.min(), ys.max() + 1, xs.min(), xs.max() + 1, mask))
    # reading order: rows (centres closer than a quarter of the element height go together), then left to right
    found.sort(key=lambda e: (e[0] + e[1]) / 2)
    rows = []
    for e in found:
        cy = (e[0] + e[1]) / 2
        if rows and abs(cy - rows[-1][0]) < 0.25 * (e[1] - e[0]):
            rows[-1][1].append(e)
        else:
            rows.append([cy, [e]])
    ordered = []
    for _, row in rows:
        ordered.extend(sorted(row, key=lambda e: e[2]))
    return ordered


def border_for(kind, w, h, rgba):
    """9-slice border (left, bottom, right, top) in px for an element of the given kind (0 = not sliced)."""
    if kind is None:
        return [0, 0, 0, 0]
    a = rgba[..., 3]
    # bottom band: rows from the bottom up to the end of the darker band are kept intact
    if kind == "pill":
        half = h // 2
        # keep the highlight near the left end intact: the stretched middle starts after it
        left = max(half, int(round(w * 0.30)))
        return [left, half, max(half, int(round(w * 0.12))), h - half - 1]
    # rounded box: corners + highlight on the top-left + the bottom band
    left = max(int(round(min(w, h) * 0.22)), int(round(w * 0.30)))
    right = int(round(min(w, h) * 0.22))
    top = int(round(min(w, h) * 0.22))
    bottom = int(round(min(w, h) * 0.26))
    left = min(left, w // 2 - 2)
    right = min(right, w - left - 4)
    top = min(top, h // 2 - 2)
    bottom = min(bottom, h - top - 4)
    return [left, bottom, right, top]


def save_safe(img, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    tmp = path + ".tmp"
    img.save(tmp, format="PNG")
    os.replace(tmp, path)


def cut_sheet(path, names):
    rgb = np.asarray(Image.open(path).convert("RGB"), dtype=np.float32)
    alpha, fringe, holes = remove_background(rgb)
    base = rgb.copy()
    base[fringe] = 0.0                 # the fringe is the outline fading out: keep it dark
    base[alpha <= 0.0] = 0.0           # transparent pixels black: no light halo when filtered
    elements = find_elements(alpha)
    if len(elements) != len(names):
        return None, f"{os.path.basename(path)}: found {len(elements)} elements, expected {len(names)} ({', '.join(n for n, _ in names)})"
    out = []
    H, W = alpha.shape
    for (y0, y1, x0, x1, mask), (name, kind) in zip(elements, names):
        y0p, y1p, x0p, x1p = max(0, y0 - PAD), min(H, y1 + PAD), max(0, x0 - PAD), min(W, x1 + PAD)
        a = (alpha * mask)[y0p:y1p, x0p:x1p]
        rgba = np.dstack([base[y0p:y1p, x0p:x1p], a * 255.0]).clip(0, 255).astype(np.uint8)
        h, w = rgba.shape[:2]
        out.append({"name": name, "img": Image.fromarray(rgba, "RGBA"), "w": w, "h": h,
                    "border": border_for(kind, w, h, rgba), "slice": kind or "none",
                    "source": os.path.relpath(path, ROOT).replace("\\", "/"), "at": [int(x0), int(y0)]})
    return (out, holes), None


def prepare_card(path, out_name):
    rgb = np.asarray(Image.open(path).convert("RGB"), dtype=np.float32)
    alpha, fringe, _ = remove_background(rgb)
    base = rgb.copy()
    base[fringe] = 0.0
    base[alpha <= 0.0] = 0.0
    ys, xs = np.nonzero(alpha > 0.02)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    rgba = np.dstack([base, alpha * 255.0]).clip(0, 255).astype(np.uint8)[y0:y1, x0:x1]
    img = Image.fromarray(rgba, "RGBA")
    if max(img.size) > CARD_MAX:
        k = CARD_MAX / max(img.size)
        img = img.convert("RGBa").resize((round(img.size[0] * k), round(img.size[1] * k)), Image.LANCZOS).convert("RGBA")
    save_safe(img, os.path.join(OUT_CARDS, out_name + ".png"))
    return img.size, (x1 - x0, y1 - y0)


def main():
    problems = []
    results = []
    for sheet, names in SHEETS.items():
        path = os.path.join(SRC_UI, sheet)
        if not os.path.exists(path):
            problems.append(f"{sheet}: missing in ArtSource/UI")
            continue
        res, err = cut_sheet(path, names)
        if err:
            problems.append(err)
            continue
        results.append((sheet, res))
    if problems:
        print("PROBLEMS (nothing written):")
        for p in problems:
            print("  - " + p)
        sys.exit(1)

    manifest = []
    for sheet, (elements, holes) in results:
        print(f"{sheet}: {len(elements)} elements" + (f" ({holes} closed white hole(s) made transparent)" if holes else ""))
        for e in elements:
            save_safe(e["img"], os.path.join(OUT_UI, e["name"] + ".png"))
            b = e["border"]
            print(f"  {e['name']:<18} {e['w']:>4} x {e['h']:<4} px  slice {e['slice']:<4}" + (f"  border L{b[0]} B{b[1]} R{b[2]} T{b[3]}" if any(b) else ""))
            manifest.append({k: e[k] for k in ("name", "w", "h", "slice", "border", "source", "at")})
    tmp = os.path.join(OUT_UI, "ui_art.json.tmp")
    with open(tmp, "w", encoding="utf-8", newline="\n") as f:
        json.dump({"generatedBy": "Tools/Art/make_ui_art.py", "elements": manifest}, f, indent=1)
        f.write("\n")
    os.replace(tmp, os.path.join(OUT_UI, "ui_art.json"))

    files = glob.glob(os.path.join(SRC_CARDS, "*.png"))
    for prefix, out_name in CARDS:
        match = [f for f in files if os.path.basename(f).lower().startswith(prefix)]
        if len(match) != 1:
            problems.append(f"Cards: expected one image starting with '{prefix}', found {len(match)}")
            continue
        size, crop = prepare_card(match[0], out_name)
        print(f"card {os.path.basename(match[0])} -> {out_name}.png {size[0]} x {size[1]} px (object {crop[0]} x {crop[1]} px)")
    if problems:
        print("PROBLEMS:")
        for p in problems:
            print("  - " + p)
        sys.exit(1)
    print(f"OK: {len(manifest)} UI elements")


if __name__ == "__main__":
    main()
