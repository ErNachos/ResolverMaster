"""Prepare the Pino drawings for the game (PG-002), with the shared cutter (sprite_cut.py, also used by Traffic jam).

Input (read only, never modified): ArtSource/PG/1.png .. 6.png, white background, objects side by side (left to right):
    1: orange penguin seen from the front: normal, happy, scared, dizzy (ErNa-012: dizzy no longer used, see SKIP)
    2: purple striped penguin seen from the front: normal, happy, scared, dizzy
    3: seen from above, head up: orange still, orange sliding on its belly, purple still, purple sliding
    4: igloo: closed with orange flag, open orange, closed purple, open purple
    5: fish, ice block, rock, block sunk in the water (square tile)
    6: tiles seen from above: ice, snow, water, snow wall
Output: Assets/Art/Penguin/<Name>.png (names in SHEETS; the game reads them from the PenguinArt sprite library).

For every object: the white background is removed from the borders (clean alpha, dark fringe: no white halo), the object is cut to
its pixels. Never scaled. The sprites of one family get the SAME canvas, so the game draws them all at one scale and one anchor:
    front penguins (6 written, 8 measured): feet on the bottom edge, the body centred (the centre of the feet), the canvas as big as the biggest one;
    top penguins (4):   the centre of the black body at the centre of the canvas (the game turns them around it);
    igloos (4):         the base on the bottom edge, the dome centred.
Objects and tiles keep their own crop. Writes are safe (temporary file, then replace); the script can be run again at any time.
Usage (from the project root):  python Tools/Art/make_penguin_art.py
"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sprite_cut import crop, load_sheet, save_safe, split_columns  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "ArtSource", "PG")
OUT = os.path.join(ROOT, "Assets", "Art", "Penguin")

FACES = ["Normal", "Happy", "Scared", "Dizzy"]
# ErNa-012: the game has no dizzy face any more. The drawing is still on the sheets: it still sets the shared canvas of the front
# penguins (so the other faces keep exactly their size and anchor), but it is not written.
SKIP = {"Penguin_Orange_Front_Dizzy", "Penguin_Purple_Front_Dizzy"}
SHEETS = {
    "1": ("front", ["Penguin_Orange_Front_" + f for f in FACES]),
    "2": ("front", ["Penguin_Purple_Front_" + f for f in FACES]),
    "3": ("top", ["Penguin_Orange_Top_Idle", "Penguin_Orange_Top_Slide", "Penguin_Purple_Top_Idle", "Penguin_Purple_Top_Slide"]),
    "4": ("igloo", ["Igloo_Orange_Closed", "Igloo_Orange_Open", "Igloo_Purple_Closed", "Igloo_Purple_Open"]),
    "5": ("single", ["Fish", "IceBlock", "Rock", "Tile_SunkenBlock"]),
    "6": ("single", ["Tile_Ice", "Tile_Snow", "Tile_Water", "Tile_SnowWall"]),
}
FOOT_ROWS = 0.06      # front penguins and igloos: the bottom rows (share of the height) give the horizontal centre
BODY_LUM = 90         # top penguins: the black body is darker than this


def anchor_front(obj):
    """(x, y) anchor in the crop: centre of the bottom rows, bottom edge."""
    a = obj[..., 3] > 128
    h = a.shape[0]
    ys, xs = np.nonzero(a[int(h * (1 - FOOT_ROWS)):, :])
    return (xs.min() + xs.max() + 1) / 2.0, float(h)


def anchor_top(obj):
    """(x, y) anchor in the crop: centroid of the black body."""
    rgb = obj[..., :3].astype(np.float32)
    body = (obj[..., 3] > 200) & (rgb.mean(axis=2) < BODY_LUM)
    ys, xs = np.nonzero(body)
    return xs.mean(), ys.mean()


def on_canvas(objs, anchors, kind):
    """Every object of a family on one canvas: the anchors at the same point (bottom centre or centre)."""
    if kind == "top":
        left = max(ax for ax, _ in anchors)
        right = max(o.shape[1] - ax for o, (ax, _) in zip(objs, anchors))
        up = max(ay for _, ay in anchors)
        down = max(o.shape[0] - ay for o, (_, ay) in zip(objs, anchors))
        half_w, half_h = int(np.ceil(max(left, right))), int(np.ceil(max(up, down)))
        W, H, cx, cy = 2 * half_w, 2 * half_h, half_w, half_h
    else:
        left = max(ax for ax, _ in anchors)
        right = max(o.shape[1] - ax for o, (ax, _) in zip(objs, anchors))
        half_w = int(np.ceil(max(left, right)))
        W, H, cx = 2 * half_w, max(o.shape[0] for o in objs), half_w
        cy = H
    out = []
    for o, (ax, ay) in zip(objs, anchors):
        canvas = np.zeros((H, W, 4), dtype=np.uint8)
        x0 = int(round(cx - ax))
        y0 = int(round(cy - ay))
        canvas[y0:y0 + o.shape[0], x0:x0 + o.shape[1]] = o
        out.append(canvas)
    return out, (W, H)


def main():
    problems, written = [], 0
    groups = {}   # kind -> list of (name, object)
    for sheet, (kind, names) in SHEETS.items():
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
        groups.setdefault(kind, []).extend((name, crop(rgba_sheet[:, x0:x1])) for (x0, x1), name in zip(ranges, names))
    if problems:
        print("PROBLEMS (nothing written):")
        for p in problems:
            print("  - " + p)
        sys.exit(1)
    for kind, items in groups.items():
        names = [n for n, _ in items]
        objs = [o for _, o in items]
        if kind == "single":
            for name, obj in items:
                save_safe(obj, os.path.join(OUT, name + ".png"))
                written += 1
                print(f"  {name}: {obj.shape[1]}x{obj.shape[0]}")
            continue
        anchors = [anchor_top(o) if kind == "top" else anchor_front(o) for o in objs]
        placed, (W, H) = on_canvas(objs, anchors, kind)
        print(f"{kind}: {len(objs)} sprites on one canvas {W}x{H}")
        for obj, canvas, (ax, ay), name in zip(objs, placed, anchors, names):
            if name in SKIP:
                print(f"  {name}: not written (unused)")
                continue
            save_safe(canvas, os.path.join(OUT, name + ".png"))
            written += 1
            print(f"  {name}: object {obj.shape[1]}x{obj.shape[0]}, anchor ({ax:.1f}, {ay:.1f})")
    print(f"OK: {written} sprites")


if __name__ == "__main__":
    main()
