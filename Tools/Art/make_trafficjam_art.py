"""Prepare the Traffic jam vehicles and obstacles drawn with Gemini for the game (TJ-002).

Input (read only, never modified): ArtSource/TJ/1.png .. 4.png, white background, objects side by side:
    1: red car worried, red car happy, blue car worried, blue car happy   (target cars)
    2: yellow car, lilac car, grey car, beige car
    3: dark grey truck, yellow truck, lilac truck
    4: barrier, manhole, rock, trash   (obstacles)
In the originals the front of every vehicle points DOWN.
Output: Assets/Art/TrafficJam/<Name>.png (names in NAMES below; the game reads them from the TrafficJamArt sprite library).

For every object:
  1. the white background is removed starting from the borders only (flood fill): the white inside (eyes, stripes, the truck's puff,
     paper) stays; the anti-aliased fringe between the outline and the background gets a partial alpha and stays dark (no white halo);
  2. the objects of a sheet are separated by the empty columns between them and cropped to their pixels (mirrors, wheels, the
     manhole lid that leaves its base: all kept whole);
  3. vehicles are turned by 180 degrees: in the game every vehicle is drawn with its front UP (the board turns it towards its way);
  4. vehicles are lengthened to their slot on the grid (car 2 cells, truck 3 cells, minus the gap around a vehicle) WITHOUT scaling:
     the rows of one flat band are repeated (cars: the roof between the windows, at most +CAR_MAX_STRETCH; trucks: the flat cargo
     box, as much as needed), so the cab, the face and the outline stay exactly as drawn. Never scaled up or down.
Writes are safe (temporary file, then replace); the script can be run again at any time.
Usage (from the project root):  python Tools/Art/make_trafficjam_art.py
"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sprite_cut import crop, load_sheet, save_safe, split_columns  # noqa: E402  (shared with every minigame, PG-002)

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "ArtSource", "TJ")
OUT = os.path.join(ROOT, "Assets", "Art", "TrafficJam")

# Slot of a vehicle on the grid, in cells: the vehicle is drawn inside (length - 2 * GAP) x (1 - 2 * GAP); keep in sync with
# TrafficJamConfig.vehicleGap (the game fits the sprite uniformly, so a small difference only leaves a little more room).
GAP = 0.06
CAR_MAX_STRETCH = 0.18  # cars: at most +18% of length (prompt: 15-20%)
BAND_DIFF = 6.0         # a row of a flat band differs from the next by at most this (mean over the object, 0..255)

NAMES = {
    "1": ["Target_Red_Worried", "Target_Red_Happy", "Target_Blue_Worried", "Target_Blue_Happy"],
    "2": ["Car_Yellow", "Car_Lilac", "Car_Grey", "Car_Beige"],
    "3": ["Truck_Grey", "Truck_Yellow", "Truck_Lilac"],
    "4": ["Obstacle_Barrier", "Obstacle_Manhole", "Obstacle_Rock", "Obstacle_Trash"],
}








def flat_band(rgba, lo, hi):
    """The longest run of rows in [lo, hi) where every row is almost equal to the next one (a flat part of the drawing).
    Returns (start, end) of the run."""
    rgb = rgba[..., :3].astype(np.float32)
    a = rgba[..., 3:4].astype(np.float32) / 255.0
    best, run_start = (lo, lo), None
    for r in range(lo, hi):
        d = np.abs(rgb[r] * a[r] - rgb[r + 1] * a[r + 1]).mean() if r + 1 < len(rgb) else 999.0
        flat = d <= BAND_DIFF
        if flat and run_start is None:
            run_start = r
        if (not flat or r == hi - 1) and run_start is not None:
            end = r if not flat else r + 1
            if end - run_start > best[1] - best[0]:
                best = (run_start, end)
            run_start = None
    return best


def lengthen(rgba, band, extra):
    """Repeats the rows of band = (start, end) so the image gets `extra` rows longer (nearest-row resampling of the band only)."""
    s, e = band
    n = e - s
    if extra <= 0 or n <= 0:
        return rgba
    idx = np.floor(np.arange(n + extra) * n / (n + extra)).astype(int) + s
    return np.concatenate([rgba[:s], rgba[idx], rgba[e:]], axis=0)


def body_color(rgba):
    """The most common opaque color of a vehicle (its paint), for TrafficJamConfig.vehicleColors."""
    px = rgba[rgba[..., 3] > 250][:, :3].astype(int)
    lum = px.mean(axis=1)
    px = px[(lum > 60) & (lum < 245)]
    q = (px // 8) * 8 + 4
    keys, counts = np.unique(q[:, 0] * 65536 + q[:, 1] * 256 + q[:, 2], return_counts=True)
    k = keys[counts.argmax()]
    sel = px[(q[:, 0] * 65536 + q[:, 1] * 256 + q[:, 2]) == k]
    return tuple(int(round(v)) for v in sel.mean(axis=0))




def main():
    problems = []
    for sheet, names in NAMES.items():
        path = os.path.join(SRC, sheet + ".png")
        if not os.path.exists(path):
            problems.append(f"missing {os.path.relpath(path, ROOT)}")
            continue
        rgba_sheet, alpha = load_sheet(path)
        ranges = split_columns(alpha)
        print(f"{os.path.relpath(path, ROOT)} {alpha.shape[1]}x{alpha.shape[0]}: {len(ranges)} object(s) at columns {ranges}")
        if len(ranges) != len(names):
            problems.append(f"sheet {sheet}: expected {len(names)} objects, found {len(ranges)}")
            continue
        for (x0, x1), name in zip(ranges, names):
            obj = crop(rgba_sheet[:, x0:x1])
            h0, w0 = obj.shape[:2]
            line = f"  {name}: {w0}x{h0}"
            if sheet != "4":
                obj = np.ascontiguousarray(obj[::-1, ::-1])   # front up
                cells = 3 if name.startswith("Truck") else 2
                want = int(round(w0 * (cells - 2 * GAP) / (1 - 2 * GAP)))   # length that fills the slot at the same width
                extra = want - h0
                if cells == 2:
                    extra = min(extra, int(h0 * CAR_MAX_STRETCH))
                    band = flat_band(obj, int(h0 * 0.30), int(h0 * 0.75))   # the roof (front is up: the face is in the top part)
                else:
                    band = flat_band(obj, int(h0 * 0.45), int(h0 * 0.97))   # the cargo box (behind the cab)
                if extra > 0 and band[1] - band[0] < 6:
                    problems.append(f"{name}: no flat band to lengthen (found rows {band})")
                obj = lengthen(obj, band, extra)
                h1 = obj.shape[0]
                line += f" -> {w0}x{h1} (band rows {band[0]}-{band[1]}, +{max(0, extra)} rows = +{100.0 * max(0, extra) / h0:.0f}%," \
                        f" aspect {h1 / w0:.2f}, slot {(cells - 2 * GAP) / (1 - 2 * GAP):.2f}), paint #{'%02X%02X%02X' % body_color(obj)}"
            save_safe(obj, os.path.join(OUT, name + ".png"))
            print(line)
    if problems:
        print("PROBLEMS:")
        for p in problems:
            print("  - " + p)
        sys.exit(1)
    print("OK")


if __name__ == "__main__":
    main()
