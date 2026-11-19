"""
Generator sprite pixel-art untuk FarmGame.
Semua gambar dibuat dari kode: grid huruf (digambar per piksel) + palet warna terbatas.
Jalankan:  python Tools/generate_assets.py        -> hasil di folder Assets/
Hasil deterministik (seed tetap), jadi aman dijalankan ulang.
"""
import math
import os
import random
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.abspath(sys.argv[1]) if len(sys.argv) > 1 else os.path.join(HERE, "..", "Assets")


# --------------------------------------------------------------------------- utilitas

def C(h, a=255):
    """'#rrggbb' -> (r, g, b, a)"""
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def new(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


def grid(rows, pal, img=None, ox=0, oy=0, w=16):
    """Gambar dari baris teks. '.' = transparan, huruf lain = warna di palet."""
    if img is None:
        img = new(w, len(rows))
    for y, row in enumerate(rows):
        assert ox + len(row) <= img.width, f"baris terlalu panjang: {row!r}"
        for x, ch in enumerate(row):
            if ch != ".":
                col = pal[ch]
                img.putpixel((ox + x, oy + y), C(col) if isinstance(col, str) else col)
    return img


def outline(img, color):
    """Garis tepi 1 piksel di sekeliling piksel yang terlihat (gaya pixel art klasik)."""
    src = img.copy()
    s, d = src.load(), img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            if s[x, y][3] != 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and s[nx, ny][3] > 128:
                    d[x, y] = C(color)
                    break
    return img


def shadow(img, x0, x1, y0, y1, alpha=55):
    """Bayangan elips lembut di tanah."""
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    rx, ry = (x1 - x0 + 1) / 2, (y1 - y0 + 1) / 2
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1.0:
                img.putpixel((x, y), (20, 12, 8, alpha))


def save(img, folder, name):
    path = os.path.join(OUT, folder)
    os.makedirs(path, exist_ok=True)
    img.save(os.path.join(path, name + ".png"))
    return img


INK = "#2e1f16"   # warna garis tepi umum (cokelat sangat gelap, lebih hangat dari hitam)

# --------------------------------------------------------------------------- TILE

GRASS = [C(c) for c in ("#3b6e33", "#4c8a3c", "#5c9c45", "#70b04f", "#92c962")]
DIRT = [C(c) for c in ("#7a5537", "#91663f", "#a5784b", "#ba8d5d", "#d0a877")]
PATH = [C(c) for c in ("#9c845a", "#b39b6e", "#c6b083", "#d8c69c", "#e9dbb6")]
WATER = [C(c) for c in ("#2c5a8c", "#3769a0", "#4379b2", "#64a0d4", "#bfe2f6")]


def waves(rng, n=3):
    # frekuensi bulat -> pola menyambung mulus di tepi tile (tileable)
    return [(rng.uniform(0.6, 1.0), rng.choice([1, 2]) * rng.choice([-1, 1]), rng.choice([1, 2, 3]),
             rng.uniform(0, math.tau)) for _ in range(n)]


def field(ws, x, y):
    return sum(a * math.sin(math.tau * (fx * x + fy * y) / 16 + ph) for a, fx, fy, ph in ws)


def grass_tile(seed, flowers=0, clover=False):
    rng = random.Random(seed)
    img = new(16, 16)
    px = img.load()
    ws = waves(rng)
    for y in range(16):
        for x in range(16):
            v = 0.6 * field(ws, x, y) + rng.uniform(-0.6, 0.6)
            px[x, y] = GRASS[1] if v < -0.9 else GRASS[2] if v < 0.8 else GRASS[3]

    # helai rumput: ujung terang, pangkal gelap
    for _ in range(rng.randint(9, 13)):
        x, y = rng.randrange(16), rng.randrange(16)
        px[x, y] = GRASS[4] if rng.random() < 0.4 else GRASS[3]
        px[x, (y + 1) % 16] = GRASS[3] if px[x, y] == GRASS[4] else GRASS[2]
        px[x, (y + 2) % 16] = GRASS[1]
    # rumpun "V"
    for _ in range(rng.randint(1, 2)):
        x, y = rng.randrange(1, 15), rng.randrange(1, 14)
        px[x - 1, y] = GRASS[3]
        px[x + 1, y] = GRASS[3]
        px[x, y + 1] = GRASS[2]
        px[x, y + 2] = GRASS[0]
    for _ in range(rng.randint(2, 4)):
        px[rng.randrange(16), rng.randrange(16)] = GRASS[0]

    if clover:
        x, y = rng.randrange(3, 11), rng.randrange(3, 11)
        for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1), (2, 1), (1, 2)):
            px[x + dx, y + dy] = GRASS[4] if (dx + dy) % 2 == 0 else GRASS[3]
        px[x + 1, y + 3] = GRASS[0]

    petals = [C("#f4efe2"), C("#ef9fbc"), C("#9cc3ee"), C("#f6d65a")]
    for _ in range(flowers):
        x, y = rng.randrange(2, 13), rng.randrange(2, 12)
        p = rng.choice(petals)
        for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            px[x + dx, y + dy] = p
        px[x, y] = C("#e9b23a") if p != petals[3] else C("#c9762e")
        px[x, y + 2] = GRASS[0]
        px[x + 1, y + 2] = GRASS[1]
    return img


def pebble(px, x, y, pal, w=2):
    for i in range(w):
        px[x + i, y] = pal[4]
    px[x + w, y] = pal[3]
    for i in range(w + 1):
        px[x + i, y + 1] = pal[0]


def dirt_tile(seed):
    rng = random.Random(seed)
    img = new(16, 16)
    px = img.load()
    ws = waves(rng)
    for y in range(16):
        for x in range(16):
            # gelombang dibuat lemah supaya tidak muncul pola garis berulang
            v = 0.35 * field(ws, x, y) + rng.uniform(-0.7, 0.7)
            px[x, y] = DIRT[1] if v < -0.75 else DIRT[2] if v < 0.65 else DIRT[3]
    for _ in range(rng.randint(5, 8)):
        x, y = rng.randrange(16), rng.randrange(16)
        px[x, y] = DIRT[0]
        if rng.random() < 0.5:
            px[(x + 1) % 16, y] = DIRT[1]
    for _ in range(rng.randint(3, 5)):
        px[rng.randrange(16), rng.randrange(16)] = DIRT[4]
    for _ in range(rng.randint(0, 2)):
        pebble(px, rng.randrange(1, 12), rng.randrange(1, 13), DIRT, rng.choice([1, 2]))
    return img


