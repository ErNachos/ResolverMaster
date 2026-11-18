"""PG-001: provisional illustration of the Pingù card on Home (until the Gemini drawing arrives).

Cartoon style with black outlines, transparent background: an ice floe with an igloo (orange flag), the penguin (orange scarf) sliding
towards it, a fish and a few rocks. Drawn at 4x and scaled down (smooth edges).
Output: Assets/Art/Cards/card_penguin.png (drawn on 960x720, cropped to the drawing). Usage (from the project root):  python Tools/Art/make_penguin_card.py
"""
import os

from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Art", "Cards", "card_penguin.png")
W, H, S = 960, 720, 4
OL = 7 * S  # outline width

BLACK = (24, 24, 28, 255)
ICE = (214, 238, 246, 255)
ICE_SHADE = (163, 206, 222, 255)
SNOW = (250, 252, 253, 255)
SNOW_SHADE = (205, 222, 232, 255)
BODY = (43, 47, 58, 255)
BELLY = (250, 250, 245, 255)
BEAK = (246, 178, 60, 255)
SCARF = (242, 140, 40, 255)
FISH = (242, 143, 122, 255)
FIN = (224, 106, 88, 255)
ROCK = (138, 143, 150, 255)
DOOR = (43, 51, 64, 255)


def box(cx, cy, rx, ry):
    return [(cx - rx) * S, (cy - ry) * S, (cx + rx) * S, (cy + ry) * S]


def ellipse(d, cx, cy, rx, ry, fill, outline=True):
    d.ellipse(box(cx, cy, rx, ry), fill=fill, outline=BLACK if outline else None, width=OL if outline else 0)


def main():
    img = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # the ice floe
    ellipse(d, 480, 520, 440, 170, ICE_SHADE)
    ellipse(d, 480, 500, 425, 150, ICE, outline=False)
    for x, y, r in [(210, 520, 40), (690, 560, 30), (560, 470, 22)]:
        d.line([((x - r) * S, y * S), ((x + r) * S, (y - 12) * S)], fill=(255, 255, 255, 230), width=6 * S)

    # igloo with its orange flag
    d.line([(735 * S, 330 * S), (735 * S, 190 * S)], fill=(64, 56, 50, 255), width=9 * S)
    d.rounded_rectangle(box(785, 215, 50, 30), radius=8 * S, fill=SCARF, outline=BLACK, width=int(OL * 0.8))
    d.pieslice(box(650, 470, 180, 170), 180, 360, fill=SNOW, outline=BLACK, width=OL)
    d.line([(472 * S, 470 * S), (828 * S, 470 * S)], fill=BLACK, width=OL)
    for y in (420, 375, 335):
        half = (180 ** 2 - (470 - y) ** 2 * (180 / 170) ** 2) ** 0.5
        d.line([((650 - half + 8) * S, y * S), ((650 + half - 8) * S, y * S)], fill=SNOW_SHADE, width=5 * S)
    d.pieslice(box(650, 470, 62, 70), 180, 360, fill=DOOR, outline=BLACK, width=int(OL * 0.8))

    # rocks and a fish
    for cx, cy, rx, ry in [(150, 430, 55, 38), (195, 445, 40, 30)]:
        ellipse(d, cx, cy, rx, ry, ROCK)
    d.polygon([(385 * S, 585 * S), (425 * S, 560 * S), (425 * S, 610 * S)], fill=FIN, outline=BLACK, width=int(OL * 0.8))
    ellipse(d, 345, 585, 48, 26, FISH)
    ellipse(d, 318, 580, 6, 6, BLACK, outline=False)

    # the penguin (sliding, tilted a little), orange scarf
    pen = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    p = ImageDraw.Draw(pen)
    cx, cy = 360, 360
    for fx in (-38, 38):
        ellipse(p, cx + fx, cy + 150, 40, 20, BEAK)
    for fx in (-118, 118):
        ellipse(p, cx + fx, cy + 40, 30, 70, BODY)
    ellipse(p, cx, cy + 50, 120, 120, BODY)
    ellipse(p, cx, cy - 75, 92, 88, BODY)
    ellipse(p, cx, cy + 62, 82, 92, BELLY, outline=False)
    ellipse(p, cx, cy - 70, 64, 52, BELLY, outline=False)
    ellipse(p, cx - 34, cy - 92, 22, 12, (255, 255, 255, 120), outline=False)
    # happy eyes and cheeks
    for ex in (-26, 26):
        p.arc(box(cx + ex, cy - 74, 14, 14), 200, 340, fill=BLACK, width=6 * S)
        ellipse(p, cx + ex * 1.9, cy - 48, 13, 7, (255, 150, 160, 200), outline=False)
    ellipse(p, cx, cy - 50, 26, 15, BEAK)
    # the scarf with its tail
    p.rounded_rectangle(box(cx, cy - 8, 96, 18), radius=16 * S, fill=SCARF, outline=BLACK, width=int(OL * 0.8))
    p.rounded_rectangle(box(cx + 52, cy + 30, 18, 36), radius=12 * S, fill=SCARF, outline=BLACK, width=int(OL * 0.8))
    pen = pen.rotate(8, resample=Image.BICUBIC, center=(cx * S, cy * S))
    img.alpha_composite(pen)

    img = img.resize((W, H), Image.LANCZOS)
    # cropped to the drawing (no empty band), like the other card illustrations
    l, t, r, b = img.getchannel("A").getbbox()
    img = img.crop((max(0, l - 8), max(0, t - 8), min(W, r + 8), min(H, b + 8)))
    tmp = OUT + ".tmp.png"
    img.save(tmp)
    os.replace(tmp, OUT)
    print("card", OUT, img.size)


if __name__ == "__main__":
    main()
