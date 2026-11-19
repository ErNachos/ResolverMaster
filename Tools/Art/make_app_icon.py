"""App icon (ErNa-011): from ArtSource/Icon/Icon.png (a disc with blue rays, three fanned cards and the "RM" ribbon on white) to every
Android icon of the Player Settings and the Play Store icon.

  * the white outside the drawing (outside the disc, around the cards and ribbon ends that stick out, and the white pockets between
    them) becomes transparent; the anti-aliased edge keeps the colour of the drawing next to it, with a partial alpha (no white halo);
  * background colour = the night blue of the disc rim (median of a thin ring just inside the edge);
  * adaptive foreground: the whole drawing, cards included, inside the safe zone (circle of 66 dp on 108 dp) with a small margin;
    adaptive background: that night blue, plain;
  * legacy (square) and round icons: the same drawing on the night blue (square, or a disc with transparent corners);
  * ArtSource/Icon/Icon_PlayStore_512.png: 512x512, 32-bit, square and full to the edges (drawing on night blue).

Run from the project root:  python Tools/Art/make_app_icon.py   (then Resolver Master > Setup > Apply Project Settings, or the batch setup)
Output in Assets/Art/AppIcon/ (one file per kind and density; the plain background also gives the colour of the Android 12+ splash). The source in ArtSource/ is never modified.
"""
import os
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sprite_cut import WHITE_MIN, save_safe  # noqa: E402  (shared tool: the same "white" threshold as every drawing)

SRC = "ArtSource/Icon/Icon.png"
OUT_DIR = "Assets/Art/AppIcon"
STORE = "ArtSource/Icon/Icon_PlayStore_512.png"

# Android densities (the slots of the Unity Player Settings): adaptive layers are 108 dp, legacy and round icons 48 dp
# (ldpi x0.75, mdpi x1 ... xxxhdpi x4)
ADAPTIVE_SIZES = [432, 324, 216, 162, 108, 81]
LEGACY_SIZES = [192, 144, 96, 72, 48, 36]

SAFE_RADIUS = 33.0 / 108.0      # the adaptive safe zone: a circle of 66 dp on 108 dp
ADAPTIVE_REACH = 0.285          # the farthest pixel of the drawing from the centre, share of the side (safe zone 0.306: small margin)
LEGACY_REACH = 0.47             # legacy, round and Play Store icons: the drawing almost to the edge
EDGE_BAND = 3                   # pixels next to the removed white that get a partial alpha
RIM_INSET = (6, 14)             # the ring (pixels inside the disc edge) that gives the night blue


def load():
    return np.asarray(Image.open(SRC).convert("RGB"), dtype=np.float32)


def white_outside(rgb):
    """The white connected to the borders, then the fit of the disc, then the white pockets that lie entirely outside the disc."""
    whiteish = np.all(rgb >= WHITE_MIN, axis=2)
    labels, n = ndimage.label(whiteish)
    border = np.unique(np.concatenate([labels[0, :], labels[-1, :], labels[:, 0], labels[:, -1]]))
    border = border[border != 0]
    background = np.isin(labels, border)
    cx, cy, r = fit_disc(background)
    h, w = background.shape
    yy, xx = np.mgrid[0:h, 0:w]
    dist = np.hypot(xx - cx, yy - cy)
    # a white pocket (e.g. between a card that sticks out and the disc) is background when none of it is inside the disc
    inner = ndimage.maximum(dist, labels, index=np.arange(1, n + 1))
    nearest = ndimage.minimum(dist, labels, index=np.arange(1, n + 1))
    for i in range(n):
        if nearest[i] > r - 2 and inner[i] > r:
            background |= labels == i + 1
    return background, (cx, cy, r), dist


def fit_disc(background):
    """Centre and radius of the disc: on 720 rays from the centre of the drawing, where the drawing ends; the rays where cards or
    ribbon stick out are dropped (far from the median), then a least-squares circle through the others."""
    h, w = background.shape
    ys, xs = np.nonzero(~background)
    cx, cy = xs.mean(), ys.mean()
    pts = []
    for a in np.linspace(0, 2 * np.pi, 720, endpoint=False):
        dx, dy = np.cos(a), np.sin(a)
        rr = None
        for t in np.arange(min(w, h) * 0.75, 0, -0.5):
            x, y = int(round(cx + dx * t)), int(round(cy + dy * t))
            if 0 <= x < w and 0 <= y < h and not background[y, x]:
                rr = t
                break
        if rr is not None:
            pts.append((cx + dx * rr, cy + dy * rr, rr))
    pts = np.array(pts)
    med = np.median(pts[:, 2])
    keep = pts[np.abs(pts[:, 2] - med) < 4]
    x, y = keep[:, 0], keep[:, 1]
    a = np.column_stack([x, y, np.ones_like(x)])
    b = x * x + y * y
    c = np.linalg.lstsq(a, b, rcond=None)[0]
    fx, fy = c[0] / 2, c[1] / 2
    fr = np.sqrt(c[2] + fx * fx + fy * fy)
    return float(fx), float(fy), float(fr)


def cut(rgb, background):
    """RGBA float (0..1 alpha): the band next to the removed white gets the colour of the nearest solid pixel of the drawing and the
    alpha that mixes it with white into what the source shows (least squares over the channels): no white halo."""
    band = ndimage.binary_dilation(background, iterations=EDGE_BAND) & ~background
    solid = ~background & ~band
    _, (iy, ix) = ndimage.distance_transform_edt(~solid, return_indices=True)
    inner = rgb[iy, ix]
    d = 255.0 - inner
    c = 255.0 - rgb
    alpha = np.clip((c * d).sum(axis=2) / np.maximum((d * d).sum(axis=2), 1.0), 0.0, 1.0)
    alpha = np.where(band, alpha, 1.0)
    alpha[background] = 0.0
    color = np.where(band[..., None], inner, rgb)
    return np.dstack([color, alpha])