def path_tile(seed):
    rng = random.Random(seed)
    img = new(16, 16)
    px = img.load()
    ws = waves(rng, 2)
    for y in range(16):
        for x in range(16):
            v = field(ws, x, y) + rng.uniform(-0.5, 0.5)
            px[x, y] = PATH[2] if v < 0.5 else PATH[3]
    for _ in range(6):
        px[rng.randrange(16), rng.randrange(16)] = PATH[1]
    # batu pipih
    placed = []
    for _ in range(rng.randint(2, 3)):
        for _try in range(20):
            w, h = rng.choice([(4, 3), (3, 2), (5, 3)])
            x, y = rng.randrange(1, 15 - w), rng.randrange(1, 15 - h)
            if all(abs(x - a) > 5 or abs(y - b) > 4 for a, b in placed):
                break
        placed.append((x, y))
        for yy in range(h):
            for xx in range(w):
                corner = (xx in (0, w - 1)) and (yy in (0, h - 1))
                if corner:
                    continue
                px[x + xx, y + yy] = PATH[4] if yy == 0 else PATH[3]
        for xx in range(1, w):
            px[x + xx, y + h] = PATH[0]
        px[x + w - 1, y + h - 1] = PATH[1]
    return img


def water_tile(frame, frames=4):
    rng = random.Random(77)
    img = new(16, 16)
    px = img.load()
    t = math.tau * frame / frames
    ws = [(0.8, 1, 1, 0.3), (0.6, -1, 2, 2.1)]
    for y in range(16):
        for x in range(16):
            v = field(ws, x, y) + 0.3 * math.sin(t + y * 0.8)
            px[x, y] = WATER[0] if v < -0.9 else WATER[1] if v < 0.6 else WATER[2]
    # garis ombak bergelombang yang bergeser tiap frame
    for row, phase in ((2, 0.0), (7, 2.2), (12, 4.1)):
        for x in range(16):
            s = math.sin(math.tau * x / 16 + phase + t)
            if s > 0.35:
                y = (row + (1 if math.sin(math.tau * x / 8 + phase) > 0.6 else 0)) % 16
                px[x, y] = WATER[4] if s > 0.92 else WATER[3]
                px[x, (y + 1) % 16] = WATER[1]
    # kilau kecil
    for i in range(2):
        x, y = rng.randrange(16), rng.randrange(16)
        if (frame + i) % 2 == 0:
            px[x, y] = WATER[4]
    return img


# ---- overlay pinggiran (autotile sederhana) ----

def grass_edge_north(seed):
    """Rumput 'menggantung' di sisi atas tile tanah/jalan."""
    rng = random.Random(seed)
    img = new(16, 16)
    px = img.load()
    depth = 2
    for x in range(16):
        depth = max(1, min(3, depth + rng.choice([-1, 0, 0, 1])))
        for y in range(depth):
            px[x, y] = GRASS[2] if y < depth - 1 else GRASS[1]
        if rng.random() < 0.3:
            px[x, 0] = GRASS[3]
        if rng.random() < 0.25:           # helai menjuntai
            px[x, depth] = GRASS[2]
            px[x, depth + 1] = (25, 15, 8, 70)
        else:
            px[x, depth] = (25, 15, 8, 70)  # bayangan rumput di atas tanah
    return img


def grass_corner_nw():
    img = new(16, 16)
    grid(["ab..",
          "bs..",
          "s..."], {"a": GRASS[2], "b": GRASS[1], "s": (25, 15, 8, 70)}, img)
    return img


def water_edges():
    E = {"e": "#6b5034", "f": "#54402a", "g": "#3e2f1e", "d": C("#1d406a", 200),
         "w": "#cfe9f8", "v": "#8fc3e8"}
    n = new(16, 16)
    grid(["effeefefeffeefef",
          "gggggggggggggggg",
          "dddddddddddddddd",
          ".w...v..w....v.."], E, n)
    s = new(16, 16)
    grid(["..v....w...v...w",
          "wvwwvwwwvwwvwwvw"], E, s, oy=14)
    w = new(16, 16)
    for y in range(16):
        w.putpixel((0, y), C(E["g"]))
        w.putpixel((1, y), C(E["w"]) if y % 5 in (1, 2) else C("#1d406a", 160))
    e = w.transpose(Image.FLIP_LEFT_RIGHT)
    return n, s, e, w


# ---- pagar ----

def fence_parts():
    base, light, top, dark = C("#94643c"), C("#b07c4e"), C("#c99460"), C("#6e4528")

    def finish(img):
        return outline(img, INK)

    post = new(16, 16)
    shadow(post, 4, 11, 12, 14, 60)
    for y in range(3, 13):
        for x in range(6, 10):
            post.putpixel((x, y), light if x == 6 else dark if x == 9 else base)
    for x in range(6, 10):
        post.putpixel((x, 3), top)
    post.putpixel((7, 4), top)
    finish(post)

    def rail_h(x0, x1):
        img = new(16, 16)
        for x in range(x0, x1 + 1):
            for y, c in ((5, top), (6, base), (9, light), (10, dark)):
                img.putpixel((x, y), c)
        return finish(img)

    def rail_v(y0, y1):
        img = new(16, 16)
        for y in range(y0, y1 + 1):
            img.putpixel((7, y), light)
            img.putpixel((8, y), dark)
        return finish(img)

    return {"fence_post": post, "fence_e": rail_h(10, 15), "fence_w": rail_h(0, 5),
            "fence_n": rail_v(0, 2), "fence_s": rail_v(13, 15)}


# --------------------------------------------------------------------------- KARAKTER (petani, 16x32)
#
# Cara membuat: kepala digambar per piksel (grid huruf), badan/lengan/kaki disusun per frame,
# lalu seluruh badan diberi garis tepi otomatis, baru bayangan ditaruh di belakangnya.
# Siklus jalan 4 frame: kaki-satu-maju, melintas (badan naik), kaki-lain-maju, melintas.

CHAR = {
    "H": "#efc874", "h": "#d4a24f", "j": "#9a6a33", "r": "#b5432f",
    "S": "#f6c9a3", "s": "#e0a27e", "c": "#ef9a86", "E": "#2a1b18", "m": "#b9614f",
    "B": "#7b4527", "b": "#553019", "n": "#a0603a",
    "T": "#4aa391", "t": "#327566", "U": "#7fd0bd", "W": "#7a5230", "Y": "#e8c170",
    "P": "#4a5d8f", "p": "#34436c", "O": "#6b4428", "o": "#4a2d18", "K": "#2a1b18",
}
CHAR_INK = "#2a1b18"

HEAD_FRONT = [
    "................",
    "......hHHh......",
    ".....HHHHHh.....",
    ".....HHHHHh.....",
    ".....rrrrrr.....",
    "..hHHHHHHHHHHh..",
    "...jjjjjjjjjj...",
    "....bBBBBBBb....",
    "....BSSSSSSB....",
    "....SSESSESS....",
    "....SSESSESS....",
    "....ScSSSScS....",
    "....sSSSmSSs....",
    ".....ssssss.....",
    ".......ss.......",
]

