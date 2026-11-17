"""Prepare the Plumber sources and targets (fountains) drawn with Gemini for the game.

Input (read only, never modified):
    ArtSource/Plumber/Sources/*.png and ArtSource/Plumber/Targets/*.png
    (fallback: ArtSource/Plumber/Fonte_*.png and ArtSource/Plumber/Arrivo_*.png)
Output:
    Assets/Art/Plumber/Sources/fonte_N.png, fonte_N_mask.png
    Assets/Art/Plumber/Targets/arrivo_N.png

For every image:
  1. the white background is removed starting from the borders (flood fill): the black outlines are closed, so the white *inside*
     the object stays; the anti-aliased fringe between the outline and the background gets a partial alpha;
  2. the object is cropped, centred in a square with a small margin and scaled down to 512 px (never up);
  3. sources only: the magenta parts (also the darker, shaded magenta) go into a grey-scale mask that keeps the shading
     (the game tints it with the water color); in the base image they become a neutral light grey.

Writes are safe (temporary file, then replace) and the script can be run again at any time.
Usage (from the project root):  python Tools/Art/make_plumber_art.py
"""
import glob
import os
import re
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "ArtSource", "Plumber")
OUT = os.path.join(ROOT, "Assets", "Art", "Plumber")

OUT_SIZE = 512          # final square side (px), only ever scaled down
MARGIN = 0.04           # empty margin around the object, fraction of the square side
WHITE_MIN = 232         # a background pixel has every channel >= this (Gemini whites are a little noisy)
FRINGE_MAX = 200        # fringe pixels next to the background, lighter than this luminance floor, get a partial alpha
NEUTRAL_GREY = np.array([222, 224, 228], dtype=np.float32)   # replaces the magenta in the base image

EXPECTED = 4            # 4 sources and 4 targets


def find_inputs(kind):
    """kind = 'Sources' or 'Targets'. Returns the sorted list of input files."""
    folder = os.path.join(SRC, kind)
    files = sorted(glob.glob(os.path.join(folder, "*.png")))
    if files:
        return files
    prefix = "Fonte_" if kind == "Sources" else "Arrivo_"
    return sorted(glob.glob(os.path.join(SRC, prefix + "*.png")), key=natural_key)


def natural_key(path):
    return [int(t) if t.isdigit() else t.lower() for t in re.split(r"(\d+)", os.path.basename(path))]


def remove_background(rgb):
    """rgb: HxWx3 float32 0..255. Returns alpha 0..1 (white connected to the borders = 0)."""
    whiteish = np.all(rgb >= WHITE_MIN, axis=2)
    labels, _ = ndimage.label(whiteish)
    border = np.unique(np.concatenate([labels[0, :], labels[-1, :], labels[:, 0], labels[:, -1]]))
    border = border[border != 0]
    background = np.isin(labels, border)
    alpha = np.where(background, 0.0, 1.0).astype(np.float32)

    # anti-aliased fringe: object pixels touching the background that are still light are a blend of the black outline
    # and the white background -> alpha from their darkness (they are drawn as dark outline pixels)
    near_bg = ndimage.binary_dilation(background, iterations=2) & ~background
    lum = rgb.mean(axis=2)
    fringe = near_bg & (lum > FRINGE_MAX)
    alpha[fringe] = np.clip((255.0 - lum[fringe]) / (255.0 - FRINGE_MAX), 0.0, 1.0)
    return alpha, fringe


def magenta_weight(rgb):
    """0..1 membership of every pixel in the magenta parts (hue around 300 deg, saturated, also when shaded dark)."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx = np.maximum(np.maximum(r, g), b)
    mn = np.minimum(np.minimum(r, g), b)
    chroma = mx - mn
    sat = np.where(mx > 0, chroma / np.maximum(mx, 1e-6), 0.0)
    # magenta: red and blue both high compared with green, red and blue close to each other
    rb_min = np.minimum(r, b)
    rb_gap = np.abs(r - b) / np.maximum(mx, 1e-6)
    green_low = (rb_min - g) / np.maximum(mx, 1e-6)
    w = smoothstep(0.18, 0.40, sat) * smoothstep(0.15, 0.35, green_low) * (1.0 - smoothstep(0.35, 0.60, rb_gap))
    w *= smoothstep(25.0, 60.0, mx)   # pure black outline is never magenta
    return w.astype(np.float32)


def magenta_tint(rgb):
    """0..1 for any pixel with even a light magenta tint (red and blue above green, close to each other)."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx = np.maximum(np.maximum(r, g), b)
    green_low = (np.minimum(r, b) - g) / np.maximum(mx, 1e-6)
    rb_gap = np.abs(r - b) / np.maximum(mx, 1e-6)
    return (smoothstep(0.02, 0.08, green_low) * (1.0 - smoothstep(0.30, 0.50, rb_gap))).astype(np.float32)


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def crop_square(rgba):
    """Crops to the alpha bounding box and centres the object in a square with MARGIN, scaled down to OUT_SIZE."""
    a = rgba[..., 3]
    ys, xs = np.nonzero(a > 2)
    if len(xs) == 0:
        raise ValueError("empty image after background removal")
    x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    obj = rgba[y0:y1, x0:x1]
    h, w = obj.shape[:2]
    side = int(round(max(w, h) / (1.0 - 2 * MARGIN)))
    square = np.zeros((side, side, 4), dtype=np.uint8)
    ox, oy = (side - w) // 2, (side - h) // 2
    square[oy:oy + h, ox:ox + w] = obj
    img = Image.fromarray(square, "RGBA")
    if side > OUT_SIZE:
        # resize in premultiplied alpha: no white or black halos around the edges
        img = img.convert("RGBa").resize((OUT_SIZE, OUT_SIZE), Image.LANCZOS).convert("RGBA")
    return img, (x0, y0, x1, y1, side)