def rim_color(rgb, background, disc, dist):
    cx, cy, r = disc
    ring = (dist > r - RIM_INSET[1]) & (dist < r - RIM_INSET[0]) & ~background
    # only the blue of the disc (the cards and the ribbon crossing the ring are not blue)
    px = rgb[ring]
    blue = px[(px[:, 2] > px[:, 0] + 40) & (px[:, 2] > px[:, 1] + 20)]
    return tuple(int(round(v)) for v in np.median(blue, axis=0))


def place(rgba, disc, reach_px, size):
    """The drawing scaled so that its farthest pixel from the disc centre is reach_px away, the disc centre on the canvas centre.
    Premultiplied resampling (no dark or white fringes), high-quality filter."""
    cx, cy, _ = disc
    h, w, _ = rgba.shape
    ys, xs = np.nonzero(rgba[..., 3] > 0.02)
    far = np.hypot(xs - cx, ys - cy).max() + 1.0
    scale = reach_px / far
    pre = rgba.copy()
    pre[..., :3] *= pre[..., 3:4]
    big = 4   # draw at 4x, then reduce: clean edges at every size
    canvas = size * big
    s = scale * big
    out = np.zeros((canvas, canvas, 4), np.float32)
    nw, nh = int(round(w * s)), int(round(h * s))
    chans = []
    for k in range(4):
        im = Image.fromarray(pre[..., k] if k < 3 else pre[..., 3] * 255.0, mode="F")
        chans.append(np.asarray(im.resize((nw, nh), Image.LANCZOS), np.float32))
    scaled = np.dstack(chans)
    ox = int(round(canvas / 2 - cx * s))
    oy = int(round(canvas / 2 - cy * s))
    x0, y0 = max(0, ox), max(0, oy)
    x1, y1 = min(canvas, ox + nw), min(canvas, oy + nh)
    out[y0:y1, x0:x1] = scaled[y0 - oy:y1 - oy, x0 - ox:x1 - ox]
    out = np.clip(out, 0.0, 255.0)
    small = []
    for k in range(4):
        small.append(np.asarray(Image.fromarray(out[..., k], mode="F").resize((size, size), Image.LANCZOS), np.float32))
    pre_small = np.clip(np.dstack(small), 0.0, 255.0)
    return pre_small   # premultiplied colour 0..255, alpha 0..255


def over(pre, bg, mask=None):
    """Premultiplied drawing over a plain colour; mask (0..1) cuts the result (round icon)."""
    a = pre[..., 3:4] / 255.0
    rgb = pre[..., :3] + np.array(bg, np.float32) * (1.0 - a)
    alpha = np.full(pre.shape[:2], 255.0, np.float32) if mask is None else mask * 255.0
    return np.dstack([rgb, alpha]).clip(0, 255).round().astype(np.uint8)


def straight(pre):
    """Premultiplied -> straight alpha (transparent foreground)."""
    a = pre[..., 3:4]
    rgb = np.where(a > 0.5, pre[..., :3] * 255.0 / np.maximum(a, 1e-3), 0.0)
    return np.dstack([rgb, a]).clip(0, 255).round().astype(np.uint8)


def disc_mask(size):
    """Anti-aliased disc filling the square (4x4 supersampling)."""
    n = 4
    yy, xx = np.mgrid[0:size * n, 0:size * n]
    c = size * n / 2.0
    inside = (np.hypot(xx + 0.5 - c, yy + 0.5 - c) <= c).astype(np.float32)
    return inside.reshape(size, n, size, n).mean(axis=(1, 3))


def main():
    rgb = load()
    background, disc, dist = white_outside(rgb)
    rgba = cut(rgb, background)
    bg = rim_color(rgb, background, disc, dist)
    os.makedirs(OUT_DIR, exist_ok=True)

    for size in ADAPTIVE_SIZES:
        fore = place(rgba, disc, ADAPTIVE_REACH * size, size)
        save_safe(straight(fore), f"{OUT_DIR}/AppIcon_Foreground_{size}.png")
        plain = np.zeros((size, size, 4), np.uint8)
        plain[...] = (*bg, 255)
        save_safe(plain, f"{OUT_DIR}/AppIcon_Background_{size}.png")
    for size in LEGACY_SIZES:
        pre = place(rgba, disc, LEGACY_REACH * size, size)
        save_safe(over(pre, bg), f"{OUT_DIR}/AppIcon_Legacy_{size}.png")
        save_safe(over(pre, bg, disc_mask(size)), f"{OUT_DIR}/AppIcon_Round_{size}.png")

    store = over(place(rgba, disc, LEGACY_REACH * 512, 512), bg)
    tmp = STORE + ".tmp"
    Image.fromarray(store, "RGBA").save(tmp, format="PNG", optimize=True)
    os.replace(tmp, STORE)

    print(f"disc centre ({disc[0]:.1f}, {disc[1]:.1f}) radius {disc[2]:.1f}; night blue #%02X%02X%02X" % bg)
    print(f"Play Store icon: {os.path.getsize(STORE)} bytes")


if __name__ == "__main__":
    main()
