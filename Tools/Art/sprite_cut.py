"""Shared sprite cutting for the drawings of every minigame (moved here from make_trafficjam_art.py in PG-002: never copy it).

A sheet is a row of objects side by side on a white background. The helpers:
  * remove_background: the white connected to the borders becomes transparent (flood fill), so the white inside the drawings (eyes,
    stripes, snow) stays; the anti-aliased fringe between the outline and the background gets a partial alpha and is made dark by
    load_sheet (no white halo);
  * split_columns: the objects of a sheet, separated by empty columns;
  * crop: an object cut to its pixels, with a small transparent margin;
  * save_safe: temporary file, then replace.
Nothing is ever scaled up or down here.
"""
import os

import numpy as np
from PIL import Image
from scipy import ndimage

WHITE_MIN = 236         # a background pixel has every channel >= this
FRINGE_MAX = 200        # fringe pixels next to the background lighter than this get a partial alpha
MIN_GAP = 12            # empty columns that separate two objects of a sheet
MIN_OBJECT_PX = 2000    # smaller groups are noise
PAD = 2                 # transparent pixels kept around every sprite (clean filtering at the edges)


def remove_background(rgb, white_min=WHITE_MIN, fringe_max=FRINGE_MAX):
    """rgb: HxWx3 float32. Returns alpha 0..1 (white connected to the borders = 0) and the fringe mask."""
    whiteish = np.all(rgb >= white_min, axis=2)
    labels, _ = ndimage.label(whiteish)
    border = np.unique(np.concatenate([labels[0, :], labels[-1, :], labels[:, 0], labels[:, -1]]))
    border = border[border != 0]
    background = np.isin(labels, border)
    alpha = np.where(background, 0.0, 1.0).astype(np.float32)
    near_bg = ndimage.binary_dilation(background, iterations=2) & ~background
    lum = rgb.mean(axis=2)
    fringe = near_bg & (lum > fringe_max)
    alpha[fringe] = np.clip((255.0 - lum[fringe]) / (255.0 - fringe_max), 0.0, 1.0)
    return alpha, fringe


def load_sheet(path, white_min=WHITE_MIN, fringe_max=FRINGE_MAX):
    """The sheet as RGBA uint8 with the background removed (the fringe made dark), and its alpha 0..1."""
    rgb = np.asarray(Image.open(path).convert("RGB"), dtype=np.float32)
    alpha, fringe = remove_background(rgb, white_min, fringe_max)
    base = rgb.copy()
    base[fringe] = 0.0   # the outline fading out: dark, never a white halo
    rgba = np.dstack([base, alpha * 255.0]).clip(0, 255).astype(np.uint8)
    return rgba, alpha


def split_columns(alpha, min_gap=MIN_GAP, min_object_px=MIN_OBJECT_PX):
    """Column ranges [x0, x1) of the objects of a sheet, separated by at least min_gap empty columns."""
    used = (alpha > 0.05).sum(axis=0) > 0
    ranges, x, w = [], 0, len(used)
    while x < w:
        if not used[x]:
            x += 1
            continue
        x0 = x
        gap = 0
        while x < w and gap < min_gap:
            gap = gap + 1 if not used[x] else 0
            x += 1
        x1 = x - gap
        if (alpha[:, x0:x1] > 0.5).sum() >= min_object_px:
            ranges.append((x0, x1))
    return ranges


def crop(rgba, pad=PAD):
    ys, xs = np.nonzero(rgba[..., 3] > 2)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    obj = rgba[y0:y1, x0:x1]
    return np.pad(obj, ((pad, pad), (pad, pad), (0, 0)))


def save_safe(arr, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    tmp = path + ".tmp"
    Image.fromarray(arr, "RGBA").save(tmp, format="PNG")
    os.replace(tmp, path)
