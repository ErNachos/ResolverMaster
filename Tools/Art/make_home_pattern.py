"""ErNa-010: the tile of the Home background pattern: small silhouettes from the games (water drop, pipe, car, fish, snowflake),
taken from the existing icon masks (Assets/Art/Icons, white on transparent), scattered sparsely and rotated a little.

The tile repeats seamlessly (shapes crossing an edge are drawn again on the other side). White on transparent: the game tints it
a little darker than the background (Core PatternBackground).
Output: Assets/Resources/ResolverMaster/HomePattern.png (512x512). Usage (from the project root):  python Tools/Art/make_home_pattern.py
"""
import os

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ICONS = os.path.join(ROOT, "Assets", "Art", "Icons")
OUT = os.path.join(ROOT, "Assets", "Resources", "ResolverMaster", "HomePattern.png")
SIZE = 512
SHAPE = 84  # px of one silhouette in the tile

# (icon, x, y, rotation degrees): a sparse, irregular scatter (two of each)
PLACES = [
    ("water_drop", 60, 70, -12), ("game_plumber", 300, 40, 18), ("game_trafficjam", 180, 190, -8), ("set_meal", 430, 170, 14),
    ("game_penguin", 70, 320, 22), ("water_drop", 330, 290, 10), ("game_trafficjam", 470, 420, 6), ("set_meal", 200, 430, -16),
    ("game_plumber", 40, 470, -20), ("game_penguin", 360, 470 - 330, -4),
]


def main():
    tile = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    for name, x, y, rot in PLACES:
        icon = Image.open(os.path.join(ICONS, name + ".png")).convert("RGBA")
        icon = icon.resize((SHAPE, SHAPE), Image.LANCZOS).rotate(rot, resample=Image.BICUBIC, expand=True)
        alpha = icon.getchannel("A")
        white = Image.new("RGBA", icon.size, (255, 255, 255, 0))
        white.putalpha(alpha)
        for dx in (-SIZE, 0, SIZE):
            for dy in (-SIZE, 0, SIZE):
                # paste clips at the edges; the copies one tile away make the pattern seamless
                tile.paste(white, (x - icon.width // 2 + dx, y - icon.height // 2 + dy), white)
    tmp = OUT + ".tmp.png"
    tile.save(tmp)
    os.replace(tmp, OUT)
    print("pattern", OUT)


if __name__ == "__main__":
    main()