HEAD_BACK = HEAD_FRONT[:7] + [
    "....bBBBBBBb....",
    "....BBnBBnBB....",
    "....BnBBBnBB....",
    "....SBBnBBBS....",
    "....BBBBBnBB....",
    "....bBBBBBBb....",
    ".....bbbbbb.....",
    ".......ss.......",
]

HEAD_SIDE = [
    "................",
    ".....hHHh.......",
    "....HHHHHh......",
    "....HHHHHh......",
    "....rrrrrr......",
    "...hHHHHHHHHHHh.",
    "....jjjjjjjjjj..",
    "....bbBSSSSS....",
    "....BBBSSSSS....",
    "....BBSSSSES....",
    "....BBSSSSES....",
    "....BBSSSSSSS...",
    "....bBSSSScm....",
    ".....bsssss.....",
    ".......ss.......",
]


def put(img, x, y, ch):
    img.putpixel((x, y), C(CHAR[ch]))


def torso_front(img, hand_left, hand_right, back):
    """Badan + lengan (tampak depan/belakang). hand_* = baris tangan: 21 normal, 22 maju, 20 mundur."""
    for y in range(15, 22):
        for x in range(4, 12):
            ch = "U" if x == 4 else "t" if x == 11 else "T"
            if x in (5, 10):
                ch = "W"                       # tali suspender
            put(img, x, y, ch)
    if not back:
        put(img, 7, 15, "S")                   # leher kerah
        put(img, 8, 15, "s")
    for x in range(4, 12):                     # sabuk + gesper
        put(img, x, 22, "Y" if x in (7, 8) and not back else "W")
    for x in range(4, 12):
        put(img, x, 23, "P")                   # pinggul
    for x, inner, hand in ((2, 3, hand_left), (13, 12, hand_right)):
        for y in range(15, hand + 1):
            if y <= 17:
                put(img, x, y, "T")            # lengan baju pendek
                put(img, inner, y, "t")
            else:
                put(img, x, y, "S")            # lengan bawah (kulit)
                put(img, inner, y, "s")


def legs_front(img, foot_left, foot_right):
    """Kaki tampak depan. foot_* = baris atas sepatu: 28 normal, 29 maju, 26 terangkat."""
    for x0, foot, inner in ((4, foot_left, 6), (9, foot_right, 9)):
        for y in range(24, foot):
            for x in range(x0, x0 + 3):
                put(img, x, y, "p" if x == inner else "P")
        for x in range(x0, x0 + 3):
            put(img, x, foot, "O")
            put(img, x, foot + 1, "o")


SIDE_ARM = {   # lengan yang dekat kamera, digambar mulai kolom 4, baris 15 (t = bayangan pemisah lengan)
    "neutral": ["...UT....", "..tUT....", "..tUT....", "..tSs....", "..tSs....", "..tSs....", "...Ss...."],
    "forward": ["...UT....", "...tUT...", "....tUT..", "....tSs..", ".....tSs.", ".....tSs.", "......SS."],
    "back":    ["...UT....", "..tUT....", "..UT.....", ".tSs.....", ".Ss......", "Ss.......", "SS......."],
}

SIDE_LEGS = {   # P/O = kaki dekat kamera, p/o = kaki jauh (lebih gelap)
    "idle": [".....PPPPPP.....", "......PPPP......", "......PPPp......", "......PPPp......",
             "......PPPp......", "......OOOOO.....", "......oOOOOO...."],
    "contact_a": [".....PPPPPP.....", ".....pPPPPP.....", "....ppp.PPPP....", "....pp...PPP....",
                  "...pp.....PPP...", "..ooo.....OOOO..", "..oo......oOOOO."],
    "pass_a": [".....PPPPPP.....", ".....pPPPP......", "....ppPPPp......", "...pp.PPPp......",
               "..oo..PPPp......", "..o...OOOOO.....", "......oOOOOO...."],
    "contact_b": [".....PPPPPP.....", ".....PPPPpp.....", "....PPP..ppp....", "...PPP....ppp...",
                  "...PP......pp...", "..OOO......ooo..", "..OO.......oooo."],
    "pass_b": [".....PPPPPP.....", ".....PPPPp......", "....PPPppp......", "...PP..ppp......",
               "..OO...ppp......", "..O....oooo.....", ".......ooooo...."],
}


def torso_side(img, arm):
    for y in range(15, 22):
        for x in range(5, 11):
            put(img, x, y, "U" if x == 5 else "t" if x == 10 else "T")
    for x in range(5, 11):
        put(img, x, 22, "W")
    grid(SIDE_ARM[arm], CHAR, img, ox=4, oy=15)


def finish_char(upper, lower, bob):
    """Gabung badan atas + kaki, beri garis tepi, lalu bayangan di belakang."""
    body = new(16, 32)
    if bob:                                     # badan naik 1 piksel saat kaki melintas
        body.alpha_composite(upper.crop((0, 1, 16, 32)), (0, 0))
        body.alpha_composite(upper.crop((0, 22, 16, 23)), (0, 22))
    else:
        body.alpha_composite(upper)
    body.alpha_composite(lower)
    outline(body, CHAR_INK)
    out = new(16, 32)
    shadow(out, 2, 13, 29, 31, 60)
    out.alpha_composite(body)
    return out


def make_player():
    sheets = {}

    # tampak depan & belakang: (kaki kiri, kaki kanan, tangan kiri, tangan kanan, badan naik?)
    poses = [(28, 28, 21, 21, False),   # 0 diam
             (29, 26, 20, 22, False),   # 1 kaki kiri maju, tangan kanan maju
             (28, 28, 21, 21, True),    # 2 melintas
             (26, 29, 22, 20, False),   # 3 kaki kanan maju, tangan kiri maju
             (28, 28, 21, 21, True)]    # 4 melintas
    for name, head, back in (("down", HEAD_FRONT, False), ("up", HEAD_BACK, True)):
        for i, (fl, fr, hl, hr, bob) in enumerate(poses):
            upper = new(16, 32)
            torso_front(upper, hl, hr, back)
            grid(head, CHAR, upper)
            lower = new(16, 32)
            legs_front(lower, fl, fr)
            sheets[f"player_{name}_{i}"] = finish_char(upper, lower, bob)

    # tampak samping (kanan); kiri = cermin
    side_poses = [("idle", "neutral", False), ("contact_a", "back", False), ("pass_a", "neutral", True),
                  ("contact_b", "forward", False), ("pass_b", "neutral", True)]
    for i, (legs, arm, bob) in enumerate(side_poses):
        upper = new(16, 32)
        torso_side(upper, arm)
        grid(HEAD_SIDE, CHAR, upper)
        lower = new(16, 32)
        grid(SIDE_LEGS[legs], CHAR, lower, oy=23)
        img = finish_char(upper, lower, bob)
        sheets[f"player_right_{i}"] = img
        sheets[f"player_left_{i}"] = img.transpose(Image.FLIP_LEFT_RIGHT)
    return sheets