def save_safe(img, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    tmp = path + ".tmp"
    img.save(tmp, format="PNG")
    os.replace(tmp, path)


def process(path, out_name, out_dir, is_source):
    src = Image.open(path).convert("RGB")
    rgb = np.asarray(src, dtype=np.float32)
    alpha, fringe = remove_background(rgb)

    base = rgb.copy()
    base[fringe] = 0.0   # the fringe is the outline fading out: keep it dark
    report = {"file": os.path.relpath(path, ROOT), "size": src.size}
    obj_px = alpha > 0.5
    report["object_pct"] = 100.0 * obj_px.mean()

    mask_img = None
    if is_source:
        w = magenta_weight(rgb) * (alpha > 0.0)
        report["magenta_pct"] = 100.0 * (w[obj_px] > 0.5).sum() / max(1, obj_px.sum())
        # shading kept: the brightness of the magenta, normalised on the brightest magenta of the image
        value = np.maximum(np.maximum(rgb[..., 0], rgb[..., 1]), rgb[..., 2])
        strong = w > 0.5
        top = np.percentile(value[strong], 98) if strong.any() else 255.0
        shade = np.clip(value / max(top, 1.0), 0.0, 1.0)
        mask = np.zeros(rgb.shape[:2] + (4,), dtype=np.float32)
        mask[..., 0] = mask[..., 1] = mask[..., 2] = shade * 255.0
        mask[..., 3] = w * alpha * 255.0
        # base: the magenta becomes a neutral light grey with the same shading
        grey = NEUTRAL_GREY[None, None, :] * shade[..., None]
        base = base * (1.0 - w[..., None]) + grey * w[..., None]
        # faint magenta tint left on the soft edges (weight below 1): made neutral grey, so no pink line shows around the mask
        tint = magenta_tint(base)
        lum = base.mean(axis=2, keepdims=True)
        base = base * (1.0 - tint[..., None]) + lum * tint[..., None]
        mask_img = mask

    rgba = np.dstack([base, alpha * 255.0]).clip(0, 255).astype(np.uint8)
    img, box = crop_square(rgba)
    save_safe(img, os.path.join(out_dir, out_name + ".png"))
    written = [out_name + ".png"]
    if mask_img is not None:
        x0, y0, x1, y1, side = box
        m = mask_img.clip(0, 255).astype(np.uint8)
        # same crop and placement as the base image, so the mask lies exactly on it
        obj = m[y0:y1, x0:x1]
        h, w_ = obj.shape[:2]
        square = np.zeros((side, side, 4), dtype=np.uint8)
        square[(side - h) // 2:(side - h) // 2 + h, (side - w_) // 2:(side - w_) // 2 + w_] = obj
        mimg = Image.fromarray(square, "RGBA")
        if side > OUT_SIZE:
            mimg = mimg.convert("RGBa").resize((OUT_SIZE, OUT_SIZE), Image.LANCZOS).convert("RGBA")
        save_safe(mimg, os.path.join(out_dir, out_name + "_mask.png"))
        written.append(out_name + "_mask.png")
    report["written"] = written
    report["out_size"] = img.size
    return report


def main():
    problems = []
    for kind, prefix, is_source in (("Sources", "fonte", True), ("Targets", "arrivo", False)):
        files = find_inputs(kind)
        print(f"{kind}: {len(files)} file(s) found: {', '.join(os.path.basename(f) for f in files)}")
        if len(files) != EXPECTED:
            problems.append(f"{kind}: expected {EXPECTED} images, found {len(files)}")
        out_dir = os.path.join(OUT, kind)
        for i, path in enumerate(files, start=1):
            r = process(path, f"{prefix}_{i}", out_dir, is_source)
            line = f"  {r['file']} {r['size'][0]}x{r['size'][1]} -> {', '.join(r['written'])} ({r['out_size'][0]} px), object {r['object_pct']:.1f}% of the image"
            if is_source:
                line += f", magenta {r['magenta_pct']:.1f}% of the object"
                if r["magenta_pct"] < 2.0:
                    problems.append(f"{r['file']}: magenta only {r['magenta_pct']:.1f}% (mask empty or wrong?)")
            print(line)
    if problems:
        print("PROBLEMS:")
        for p in problems:
            print("  - " + p)
        sys.exit(1)
    print("OK")


if __name__ == "__main__":
    main()