# --------------------------------------------------------------------------- BANGUNAN & OBJEK

ROOF = [C(c) for c in ("#244b49", "#2f5d5a", "#3d7470", "#4f8a83", "#74b3a8")]
WALL = [C(c) for c in ("#a8875a", "#c4a473", "#dcc191", "#ecd6ab")]


def make_house():
    """Rumah 80x60 (5x3 tile + atap yang menjulang ke atas)."""
    img = new(80, 60)
    px = img.load()
    rng = random.Random(5)

    # cerobong (di belakang atap)
    brick, brick_d, mortar = C("#9c5b45"), C("#7d4434"), C("#c9a58a")
    for y in range(0, 16):
        for x in range(57, 65):
            m = (y % 3 == 2) or ((x + (y // 3) * 2) % 4 == 0)
            px[x, y] = mortar if m else (brick_d if x >= 63 else brick)
    for x in range(56, 66):
        px[x, 0] = C("#4a2a20")
        px[x, 1] = C("#5e3528")

    # atap genteng (trapesium, sisik berselang-seling)
    for y in range(4, 34):
        t = (y - 4) / 29
        x0, x1 = round(7 - 7 * t), round(72 + 7 * t)
        row, ly = (y - 4) // 4, (y - 4) % 4
        for x in range(x0, x1 + 1):
            sx = (x + (row % 2) * 3) % 6
            c = ROOF[2]
            if ly == 3:
                c = ROOF[0] if sx in (0, 5) else ROOF[1]
            elif sx == 0:
                c = ROOF[1]
            elif ly == 0 and sx in (1, 2):
                c = ROOF[3]
            if x <= x0 + 1:
                c = ROOF[3] if ly != 3 else ROOF[1]   # sisi kiri kena cahaya
            if x >= x1 - 1:
                c = ROOF[1] if ly != 3 else ROOF[0]   # sisi kanan bayangan
            px[x, y] = c
    for x in range(7, 73):                                   # bubungan
        px[x, 4] = ROOF[4]
        px[x, 5] = ROOF[3] if x % 3 else ROOF[4]
    for x in range(0, 80):                                   # lis atap
        px[x, 32] = C("#5a3a22")
        px[x, 33] = C("#3e2615")

    # dinding papan kayu
    for y in range(34, 56):
        for x in range(3, 77):
            c = WALL[2]
            if (y - 34) % 4 == 3:
                c = WALL[1]
            elif (y - 34) % 4 == 0:
                c = WALL[3]
            seam_off = ((y - 34) // 4) * 7
            if (x + seam_off) % 19 == 0 and (y - 34) % 4 != 3:
                c = WALL[1]
            if x <= 4:
                c = WALL[3]
            if x >= 75:
                c = WALL[0]
            px[x, y] = c
    for x in range(3, 77):                                   # bayangan lis
        for y, a in ((34, 110), (35, 60)):
            r, g, b, _ = px[x, y]
            px[x, y] = (int(r * (1 - a / 255) + 30 * a / 255), int(g * (1 - a / 255) + 20 * a / 255),
                        int(b * (1 - a / 255) + 14 * a / 255), 255)

    # jendela + daun jendela + pot bunga
    def window(x0):
        frame, glass, glass_d, glare = C("#f3ead8"), C("#6fa3cc"), C("#3f6f99"), C("#bfe0f3")
        shut, shut_d = ROOF[3], ROOF[1]
        for y in range(38, 51):
            px[x0 - 3, y] = shut
            px[x0 - 2, y] = shut_d if y % 3 == 0 else shut
            px[x0 + 13, y] = shut
            px[x0 + 14, y] = shut_d if y % 3 == 0 else shut
        for y in range(38, 51):
            for x in range(x0 - 1, x0 + 13):
                px[x, y] = frame
        for y in range(39, 50):
            for x in range(x0, x0 + 12):
                if x in (x0 + 5, x0 + 6) or y == 44:
                    continue
                px[x, y] = glass_d if y in (39, 45) else glass
        for i in range(3):
            px[x0 + 1 + i, 42 - i] = glare
            px[x0 + 8 + i, 48 - i] = glare
        for x in range(x0 - 1, x0 + 13):                     # pot bunga
            px[x, 51] = C("#8a5a34")
            px[x, 52] = C("#6e4528")
        for i, x in enumerate(range(x0, x0 + 12)):
            px[x, 50] = C("#5c9c45")
            if i % 3 == 1:
                px[x, 49] = C("#d9534a") if i % 2 else C("#f2c94c")
                px[x, 50] = C("#a83a32") if i % 2 else C("#d9a62e")

    window(10)
    window(57)

    # kusen pintu (daun pintunya objek FarmhouseDoor)
    for y in range(40, 60):
        for x in range(31, 49):
            px[x, y] = C("#5a3a22") if (x in (31, 48) or y in (40, 41)) else C("#3e2615")
    for x in range(30, 50):
        px[x, 39] = C("#6e4528")

    # pondasi batu
    stone = [C("#7d7a74"), C("#959189"), C("#aaa59b")]
    for y in range(56, 60):
        for x in range(2, 78):
            if 31 <= x <= 48:
                continue
            c = stone[1]
            if (x + (y - 56) // 2 * 3) % 6 == 0 or y == 59:
                c = stone[0]
            elif y == 56:
                c = stone[2]
            px[x, y] = c

    outline(img, INK)
    return img


def make_door():
    img = new(16, 16)
    px = img.load()
    plank = [C("#8a5634"), C("#9c6640")]
    seam, brace, brace_l = C("#5e3720"), C("#6b3f22"), C("#a8744a")
    for y in range(2, 16):
        for x in range(1, 15):
            px[x, y] = seam if (x - 1) % 4 == 3 else plank[((x - 1) // 4) % 2]
    for y in (6, 12):
        for x in range(1, 15):
            px[x, y] = brace
            px[x, y - 1] = brace_l
    for x in range(4, 12):                                   # jendela kecil melengkung
        px[x, 3] = C("#3f6f99")
    for x in range(5, 11):
        px[x, 2] = C("#6fa3cc")
    px[6, 3] = C("#bfe0f3")
    px[11, 9] = C("#f0c75a")                                 # gagang
    px[11, 10] = C("#8a6520")
    for y in range(0, 16):                                   # kusen
        px[0, y] = C("#5a3a22")
        px[15, y] = C("#5a3a22")
    for x in range(0, 16):
        px[x, 0] = C("#5a3a22")
        px[x, 1] = C("#6e4528")
    return img


def make_bin():
    """Kotak kirim 32x24 (2 tile lebar, menjulang 8 piksel)."""
    img = new(32, 24)
    px = img.load()
    shadow(img, 1, 30, 19, 23, 60)
    wood = [C("#6e4528"), C("#8a5a34"), C("#9a6a40"), C("#b07c4e"), C("#c99460")]
    for y in range(9, 21):                                   # badan
        for x in range(2, 30):
            ly = (y - 9) % 4
            px[x, y] = wood[1] if ly == 3 else wood[3] if ly == 0 else wood[2]
            if x >= 28:
                px[x, y] = wood[0] if ly == 3 else wood[1]
    for y in range(3, 10):                                   # tutup
        for x in range(1, 31):
            c = wood[3]
            if y == 3:
                c = wood[4]
            elif y >= 8:
                c = wood[1]
            elif (x + y) % 9 == 0:
                c = wood[2]
            px[x, y] = c
    metal, metal_l, rivet = C("#6f757c"), C("#aab2ba"), C("#dfe4e8")
    for bx in (5, 25):                                       # sabuk besi
        for y in range(3, 21):
            px[bx, y] = metal_l
            px[bx + 1, y] = metal
        for y in (5, 13, 18):
            px[bx, y] = rivet
    for y in range(12, 18):                                  # plat kuningan + koin
        for x in range(13, 19):
            edge = x in (13, 18) or y in (12, 17)
            px[x, y] = C("#a87a24") if edge else C("#e0b04a")
    for x, y in ((15, 14), (16, 14), (15, 15), (16, 15)):
        px[x, y] = C("#fff0b0")
    px[15, 14] = C("#ffffff")
    outline(img, INK)
    return img


def make_hoedirt(wet, mask):
    """
    Tanah dicangkul yang bisa menyambung. mask: atas=1, kanan=2, bawah=4, kiri=8.
    Sisi yang bersambung digambar sampai tepi (tanpa garis), sisi lain diberi tepi membulat.
    """
    pal = ([C(c) for c in ("#2c1b10", "#3e2717", "#4d3220", "#5f412b", "#725238")] if wet
           else [C(c) for c in ("#4f321f", "#65412a", "#7a5235", "#8f6442", "#a57650")])
    n, e, s, w = mask & 1, mask & 2, mask & 4, mask & 8
    x0, x1 = (0 if w else 1), (15 if e else 14)
    y0, y1 = (0 if n else 1), (15 if s else 14)
    img = new(16, 16)
    px = img.load()
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            band = [4, 3, 2, 1][y % 4]                      # alur cangkul horizontal
            if ((x * 7 + y * 13) * 2654435761) % 100 < 16:  # bintik acak tapi sama di semua tile
                band = max(1, band - 1)
            px[x, y] = pal[band]
    if not n:
        for x in range(x0, x1 + 1):
            px[x, y0] = pal[4]                              # bibir atas bedeng kena cahaya
    if not s:
        for x in range(x0, x1 + 1):
            px[x, y1] = pal[1]                              # sisi bawah bedeng gelap
    for cx, cy, a, b in ((x0, y0, n, w), (x1, y0, n, e), (x0, y1, s, w), (x1, y1, s, e)):
        if not a and not b:
            px[cx, cy] = (0, 0, 0, 0)                       # sudut membulat
    if wet:
        rng = random.Random(9 + mask)
        for _ in range(4):
            px[rng.randrange(2, 14), rng.randrange(2, 14)] = C("#7d7480")
    return outline(img, "#1e120a" if wet else "#3b2414")


# --------------------------------------------------------------------------- TANAMAN

PLANT_INK = "#22331b"

CROP_PAL = {
    "parsnip":     {"L": "#6cb043", "l": "#467f2d", "g": "#a2d862", "s": "#8bb35a",
                    "C": "#f1e0b0", "c": "#d7bc84", "d": "#b0905a", "w": "#fff7df"},
    "potato":      {"L": "#5e9e3c", "l": "#3c742a", "g": "#8cc65c", "s": "#7aa24c",
                    "F": "#efe8f6", "f": "#b49ad6", "y": "#f2d14b", "B": "#c08a52", "b": "#8f6436"},
    "kale":        {"L": "#4f9a80", "l": "#2f6e5a", "g": "#80c8a8", "s": "#b8d98c", "v": "#a7dcc5"},
    "cauliflower": {"L": "#6aa04a", "l": "#44773a", "g": "#93c86c", "s": "#9fbf6a",
                    "W": "#f6f2e4", "w": "#d9d1b8", "h": "#ffffff"},
}

SEED_MOUND = [""] * 10 + [
    "......bmmb......",
    ".....bmsmsmb....",
    "......bbbb......",
]

SPROUT = [""] * 8 + [
    ".....gL..Lg.....",
    "....gLl..lLg....",
    "......lLLl......",
    ".......Ll.......",
    ".......s........",
]

SMALL = [""] * 5 + [
    ".......g........",
    "......gL........",
    "...gL.Ll..Lg....",
    "..gLLl.l.lLLg...",
    "...lLLlLlLLl....",
    ".....llLll......",
    ".......L........",
    ".......s........",
]

BIG = [""] * 2 + [
    ".......gg.......",
    "......gLLg......",
    "..gg..LLlL..gg..",
    ".gLLL.lLl..LLLg.",
    ".LLLll.l..llLLL.",
    "..lLLLl.lLLLl...",
    "...llLLlLLll....",
    "..gL.lLLLl.Lg...",
    "..LLl.lLl.lLL...",
    "...llLLlLLll....",
    ".....llsll......",
]

MATURE = {
    "parsnip": [
        "................",
        "......g..g......",
        ".....gL.gL......",
        "...g.Ll.Ll.g....",
        "...Ll.LlLl.Lg...",
        "....LlLLLlLl....",
        ".gL..lLLLl..Lg..",
        "..LLl.lLl.lLL...",
        "....lLlLlLl.....",
        "......lLl.......",
        ".....wCCCc......",
        "....wCCCCCc.....",
        "....CCCCCCcd....",
        ".....cCCCcd.....",
        "......cddd......",
    ],
    "potato": [
        "......F.........",
        ".....FyF...F....",
        "..F...F...FyF...",
        ".FyF.gLLg..F....",
        "..F.gLLlLLg.....",
        "..gLLlLLlLLLg...",
        ".LLLll.l..llLL..",
        "..lLLLl.lLLLl...",
        "...llLLlLLll....",
        "..gL.lLLLl.Lg...",
        "..LLl.lLl.lLL...",
        "...llLLlLLll....",
        "..hBBb.ll.hBb...",
        "...bb......b....",
    ],
    "kale": [
        "................",
        "................",
        "....g..gg..g....",
        "...gLggLLggLg...",
        "..gLLLLlLLLLLg..",
        ".gLLlLLlLLlLLLg.",
        ".LLLLlLlLlLLlLL.",
        ".LlLLLLvLLLLLlL.",
        ".LLlLLlvlLLlLL..",
        ".lLLLlLvLlLLLl..",
        "..llLLLvLLLll...",
        "...lllLvLlll....",
        ".....llsll......",
        ".......s........",
    ],
    "cauliflower": [
        "................",
        "................",
        "................",
        ".....wWWWw......",
        "...wWWhWWWWw....",
        "..wWhWWWWhWWw...",
        "..WWWWWhWWWWW...",
        ".LwWWWWWWWWWwL..",
        ".LLwwWWWWWwwLL..",
        ".gLLLwwwwwLLLg..",
        ".lLLlLLLLLlLLl..",
        "..llLLlLLlLll...",
        "....lllLLlll....",
        "......llll......",
    ],
}

DEAD = [""] * 5 + [
    "......d...d.....",
    ".....dD..dD.....",
    "....D.dD.D......",
    "......dDd..D....",
    "...D..dDd.D.....",
    "....DddDddD.....",
    ".....dDDDd......",
    ".......D........",
]


def plant(rows, pal):
    img = new(16, 16)
    pal = dict(pal)
    pal.setdefault("m", "#8a5a3a")
    pal.setdefault("b", "#5c3b25")
    pal.setdefault("s", "#7aa24c")
    pal.setdefault("h", "#ddb27a")
    grid([r for r in rows], pal, img)
    return outline(img, PLANT_INK)


def make_crops():
    out = {}
    for crop, pal in CROP_PAL.items():
        mound = new(16, 16)
        grid(SEED_MOUND, {"b": "#3e2717", "m": "#6b4a30", "s": "#efe0b0"}, mound)
        out[f"crop_{crop}_0"] = mound
        out[f"crop_{crop}_1"] = plant(SPROUT, pal)
        out[f"crop_{crop}_2"] = plant(SMALL, pal)
        out[f"crop_{crop}_3"] = plant(BIG, pal)
        out[f"crop_{crop}_4"] = plant(MATURE[crop], pal)
    out["crop_dead"] = plant(DEAD, {"D": "#8a6a3e", "d": "#6b4f2c"})
    return out


# --------------------------------------------------------------------------- IKON ITEM

def icon(rows, pal):
    img = new(16, 16)
    grid(rows, pal, img)
    return outline(img, INK)


PRODUCE_ICONS = {
    "parsnip": ([
        "................",
        "......g..g......",
        ".....gL.gL......",
        "......LlL.g.....",
        ".......lLL......",
        "......cwCCc.....",
        ".....cwCCCCc....",
        ".....cwCCCCc....",
        ".....cCCCCCd....",
        "......cCCCcd....",
        "......cCCCd.....",
        ".......cCCd.....",
        ".......cCd......",
        "........cd......",
        "........d.......",
    ], CROP_PAL["parsnip"]),
    "potato": ([
        "................",
        "................",
        "................",
        "................",
        "......hhhh......",
        "....hhBBBBBh....",
        "...hBBBBBBBBb...",
        "...hBBeBBBBBb...",
        "...BBBBBBBeBBb..",
        "...BBBBBBBBBBb..",
        "...bBBeBBBBBbb..",
        "....bBBBBBBbb...",
        ".....bbbbbbb....",
    ], {"h": "#ddb27a", "B": "#c08a52", "b": "#97683a", "e": "#7a5230"}),
    "kale": ([
        "................",
        ".....g.gg.g.....",
        "....gLgLLgLg....",
        "...gLLLLLLLLg...",
        "...LLlLLlLLlL...",
        "..gLLLlLLlLLLg..",
        "..LLlLLvLLlLLL..",
        "...LLLLvLLLLl...",
        "...lLlLvLlLLl...",
        "....lLLvLLll....",
        ".....llvll......",
        "......sss.......",
        "......sss.......",
        ".......s........",
    ], CROP_PAL["kale"]),
    "cauliflower": ([
        "................",
        "................",
        "................",
        ".....wWWWw......",
        "...wWWhWWWWw....",
        "..wWhWWWWhWWw...",
        "..WWWWWhWWWWW...",
        ".LwWWWWWWWWWwL..",
        ".LLwwWWWWWwwLL..",
        ".gLLLwwwwwLLLg..",
        ".lLLlLLLLLlLLl..",
        "..llLLlLLlLll...",
        "....lllLLlll....",
        "......llll......",
    ], CROP_PAL["cauliflower"]),
}

SEED_ACCENT = {"parsnip": "#e0a040", "potato": "#a8743e", "kale": "#3f8a6a", "cauliflower": "#6fa6cc"}
SEED_DOT = {"parsnip": "#f1e0b0", "potato": "#c08a52", "kale": "#4f9a80", "cauliflower": "#f6f2e4"}


def seed_packet(crop):
    rows = [
        "................",
        "................",
        "...pqpqpqpqp....",
        "...aaaaaaaaaP...",
        "...aAAAAAAAaP...",
        "...ppppppppqP...",
        "...pppp.pppqP...",
        "...ppp...ppqP...",
        "...ppp...ppqP...",
        "...pppp.pppqP...",
        "...ppppppppqP...",
        "...pkpkpkppqP...",
        "...ppppppppqP...",
        "...qqqqqqqqqP...",
    ]
    pal = {"p": "#efe2c0", "q": "#d4c49a", "P": "#b8a676", "a": SEED_ACCENT[crop], "A": "#ffffff",
           "k": "#9a845a"}
    img = icon(rows, pal)
    # label bundar di tengah: warna hasil panen + daun kecil
    px = img.load()
    for x, y in ((7, 6), (6, 7), (7, 7), (8, 7), (6, 8), (7, 8), (8, 8), (7, 9)):
        px[x, y] = C(SEED_DOT[crop])
    px[6, 7] = C("#ffffff") if crop != "cauliflower" else C("#d9d1b8")
    px[8, 6] = C("#5c9c45")
    px[9, 5] = C("#3c742a")
    # garis aksen ditimpa warna tetap biar terbaca
    return img


def make_hoe():
    img = new(16, 16)
    px = img.load()
    wood, wood_d = C("#b07c4e"), C("#7a4a2a")
    for i in range(10):                                      # gagang diagonal
        x, y = 2 + i, 14 - i
        px[x, y] = wood
        px[x + 1, y] = wood_d
    blade = [
        "..........hMM...",
        ".........hmmmM..",
        "..........mmmMM.",
        "...........mMMM.",
        "............MM..",
    ]
    grid(blade, {"h": "#eef0f2", "m": "#c9ccd1", "M": "#8a8f96"}, img, oy=1)
    return outline(img, INK)


def make_can():
    rows = [
        "................",
        "................",
        ".....DDDD.......",
        "....D....D......",
        "....D....D......",
        "..LLLLLLLLL.....",
        "..LhBBBBBBBL..SS",
        "..BhBBBBBBBB.SS.",
        "..BhBBBBBBBBSS..",
        "..BBBBBBBBBSS...",
        "..BBBBBBBBBB....",
        "..BBBBBBBBBB....",
        "..DBBBBBBBBD....",
        "...DDDDDDDD.....",
    ]
    return icon(rows, {"B": "#6b9fb8", "L": "#9cc8dc", "D": "#45708a", "h": "#cde6f0", "S": "#557f96"})


# --------------------------------------------------------------------------- ALAM (pohon, semak, batu, rumput, bunga, teratai, kupu-kupu)

LEAF = [C(c) for c in ("#24482a", "#2f5f31", "#3f7a37", "#5a9a43", "#86c05a")]


def clump_foliage(img, clumps, light=(-0.4, -0.45)):
    """Daun dari banyak gumpalan bulat. Gumpalan belakang digambar dulu, depan menimpa -> terasa berlapis."""
    px = img.load()
    for cx, cy, r in clumps:
        lx, ly = cx + light[0] * r, cy + light[1] * r
        for y in range(img.height):
            for x in range(img.width):
                d = math.hypot(x - cx, y - cy)
                if d > r:
                    continue
                t = math.hypot(x - lx, y - ly) / (r * 1.35)
                c = LEAF[4] if t < 0.13 else LEAF[3] if t < 0.45 else LEAF[2] if t < 0.8 else LEAF[1]
                if d > r - 1.1 and y > cy:
                    c = LEAF[0]                     # tepi bawah gumpalan gelap
                px[x, y] = c


def sway_rows(img, amount, bottom):
    """Geser baris ke samping: pucuk bergeser paling jauh, pangkal diam (efek kena angin)."""
    out = new(img.width, img.height)
    for y in range(img.height):
        dx = round(amount * max(0.0, 1 - y / bottom) ** 1.3)
        row = img.crop((0, y, img.width, y + 1))
        out.paste(row, (dx, y), row)
    return out


def make_trees():
    rng = random.Random(21)
    canopy = new(32, 48)
    clumps = []
    for cy, xs, r in ((9, [11, 17, 22], 6.0), (15, [6, 12, 19, 25], 6.2), (22, [7, 15, 23], 6.6), (28, [11, 20], 6.0)):
        for cx in xs:
            clumps.append((cx + rng.uniform(-0.8, 0.8), cy + rng.uniform(-0.8, 0.8), r + rng.uniform(-0.4, 0.4)))
    clump_foliage(canopy, clumps)
    cp = canopy.load()
    for _ in range(14):                              # kilau daun
        x, y = rng.randrange(4, 28), rng.randrange(4, 24)
        if cp[x, y][3] and cp[x, y] in (LEAF[3], LEAF[2]):
            cp[x, y] = LEAF[4]

    trunk = new(32, 48)
    tp = trunk.load()
    bark = [C("#4e3420"), C("#6b4a2f"), C("#8a6240"), C("#a57a52")]
    for y in range(28, 46):
        for x in range(13, 19):
            c = bark[1]
            if x == 13:
                c = bark[2]
            if x == 18:
                c = bark[0]
            if (x + y * 3) % 7 == 0:
                c = bark[0]
            tp[x, y] = c
    for x, y in ((11, 45), (12, 45), (12, 44), (19, 44), (19, 45), (20, 45)):
        tp[x, y] = bark[1]                           # akar
    tp[14, 38] = bark[3]
    tp[14, 39] = bark[3]

    frames = {}
    for i, amt in enumerate((0, 1.6, -1.6)):
        tree = new(32, 48)
        tree.alpha_composite(trunk)
        tree.alpha_composite(sway_rows(canopy, amt, 34))
        outline(tree, "#1c2a19")
        out = new(32, 48)
        shadow(out, 4, 27, 42, 47, 70)
        out.alpha_composite(tree)
        frames[f"tree_{i}"] = out
    return frames


def make_bush():
    img = new(16, 20)
    clump_foliage(img, [(5, 10, 4.2), (10, 9, 4.4), (4, 14, 3.8), (12, 14, 3.8), (8, 13, 4.6)])
    px = img.load()
    for x, y in ((5, 9), (11, 12), (7, 15), (10, 8)):
        px[x, y] = C("#d9534a")                     # buah beri
        px[x, y + 1] = C("#962d27")
    px[4, 8] = C("#ffd0c8")
    outline(img, "#1c2a19")
    out = new(16, 20)
    shadow(out, 1, 14, 16, 19, 60)
    out.alpha_composite(img)
    return out


def make_rock():
    img = new(16, 16)
    px = img.load()
    stone = [C(c) for c in ("#5c6066", "#787d84", "#959aa1", "#b5b9be", "#d3d6da")]
    cx, cy, rx, ry = 8, 9.5, 6.4, 4.8
    for y in range(16):
        for x in range(16):
            dx, dy = (x - cx) / rx, (y - cy) / ry
            if dx * dx + dy * dy > 1 or y > 13:
                continue
            t = dx * 0.6 + dy * 0.8                 # cahaya dari kiri atas -> faset
            c = stone[4] if t < -0.75 else stone[3] if t < -0.2 else stone[2] if t < 0.35 else stone[1]
            if y == 13:
                c = stone[0]
            px[x, y] = c
    for x, y in ((9, 7), (9, 8), (10, 9), (10, 10)):  # retakan
        px[x, y] = stone[0]
    for x, y in ((4, 6), (5, 5), (6, 5), (5, 6)):     # lumut
        px[x, y] = C("#6a9a46")
    outline(img, "#25272b")
    out = new(16, 16)
    shadow(out, 1, 14, 12, 15, 60)
    out.alpha_composite(img)
    return out


def make_tufts():
    blades = [(4, 7, -2), (6, 9, -1), (7, 6, 0), (8, 10, 1), (10, 8, 1), (11, 6, 2), (9, 7, -1)]
    frames = {}
    for i, sway in enumerate((0, 1.5, -1.5)):
        img = new(16, 16)
        px = img.load()
        for bx, h, lean in blades:
            for k in range(h + 1):
                f = k / h
                x = round(bx + (lean + sway) * f ** 1.6)
                px[x, 14 - k] = GRASS[min(4, int(f * 4.4))]
        outline(img, "#2b4a24")
        frames[f"tuft_{i}"] = img
    return frames


def make_flowers():
    petals = [("#f4efe2", "#d8d0c0", "#e9b23a"), ("#ef8fb0", "#c9668a", "#f6d65a"), ("#8fb7ee", "#5f88c8", "#f2f2f2")]
    stems = [(5, 8, -1), (8, 10, 0), (11, 7, 1)]
    frames = {}
    for v, (pl, pd, ctr) in enumerate(petals):
        for i, sway in enumerate((0, 1, -1)):
            img = new(16, 16)
            px = img.load()
            for bx, h, lean in stems:
                tip = None
                for k in range(h + 1):
                    f = k / h
                    x = round(bx + (lean + sway) * f * f)
                    px[x, 14 - k] = GRASS[1] if k < h * 0.5 else GRASS[2]
                    tip = (x, 14 - k)
                    if k == 2:
                        px[x + 1, 14 - k] = GRASS[3]   # daun kecil
                tx, ty = tip
                for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                    px[tx + dx, ty + dy] = C(pl)
                px[tx + 1, ty + 1] = C(pd)
                px[tx, ty] = C(ctr)
            outline(img, "#2a3b1f")
            frames[f"flowers_{v}_{i}"] = img
    return frames


def make_lily(variant):
    img = new(16, 16)
    px = img.load()
    pad = [C("#3f7a32"), C("#5f9e46"), C("#7bbb5a"), C("#a6d67a")]
    cx, cy, r = 8, 9, 5.2
    for y in range(16):
        for x in range(16):
            d = math.hypot(x - cx, y - cy)
            if d > r:
                continue
            ang = math.atan2(y - cy, x - cx)
            if -0.35 < ang < 0.35 and d > 1:          # celah khas daun teratai
                continue
            t = math.hypot(x - (cx - 2), y - (cy - 2)) / (r * 1.4)
            px[x, y] = pad[3] if t < 0.2 else pad[2] if t < 0.55 else pad[1] if t < 0.85 else pad[0]
    if variant == 1:
        for x, y in ((7, 7), (9, 7), (8, 6), (7, 9), (9, 9)):
            px[x, y] = C("#f2a6c2")
        px[8, 7] = C("#fbd3e2")
        px[8, 8] = C("#f2d14b")
        px[8, 9] = C("#fbd3e2")
    outline(img, "#1d3d2a")
    return img


def make_butterflies():
    rows_open = [".......", "Aa.K.aA", "AAaKaAA", ".AaKaA.", "..a.a..", "......."]
    rows_closed = ["...K...", "..AKA..", "..AKA..", "..aKa..", "...K...", "......."]
    colors = [{"A": "#f0a03a", "a": "#b8641e", "K": "#2a1b18"},
              {"A": "#eef3ff", "a": "#8fb7e8", "K": "#2a1b18"}]
    out = {}
    for c, pal in enumerate(colors):
        out[f"butterfly_{c}_0"] = grid(rows_open, pal, w=7)
        out[f"butterfly_{c}_1"] = grid(rows_closed, pal, w=7)
    return out


# --------------------------------------------------------------------------- MAIN

def main():
    print("Output:", OUT)

    # hapus PNG lama supaya tidak ada sisa gambar yang sudah tidak dipakai
    for folder, _dirs, files in os.walk(OUT):
        for f in files:
            if f.endswith(".png"):
                os.remove(os.path.join(folder, f))

    # tile
    for i in range(8):
        save(grass_tile(100 + i, clover=(i == 5)), "Tiles", f"grass_{i}")
    save(grass_tile(200, flowers=2), "Tiles", "grass_8")
    save(grass_tile(201, flowers=1), "Tiles", "grass_9")
    for i in range(5):
        save(dirt_tile(300 + i), "Tiles", f"dirt_{i}")
    for i in range(4):
        save(path_tile(400 + i), "Tiles", f"path_{i}")
    for f in range(4):
        save(water_tile(f), "Tiles", f"water_{f}")

    n = grass_edge_north(11)
    save(n, "Tiles", "edge_grass_n")
    save(grass_edge_north(12).transpose(Image.ROTATE_180), "Tiles", "edge_grass_s")
    save(grass_edge_north(13).transpose(Image.ROTATE_90), "Tiles", "edge_grass_w")
    save(grass_edge_north(14).transpose(Image.ROTATE_270), "Tiles", "edge_grass_e")
    nw = grass_corner_nw()
    save(nw, "Tiles", "edge_grass_nw")
    save(nw.transpose(Image.FLIP_LEFT_RIGHT), "Tiles", "edge_grass_ne")
    save(nw.transpose(Image.FLIP_TOP_BOTTOM), "Tiles", "edge_grass_sw")
    save(nw.transpose(Image.ROTATE_180), "Tiles", "edge_grass_se")

    wn, ws_, we, ww = water_edges()
    save(wn, "Tiles", "edge_water_n")
    save(ws_, "Tiles", "edge_water_s")
    save(we, "Tiles", "edge_water_e")
    save(ww, "Tiles", "edge_water_w")

    for name, img in fence_parts().items():
        save(img, "Tiles", name)

    # karakter
    for name, img in make_player().items():
        save(img, "Characters", name)

    # objek
    save(make_house(), "Objects", "house")
    save(make_door(), "Objects", "door")
    save(make_bin(), "Objects", "bin")
    for mask in range(16):
        save(make_hoedirt(False, mask), "Objects", f"hoedirt_dry_{mask}")
        save(make_hoedirt(True, mask), "Objects", f"hoedirt_wet_{mask}")

    # alam
    for name, img in {**make_trees(), **make_tufts(), **make_flowers(), **make_butterflies()}.items():
        save(img, "Nature", name)
    save(make_bush(), "Nature", "bush")
    save(make_rock(), "Nature", "rock")
    save(make_lily(0), "Nature", "lily_0")
    save(make_lily(1), "Nature", "lily_1")

    for name, img in make_crops().items():
        save(img, "Crops", name)

    # ikon item (nama = Item.SpriteKey)
    save(make_hoe(), "Items", "tool_hoe")
    save(make_can(), "Items", "tool_watering_can")
    for crop, (rows, pal) in PRODUCE_ICONS.items():
        save(icon(rows, pal), "Items", f"produce_{crop}")
        save(seed_packet(crop), "Items", f"seed_{crop}_seeds")

    print("Selesai.")


if __name__ == "__main__":
    main()
