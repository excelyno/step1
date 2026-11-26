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

from PIL import Image, ImageEnhance, ImageDraw

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


# MVP 4: folder yang benda-bendanya diberi "volume" (cahaya dari kiri-atas). Tile & ikon tidak.
VOLUME_FOLDERS = ("Objects", "Nature", "Characters", "Crops")
FLAT_SPRITES = ("hoedirt", "rug", "doormat", "lily")   # benda rata di tanah: tanpa volume


def is_outline(p):
    """Piksel garis tepi = sangat gelap."""
    return p[0] * 0.3 + p[1] * 0.59 + p[2] * 0.11 < 62


def tint(p, k):
    """k > 0 = lebih terang (ke arah kuning hangat), k < 0 = lebih gelap (ke arah ungu tua, bukan hitam)."""
    r, g, b, a = p
    if k > 0:
        return (int(r + (255 - r) * k), int(g + (240 - g) * k), int(b + (200 - b) * k * 0.6), a)
    k = -k
    return (int(r * (1 - k) + 40 * k * 0.3), int(g * (1 - k)), int(b * (1 - k) + 60 * k * 0.3), a)


def volume_shade(img):
    """
    MVP 4: kesan 3D. Cahaya datang dari kiri-atas (sama dengan arah bayangan bangunan di game):
    pinggir dalam yang menghadap kiri/atas sedikit terang, yang menghadap kanan/bawah sedikit gelap.
    """
    src = img.copy()
    s, d = src.load(), img.load()
    w, h = img.size

    def solid(x, y):
        return 0 <= x < w and 0 <= y < h and s[x, y][3] > 200 and not is_outline(s[x, y])

    for y in range(h):
        for x in range(w):
            p = s[x, y]
            if p[3] <= 200 or is_outline(p):
                continue
            lit = not solid(x - 1, y) or not solid(x, y - 1)
            dark = not solid(x + 1, y) or not solid(x, y + 1)
            if lit and not dark:
                d[x, y] = tint(p, 0.16)
            elif dark and not lit:
                d[x, y] = tint(p, -0.2)
    return img


def grade(img):
    """MVP 4: warna sedikit lebih kaya & hangat untuk SEMUA gambar (gaya sore yang hangat)."""
    alpha = img.getchannel("A")
    rgb = ImageEnhance.Color(img.convert("RGB")).enhance(1.15)     # saturasi +15%
    rgb = ImageEnhance.Contrast(rgb).enhance(1.05)
    r, g, b = rgb.split()
    r = r.point(lambda v: min(255, v + 6))                         # sedikit lebih hangat
    b = b.point(lambda v: max(0, v - 6))
    out = Image.merge("RGBA", (r, g, b, alpha))
    return out


def save(img, folder, name):
    path = os.path.join(OUT, folder)
    os.makedirs(path, exist_ok=True)
    if folder in VOLUME_FOLDERS and not name.startswith(FLAT_SPRITES):
        img = volume_shade(img.copy())
    img = grade(img)
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


def put(img, x, y, ch, pal):
    img.putpixel((x, y), C(pal[ch]))


def torso_front(img, hand_left, hand_right, back, pal):
    """Badan + lengan (tampak depan/belakang). hand_* = baris tangan: 21 normal, 22 maju, 20 mundur."""
    for y in range(15, 22):
        for x in range(4, 12):
            ch = "U" if x == 4 else "t" if x == 11 else "T"
            if x in (5, 10):
                ch = "W"                       # tali suspender
            put(img, x, y, ch, pal)
    if not back:
        put(img, 7, 15, "S", pal)              # leher kerah
        put(img, 8, 15, "s", pal)
    for x in range(4, 12):                     # sabuk + gesper
        put(img, x, 22, "Y" if x in (7, 8) and not back else "W", pal)
    for x in range(4, 12):
        put(img, x, 23, "P", pal)              # pinggul
    for x, inner, hand in ((2, 3, hand_left), (13, 12, hand_right)):
        for y in range(15, hand + 1):
            if y <= 17:
                put(img, x, y, "T", pal)       # lengan baju pendek
                put(img, inner, y, "t", pal)
            else:
                put(img, x, y, "S", pal)       # lengan bawah (kulit)
                put(img, inner, y, "s", pal)


def legs_front(img, foot_left, foot_right, pal):
    """Kaki tampak depan. foot_* = baris atas sepatu: 28 normal, 29 maju, 26 terangkat."""
    for x0, foot, inner in ((4, foot_left, 6), (9, foot_right, 9)):
        for y in range(24, foot):
            for x in range(x0, x0 + 3):
                put(img, x, y, "p" if x == inner else "P", pal)
        for x in range(x0, x0 + 3):
            put(img, x, foot, "O", pal)
            put(img, x, foot + 1, "o", pal)


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


def torso_side(img, arm, pal):
    for y in range(15, 22):
        for x in range(5, 11):
            put(img, x, y, "U" if x == 5 else "t" if x == 10 else "T", pal)
    for x in range(5, 11):
        put(img, x, 22, "W", pal)
    grid(SIDE_ARM[arm], pal, img, ox=4, oy=15)


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


def make_character(prefix, pal):
    """Semua karakter (petani + warga) memakai bentuk badan yang sama; bedanya hanya palet warna."""
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
            torso_front(upper, hl, hr, back, pal)
            grid(head, pal, upper)
            lower = new(16, 32)
            legs_front(lower, fl, fr, pal)
            sheets[f"{prefix}_{name}_{i}"] = finish_char(upper, lower, bob)

    # tampak samping (kanan); kiri = cermin
    side_poses = [("idle", "neutral", False), ("contact_a", "back", False), ("pass_a", "neutral", True),
                  ("contact_b", "forward", False), ("pass_b", "neutral", True)]
    for i, (legs, arm, bob) in enumerate(side_poses):
        upper = new(16, 32)
        torso_side(upper, arm, pal)
        grid(HEAD_SIDE, pal, upper)
        lower = new(16, 32)
        grid(SIDE_LEGS[legs], pal, lower, oy=23)
        img = finish_char(upper, lower, bob)
        sheets[f"{prefix}_right_{i}"] = img
        sheets[f"{prefix}_left_{i}"] = img.transpose(Image.FLIP_LEFT_RIGHT)
    return sheets


# Warga kota (MVP 2): palet petani dengan warna topi, rambut, baju, celana yang diganti.
NPC_PALETTES = {
    # Bu Sari, penjual bibit: topi bunga merah muda, baju oranye, celemek krem
    "sari": {**CHAR, "H": "#e88fa8", "h": "#c56a85", "j": "#8a3a55", "r": "#f6f0e0",
             "B": "#3a2418", "b": "#24160e", "n": "#5a3a28",
             "S": "#e8b890", "s": "#c99270", "c": "#e08c7c",
             "T": "#d9805a", "t": "#a85a3a", "U": "#f0a880", "W": "#efe6d0", "Y": "#efe6d0",
             "P": "#6a4a8a", "p": "#4a3266"},
    # Pak Darto, warga tua: topi anyaman, rambut abu-abu, kemeja putih, celana abu-abu
    "darto": {**CHAR, "H": "#d8c690", "h": "#b5a066", "j": "#7a6a40", "r": "#6b4428",
              "B": "#9a9a9a", "b": "#6a6a6a", "n": "#c0c0c0",
              "T": "#e4dcc4", "t": "#b0a890", "U": "#f6f2e6", "W": "#6b4428", "Y": "#d8b060",
              "P": "#5e6068", "p": "#41434a"},
    # Raka, pemancing muda (MVP 4): topi bucket hijau, rambut hitam, kaus biru, celana pendek cokelat
    "raka": {**CHAR, "H": "#6f9a4e", "h": "#52773a", "j": "#33502a", "r": "#d9c38a",
             "B": "#2a1a12", "b": "#170e09", "n": "#4a3020",
             "T": "#3d7fb8", "t": "#2a5c8a", "U": "#62a6dc", "W": "#5a3a22", "Y": "#c9a048",
             "P": "#8a6440", "p": "#634528"},
}


def make_player():
    return make_character("player", CHAR)


# --------------------------------------------------------------------------- BANGUNAN & OBJEK

ROOF = [C(c) for c in ("#244b49", "#2f5d5a", "#3d7470", "#4f8a83", "#74b3a8")]
WALL = [C(c) for c in ("#a8875a", "#c4a473", "#dcc191", "#ecd6ab")]
ROOF_RED = [C(c) for c in ("#5a2420", "#73302a", "#8f3e33", "#a8544a", "#cf7f6e")]    # rumah warga (MVP 2)
ROOF_BLUE = [C(c) for c in ("#25385c", "#304873", "#3e5c8f", "#5274a8", "#7ea0cf")]


def make_house(roof=ROOF):
    """Rumah 80x60 (5x3 tile + atap yang menjulang ke atas). roof = palet 5 warna genteng."""
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
            c = roof[2]
            if ly == 3:
                c = roof[0] if sx in (0, 5) else roof[1]
            elif sx == 0:
                c = roof[1]
            elif ly == 0 and sx in (1, 2):
                c = roof[3]
            if x <= x0 + 1:
                c = roof[3] if ly != 3 else roof[1]   # sisi kiri kena cahaya
            if x >= x1 - 1:
                c = roof[1] if ly != 3 else roof[0]   # sisi kanan bayangan
            px[x, y] = c
    for x in range(7, 73):                                   # bubungan
        px[x, 4] = roof[4]
        px[x, 5] = roof[3] if x % 3 else roof[4]
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
        shut, shut_d = roof[3], roof[1]
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


# ---- MVP 2: kota ----

SHOP_ROOF = [C(c) for c in ("#5a3418", "#74441f", "#8f5628", "#a86a34", "#cf8f50")]
AWNING = [C("#c8453a"), C("#9a3029"), C("#f3ead8"), C("#cfc4ae")]   # merah, merah gelap, putih, putih gelap


def shingle_roof(px, y0, y1, left, right, spread, pal):
    """Atap genteng trapesium: lebar di bawah. Sisik berselang-seling, sisi kiri terang, kanan gelap."""
    for y in range(y0, y1 + 1):
        t = (y - y0) / (y1 - y0)
        x0, x1 = round(left - spread * t), round(right + spread * t)
        row, ly = (y - y0) // 4, (y - y0) % 4
        for x in range(x0, x1 + 1):
            sx = (x + (row % 2) * 3) % 6
            c = pal[2]
            if ly == 3:
                c = pal[0] if sx in (0, 5) else pal[1]
            elif sx == 0:
                c = pal[1]
            elif ly == 0 and sx in (1, 2):
                c = pal[3]
            if x <= x0 + 1:
                c = pal[3] if ly != 3 else pal[1]
            if x >= x1 - 1:
                c = pal[1] if ly != 3 else pal[0]
            px[x, y] = c
    for x in range(left, right + 1):              # bubungan
        px[x, y0] = pal[4]
        px[x, y0 + 1] = pal[3] if x % 3 else pal[4]


def plank_wall(px, x0, x1, y0, y1):
    """Dinding papan kayu horizontal dengan sambungan acak-tapi-tetap."""
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            c = WALL[2]
            if (y - y0) % 4 == 3:
                c = WALL[1]
            elif (y - y0) % 4 == 0:
                c = WALL[3]
            if (x + ((y - y0) // 4) * 7) % 19 == 0 and (y - y0) % 4 != 3:
                c = WALL[1]
            if x <= x0 + 1:
                c = WALL[3]
            if x >= x1 - 1:
                c = WALL[0]
            px[x, y] = c


def make_shop():
    """Toko bibit 96x64 (6x3 tile + atap 16 piksel): papan nama, tenda garis-garis, dua etalase, pintu."""
    img = new(96, 64)
    px = img.load()

    shingle_roof(px, 2, 35, 6, 89, 6, SHOP_ROOF)
    for x in range(0, 96):                                   # lis atap
        px[x, 36] = C("#5a3a22")
        px[x, 37] = C("#3e2615")

    # papan nama di atap + 3 bungkus bibit
    for y in range(7, 28):
        for x in range(21, 75):
            edge = x in (21, 74) or y in (7, 27)
            px[x, y] = C("#5a3a22") if edge else (C("#c99460") if (y - 8) % 5 else C("#b07c4e"))
    for i, crop in enumerate(("parsnip", "tomato", "pumpkin")):
        img.alpha_composite(seed_packet(crop), (25 + i * 16, 9))

    plank_wall(px, 3, 92, 38, 59)

    # tenda garis-garis + tepi bergelombang
    for y in range(38, 47):
        for x in range(1, 95):
            stripe = (x // 6) % 2
            c = AWNING[0] if stripe == 0 else AWNING[2]
            if y == 38:
                c = AWNING[1] if stripe == 0 else AWNING[3]
            if y == 46 and x % 6 in (0, 5):
                continue                                     # lekukan bawah tenda
            px[x, y] = c
    for x in range(3, 93):                                   # bayangan tenda di dinding
        r, g, b, _ = px[x, 47]
        px[x, 47] = (r * 3 // 4, g * 3 // 4, b * 3 // 4, 255)

    # dua etalase berisi peti sayur
    def display(x0):
        frame, glass, glare = C("#f3ead8"), C("#7fb0d6"), C("#d4ecf8")
        for y in range(49, 59):
            for x in range(x0 - 1, x0 + 25):
                px[x, y] = frame
        for y in range(50, 58):
            for x in range(x0, x0 + 24):
                px[x, y] = glass
        veg = ["#f1e0b0", "#e0453a", "#e8892e", "#6a3a8a", "#f2d14b", "#5c9c45"]
        for i in range(3):                                   # peti + isi bulat
            cx = x0 + 2 + i * 8
            for x in range(cx, cx + 6):
                px[x, 55] = C("#8a5a34")
                px[x, 56] = C("#6e4528")
                px[x, 57] = C("#8a5a34")
            for j, x in enumerate(range(cx, cx + 6, 2)):
                col = C(veg[(i * 2 + j) % len(veg)])
                px[x, 54] = col
                px[x + 1, 54] = col
                px[x, 53] = col
        for i in range(3):
            px[x0 + 2 + i, 52 - i] = glare
    display(9)
    display(62)

    # pintu di tengah
    for y in range(48, 64):
        for x in range(40, 56):
            if x in (40, 55) or y in (48, 49):
                px[x, y] = C("#5a3a22")
            else:
                px[x, y] = C("#7a4a2a") if (x - 41) % 4 == 3 else C("#9c6640")
    px[52, 56] = C("#f0c75a")

    # pondasi batu
    stone = [C("#7d7a74"), C("#959189"), C("#aaa59b")]
    for y in range(60, 64):
        for x in range(2, 94):
            if 40 <= x <= 55:
                continue
            c = stone[1]
            if (x + (y - 60) // 2 * 3) % 6 == 0 or y == 63:
                c = stone[0]
            elif y == 60:
                c = stone[2]
            px[x, y] = c

    outline(img, INK)
    return img


def make_counter():
    """Meja toko 64x24 (4 tile lebar, barang di atas meja menjulang 8 piksel)."""
    img = new(64, 24)
    px = img.load()
    shadow(img, 1, 62, 19, 23, 60)
    wood = [C("#6e4528"), C("#8a5a34"), C("#9a6a40"), C("#b07c4e"), C("#c99460")]
    for y in range(8, 12):                                   # permukaan meja
        for x in range(1, 63):
            px[x, y] = wood[4] if y == 8 else wood[3]
    for y in range(12, 21):                                  # papan depan (vertikal)
        for x in range(2, 62):
            px[x, y] = wood[1] if (x - 2) % 6 == 5 else wood[2]
            if y == 20:
                px[x, y] = wood[0]

    # keranjang sayur (kiri) + bungkus bibit berdiri (kanan)
    def basket(x0, colors):
        for x in range(x0, x0 + 12):
            px[x, 7] = C("#a8743e")
            px[x, 6] = C("#c9935a") if x % 2 else C("#a8743e")
        for i, x in enumerate(range(x0 + 1, x0 + 11, 3)):
            col = C(colors[i % len(colors)])
            for dx, dy in ((0, 4), (1, 4), (0, 5), (1, 5), (2, 5), (1, 3)):
                px[x + dx, dy] = col
    basket(3, ["#e0453a", "#f2d14b", "#e0453a"])
    basket(17, ["#e8892e", "#6a3a8a", "#f1e0b0"])
    for i, crop in enumerate(("parsnip", "potato", "kale", "cauliflower", "tomato", "corn")):
        x0 = 33 + i * 5
        for y in range(2, 8):
            for x in range(x0, x0 + 4):
                px[x, y] = C(SEED_ACCENT[crop]) if y < 4 else C("#efe2c0")
        px[x0 + 1, 5] = C(SEED_DOT[crop])
        px[x0 + 2, 5] = C(SEED_DOT[crop])
    return outline(img, INK)


def make_sign():
    """Papan petunjuk 16x24: papan kayu di atas tiang."""
    img = new(16, 24)
    px = img.load()
    shadow(img, 3, 12, 20, 23, 60)
    for y in range(11, 22):                                  # tiang
        px[7, y] = C("#9a6a40")
        px[8, y] = C("#6e4528")
    for y in range(2, 12):                                   # papan
        for x in range(1, 15):
            c = C("#c99460") if y < 7 else C("#b07c4e")
            if y in (2, 11):
                c = C("#8a5a34")
            px[x, y] = c
    for y, (a, b) in ((5, (3, 12)), (7, (3, 10)), (9, (3, 11))):   # "tulisan"
        for x in range(a, b + 1):
            if x % 4 != 2:
                px[x, y] = C("#5a3a22")
    return outline(img, INK)


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
    # MVP 2: musim panas & gugur
    "tomato":      {"L": "#5a9a3c", "l": "#3a6e2a", "g": "#8cc65c", "s": "#7aa24c", "k": "#9a7a4a",
                    "R": "#e0453a", "r": "#a8302a", "h": "#ff9a88"},
    "chili":       {"L": "#4f8a34", "l": "#2f6422", "g": "#7ab84e", "s": "#6e9a44", "k": "#3c742a",
                    "C": "#e0362e", "c": "#a32620", "h": "#ff8a70", "O": "#f08a2e"},
    "corn":        {"L": "#6aae48", "l": "#43803a", "g": "#9ad46a", "s": "#8bb35a",
                    "Y": "#f2d14b", "y": "#c9a22e"},
    "eggplant":    {"L": "#5a9a48", "l": "#3a6e32", "g": "#8cc66c", "s": "#7aa24c", "k": "#4a7a32",
                    "V": "#6a3a8a", "v": "#462460", "h": "#a878c8"},
    "pumpkin":     {"L": "#5e9e3c", "l": "#3c742a", "g": "#8cc65c", "s": "#7aa24c", "k": "#6b8a2a",
                    "O": "#e8892e", "o": "#b8601c", "h": "#f8b860"},
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
    "tomato": [
        "................",
        ".......k........",
        "....gL.kLg......",
        "...gLLlkLLLg....",
        "..LLRrLkLlLL....",
        "..LRRrlkLRhL....",
        "...lrrLkLRRr....",
        "..gLLlLkLrrLg...",
        ".LLlRhLkLlLLL...",
        ".LlRRrlklLRhl...",
        "..lrrLLkLRRr....",
        "...llLLkLlrl....",
        ".....llkll......",
        ".......k........",
    ],
    "corn": [
        ".......gg.......",
        "......gLL.......",
        ".......LYy......",
        "...g...LYYy.....",
        "..gLl..LYYy.....",
        "...lLl.LYyl.g...",
        "....lLlLLl.gL...",
        ".......LllLLl...",
        "..gLL..Ll.l.....",
        ".gLlllLL........",
        "..l...LLlLLg....",
        ".......Ll..lLg..",
        ".......Ll...l...",
        ".......Ll.......",
        ".......ss.......",
    ],
    "eggplant": [
        "................",
        "......g..g......",
        "....gLLggLLg....",
        "...gLLlLLlLLg...",
        "..LLlLLlLLlLL...",
        "..lLL.lLl.LLl...",
        "...l.kl.lk.l....",
        "....VVv..VVv....",
        "...VhVVv.VhVv...",
        "...VVVVv.VVVv...",
        "...VVVVv..Vv....",
        "....VVv.........",
        ".....v..........",
    ],
    "chili": [
        "................",
        "......g..g......",
        ".....gLggLg.....",
        "....gLLlLLg.....",
        "...gLLlLLlLLg...",
        "...lLL.lLl.LL...",
        "....l.kl.lk.....",
        ".....Ch..Cc.....",
        "....CCh.CCc.....",
        "....OCC.CCc.....",
        "....OCc..Cc.....",
        ".....cc..c......",
        "......c.........",
    ],
    "pumpkin": [
        "................",
        "................",
        "...gL......gL...",
        "..gLLl..g.LLlg..",
        "..LLlL.gLlLLl...",
        "...ll.LLl.ll....",
        "......lk........",
        "....oOOkOOo.....",
        "...oOOhOOOOo....",
        "..oOOhOOoOOOo...",
        "..OOOOOoOOOOO...",
        "..OOoOOoOOoOO...",
        "..oOOoOOoOOoo...",
        "...ooooooooo....",
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
    # MVP 2
    "tomato": ([
        "................",
        "................",
        "......k.k.......",
        ".....kKkKk......",
        "....rRRkRRr.....",
        "...rRhhRRRRr....",
        "...RRhRRRRRr....",
        "...RRRRRRRRr....",
        "...RRRRRRRrr....",
        "...rRRRRRRrr....",
        "....rRRRRrr.....",
        ".....rrrrr......",
    ], {**CROP_PAL["tomato"], "k": "#3a6e2a", "K": "#5a9a3c"}),
    "corn": ([
        "................",
        "........Yy......",
        ".......YYYy.....",
        "......YhYYy.....",
        "......YYhYy.....",
        ".....LYYYYy.....",
        ".....LYhYYyl....",
        "....LlYYhYyl....",
        "....LlYYYYl.....",
        "....LLlYYyl.....",
        ".....LLlyl......",
        "......LLl.......",
        ".......Ll.......",
        "........l.......",
    ], {**CROP_PAL["corn"], "h": "#fff0a0"}),
    "eggplant": ([
        "................",
        "........kK......",
        ".......kKKk.....",
        "......kKkkK.....",
        ".....VVkkV......",
        "....VhVVVVv.....",
        "...VhVVVVVv.....",
        "...VVVVVVVv.....",
        "..VVVVVVVvv.....",
        "..VVVVVVvv......",
        "..vVVVVvv.......",
        "...vvvvv........",
    ], {**CROP_PAL["eggplant"], "K": "#6aa04a"}),
    "pumpkin": ([
        "................",
        "................",
        ".......kk.......",
        "........k.......",
        "....oOOkOOo.....",
        "...oOhOOOOOo....",
        "..oOhOoOOoOOo...",
        "..OOOOoOOoOOO...",
        "..OOOOoOOoOOO...",
        "..oOOOoOOoOOo...",
        "...oOOoOOoOo....",
        "....ooooooo.....",
    ], CROP_PAL["pumpkin"]),
    "chili": ([
        "................",
        "........kk......",
        ".......kKk......",
        "......kK.kK.....",
        ".....Ch..Cc.....",
        "....CCh.CCch....",
        "....OCC.CCc.....",
        "....OCc.Cc......",
        ".....cc..c......",
        "......c.........",
    ], {**CROP_PAL["chili"], "K": "#5a9a3c"}),
}

SEED_ACCENT = {"parsnip": "#e0a040", "potato": "#a8743e", "kale": "#3f8a6a", "cauliflower": "#6fa6cc",
               "tomato": "#d04a3a", "corn": "#e0b830", "eggplant": "#7a4a9a", "pumpkin": "#e07a28",
               "chili": "#d03a30"}
SEED_DOT = {"parsnip": "#f1e0b0", "potato": "#c08a52", "kale": "#4f9a80", "cauliflower": "#f6f2e4",
            "tomato": "#e0453a", "corn": "#f2d14b", "eggplant": "#6a3a8a", "pumpkin": "#e8892e",
            "chili": "#e0362e"}


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


# --------------------------------------------------------------------------- MVP 3: DALAM RUMAH

FLOOR = [C(c) for c in ("#5e3e26", "#7a5232", "#8e623c", "#a27248", "#b88658")]


def floor_tile(seed):
    """Lantai papan kayu: 4 papan horizontal setinggi 4 piksel, sambungan di posisi acak."""
    rng = random.Random(seed)
    img = new(16, 16)
    px = img.load()
    for board in range(4):
        y0 = board * 4
        base = rng.choice([2, 2, 3])
        seam = rng.randrange(2, 14)
        for y in range(y0, y0 + 4):
            for x in range(16):
                c = FLOOR[base]
                if y == y0 + 3:
                    c = FLOOR[0]                     # celah antar papan
                elif y == y0:
                    c = FLOOR[base + 1]              # tepi atas papan kena cahaya
                if x == seam and y != y0 + 3:
                    c = FLOOR[1]                     # sambungan ujung papan
                px[x, y] = c
        for _ in range(2):                           # serat kayu
            gx, gy = rng.randrange(15), y0 + rng.randrange(1, 3)
            px[gx, gy] = FLOOR[base - 1]
            px[gx + 1, gy] = FLOOR[base - 1]
    return img


def wall_front():
    """Dinding dalam: kertas dinding bergaris + motif daun kecil, plint kayu di bawah. Menyambung ke samping."""
    img = new(16, 16)
    px = img.load()
    paper, paper_d, leaf = C("#e2d6ae"), C("#d2c59a"), C("#9cb07e")
    for y in range(16):
        for x in range(16):
            c = paper if (x // 4) % 2 == 0 else paper_d
            if x % 8 == 1 and y % 6 == 2:
                c = leaf
            if x % 8 == 5 and y % 6 == 5:
                c = leaf
            px[x, y] = c
    for x in range(16):
        px[x, 11] = C("#a8744a")
        px[x, 12] = C("#8a5a34")
        px[x, 13] = C("#7a4c2c")
        px[x, 14] = C("#6e4528")
        px[x, 15] = C("#3e2615")
    return img


def wall_top():
    """Bagian atas dinding (yang tidak menghadap lantai): kayu gelap."""
    img = new(16, 16)
    px = img.load()
    for y in range(16):
        for x in range(16):
            px[x, y] = C("#3a281c") if (y % 8) != 7 else C("#2a1c12")
            if (x + y // 8 * 5) % 11 == 0 and y % 8 != 7:
                px[x, y] = C("#46301f")
    return img


def make_window():
    img = new(16, 16)
    px = img.load()
    frame, sky, sky_l, cloud = C("#f3ead8"), C("#8cc4e8"), C("#b6dcf2"), C("#ffffff")
    for y in range(1, 12):
        for x in range(2, 14):
            px[x, y] = frame
    for y in range(2, 11):
        for x in range(3, 13):
            if x in (7, 8) or y == 6:
                continue                             # palang jendela
            px[x, y] = sky_l if y < 5 else sky
    for x, y in ((4, 3), (5, 3), (5, 2), (10, 8), (11, 8)):
        px[x, y] = cloud
    for y in range(1, 13):                           # gorden
        for x in (1, 14):
            px[x, y] = C("#c8584a") if y % 3 else C("#a84038")
    for x in range(1, 15):
        px[x, 12] = C("#8a5a34")                     # ambang jendela
    return outline(img, INK)


def make_fireplace(frame):
    """Perapian batu 32x32 (2 tile lebar, menjulang 16 piksel). frame 0/1 = nyala api berbeda."""
    img = new(32, 32)
    px = img.load()
    stone = [C("#6a665f"), C("#857f76"), C("#a09a8f"), C("#b9b3a7")]
    for y in range(4, 32):
        for x in range(1, 31):
            c = stone[2]
            if (x + (y // 4) * 3) % 7 == 0 or y % 4 == 0:
                c = stone[1]
            if x <= 2:
                c = stone[3]
            if x >= 29:
                c = stone[0]
            px[x, y] = c
    for y in range(2, 6):                            # papan rak di atas
        for x in range(0, 32):
            px[x, y] = C("#8a5a34") if y < 4 else C("#6e4528")
    for y in range(14, 31):                          # lubang api
        for x in range(8, 24):
            arch = y == 14 and x in (8, 23)
            if not arch:
                px[x, y] = C("#1e140e")
    rng = random.Random(40 + frame)
    for x in range(10, 22):                          # api: makin ke atas makin kuning
        h = rng.randint(6, 12)
        for k in range(h):
            y = 29 - k
            t = k / h
            px[x, y] = C("#c8401e") if t < 0.35 else C("#f08a24") if t < 0.75 else C("#ffd860")
    for x in range(9, 23):                           # kayu bakar
        px[x, 30] = C("#5a3a22")
    px[12, 29] = C("#ffffff") if frame else C("#ffe9a0")
    return outline(img, INK)


def make_bed():
    """Kasur 16x32 (1x2 tile): sandaran kepala, bantal, selimut tambal sulam."""
    img = new(16, 32)
    px = img.load()
    wood, wood_d, wood_l = C("#8a5a34"), C("#6e4528"), C("#b07c4e")
    for y in range(0, 8):                            # sandaran kepala
        for x in range(1, 15):
            px[x, y] = wood_l if y == 0 else wood if x % 4 else wood_d
    for y in range(7, 31):                           # rangka
        px[1, y] = wood
        px[14, y] = wood_d
    for y in range(6, 11):                           # bantal
        for x in range(3, 13):
            px[x, y] = C("#ffffff") if y < 8 else C("#dfe4ea")
    quilt = [C("#4f7fb8"), C("#d9a441"), C("#c8584a"), C("#6aa04a")]
    for y in range(11, 30):                          # selimut tambal sulam 4x4
        for x in range(2, 14):
            px[x, y] = quilt[((x - 2) // 4 + (y - 11) // 4) % 4]
            if (x - 2) % 4 == 0 or (y - 11) % 4 == 0:
                r, g, b, _ = px[x, y]
                px[x, y] = (r * 4 // 5, g * 4 // 5, b * 4 // 5, 255)   # jahitan
    for x in range(2, 14):
        px[x, 11] = C("#f3ead8")                     # lipatan selimut
    for x in range(1, 15):                           # papan kaki
        px[x, 30] = wood
        px[x, 31] = wood_d
    return outline(img, INK)


def make_table():
    """Meja 32x24 (2 tile lebar, menjulang 8 piksel) dengan cangkir dan vas bunga."""
    img = new(32, 24)
    px = img.load()
    shadow(img, 2, 29, 20, 23, 60)
    for y in range(6, 12):                           # permukaan
        for x in range(1, 31):
            px[x, y] = C("#c99460") if y < 10 else C("#a8744a")
    for y in range(12, 22):                          # kaki
        for x in (3, 4, 27, 28):
            px[x, y] = C("#8a5a34") if x in (3, 27) else C("#6e4528")
    for x in range(5, 27):
        px[x, 12] = C("#7a4c2c")                     # bayangan bawah meja
    for y in range(3, 8):                            # cangkir
        for x in range(6, 10):
            px[x, y] = C("#f3ead8")
    px[10, 5] = C("#f3ead8")
    for y in range(2, 8):                            # vas + bunga
        for x in range(21, 25):
            px[x, y] = C("#4f7fb8") if y > 3 else (0, 0, 0, 0)
    for x, y, c in ((21, 1, "#ef8fb0"), (23, 0, "#f6d65a"), (24, 2, "#ef8fb0"), (22, 2, "#5c9c45")):
        px[x, y] = C(c)
    return outline(img, INK)


def make_plant():
    """Tanaman pot 16x24."""
    img = new(16, 24)
    clump_foliage(img, [(5, 9, 3.6), (10, 8, 3.8), (8, 5, 3.4), (7, 12, 3.4), (11, 12, 3.0)])
    px = img.load()
    for y in range(15, 23):                          # pot tanah liat
        w = 5 if y < 17 else 4
        for x in range(8 - w, 8 + w):
            px[x, y] = C("#c0643a") if x < 9 else C("#9a4a2a")
    for x in range(2, 14):
        px[x, 15] = C("#d97a4a")
    outline(img, "#1c2a19")
    out = new(16, 24)
    shadow(out, 2, 13, 20, 23, 60)
    out.alpha_composite(img)
    return out


def make_rug():
    """Karpet 48x32 (3x2 tile), datar di lantai: tepi bermotif, tengah polos dengan pola belah ketupat."""
    img = new(48, 32)
    px = img.load()
    red, red_d, cream, gold = C("#a8443a"), C("#7e2f28"), C("#efe0c0"), C("#d9a441")
    for y in range(2, 30):
        for x in range(2, 46):
            border = x < 6 or x > 41 or y < 6 or y > 25
            c = red_d if border else red
            if border and (x + y) % 4 == 0:
                c = gold
            if not border and (abs(x - 24) + abs(y - 16)) % 8 == 0:
                c = cream
            px[x, y] = c
    for y in range(2, 30, 2):                        # rumbai di ujung kiri & kanan
        px[1, y] = cream
        px[46, y] = cream
    return img


def make_doormat():
    img = new(16, 16)
    px = img.load()
    for y in range(4, 14):
        for x in range(1, 15):
            px[x, y] = C("#8a6a3a") if (y % 3) else C("#6e5230")
    for x in range(1, 15):
        px[x, 4] = C("#a8844a")
    return outline(img, "#3e2a18")


# --------------------------------------------------------------------------- MVP 3: TUNGGUL, RANTING, KERIKIL

def make_stump():
    img = new(16, 16)
    px = img.load()
    bark = [C("#4e3420"), C("#6b4a2f"), C("#8a6240")]
    for y in range(8, 15):                           # batang pendek
        for x in range(3, 13):
            px[x, y] = bark[2] if x == 3 else bark[0] if x == 12 else bark[1]
    for y in range(5, 10):                           # potongan atas: lingkaran tahun
        for x in range(3, 13):
            d = math.hypot((x - 7.5) / 5, (y - 7) / 2.4)
            if d <= 1:
                px[x, y] = C("#e2c08a") if int(d * 3) % 2 == 0 else C("#c9a26a")
    for x in (2, 13):
        px[x, 14] = bark[1]                          # akar
    outline(img, "#2a1c12")
    out = new(16, 16)
    shadow(out, 1, 14, 12, 15, 60)
    out.alpha_composite(img)
    return out


def make_twig(variant):
    img = new(16, 16)
    px = img.load()
    rng = random.Random(60 + variant)
    wood, wood_d = C("#8a6240"), C("#5e4028")
    for _ in range(2):                               # dua batang ranting bersilang
        x0, y0 = rng.randint(2, 5), rng.randint(8, 12)
        x1, y1 = rng.randint(10, 13), rng.randint(6, 12)
        for i in range(12):
            t = i / 11
            x, y = round(x0 + (x1 - x0) * t), round(y0 + (y1 - y0) * t)
            px[x, y] = wood
            px[x, y + 1] = wood_d
        px[x1, y1 - 1] = C("#6aa04a")                # daun kecil masih menempel
    return outline(img, "#2a1c12")


def make_pebble(variant):
    img = new(16, 16)
    px = img.load()
    stone = [C("#6a6e74"), C("#8a8f96"), C("#aab0b6"), C("#c8cdd2")]
    spots = [(6, 10, 3.2, 2.2), (11, 11, 2.0, 1.5)] if variant == 0 else [(8, 10, 3.6, 2.4), (4, 12, 1.6, 1.2)]
    for cx, cy, rx, ry in spots:
        for y in range(16):
            for x in range(16):
                dx, dy = (x - cx) / rx, (y - cy) / ry
                if dx * dx + dy * dy <= 1:
                    t = dx * 0.6 + dy * 0.8
                    px[x, y] = stone[3] if t < -0.6 else stone[2] if t < 0 else stone[1]
    outline(img, "#25272b")
    out = new(16, 16)
    shadow(out, 2, 13, 11, 14, 50)
    out.alpha_composite(img)
    return out


# --------------------------------------------------------------------------- MVP 3: IKON ALAT, BAHAN, IKAN

def make_axe():
    img = new(16, 16)
    px = img.load()
    for i in range(11):                              # gagang diagonal
        x, y = 2 + i, 14 - i
        px[x, y] = C("#b07c4e")
        px[x + 1, y] = C("#7a4a2a")
    head = [
        "........MMh.....",
        ".......MmmMh....",
        "......MmmmmMh...",
        ".......mmmmMM...",
        "........mmM.....",
    ]
    grid(head, {"h": "#eef0f2", "m": "#b9bec5", "M": "#7d838b"}, img, oy=0)
    return outline(img, INK)


def make_pickaxe():
    img = new(16, 16)
    px = img.load()
    for i in range(11):
        x, y = 3 + i, 14 - i
        px[x, y] = C("#b07c4e")
        px[x + 1, y] = C("#7a4a2a")
    head = [
        ".....hmmmm......",
        "...hmm....mmm...",
        "..hm........mM..",
        ".hm..........M..",
        ".m..............",
    ]
    grid(head, {"h": "#eef0f2", "m": "#b9bec5", "M": "#7d838b"}, img, oy=1)
    return outline(img, INK)


def make_rod():
    img = new(16, 16)
    px = img.load()
    for i in range(13):                              # joran panjang
        x, y = 1 + i, 14 - i
        px[x, y] = C("#c9935a") if i > 4 else C("#6e4528")
    for i in range(10):                              # senar menjuntai
        px[14, 2 + i] = C("#e8e8e8")
    px[14, 12] = C("#e0453a")                        # kail merah
    px[13, 12] = C("#ffffff")
    for x, y in ((4, 9), (5, 9), (4, 10), (5, 10)):  # gulungan
        px[x, y] = C("#9aa2aa")
    return outline(img, INK)


def make_wood():
    img = new(16, 16)
    px = img.load()
    bark, bark_d, ring, ring_d = C("#8a6240"), C("#5e4028"), C("#e2c08a"), C("#b8925a")
    for x0, y0 in ((1, 8), (5, 3)):                  # dua batang kayu bertumpuk
        for y in range(y0, y0 + 5):
            for x in range(x0, x0 + 10):
                px[x, y] = bark if y < y0 + 3 else bark_d
        for y in range(y0, y0 + 5):                  # ujung potongan
            for x in range(x0 + 8, x0 + 11):
                d = math.hypot((x - (x0 + 9.5)) / 1.6, (y - (y0 + 2)) / 2.5)
                if d <= 1:
                    px[x, y] = ring if int(d * 3) % 2 == 0 else ring_d
    return outline(img, INK)


def make_stone_item():
    img = new(16, 16)
    px = img.load()
    stone = [C("#6a6e74"), C("#8a8f96"), C("#aab0b6"), C("#d3d6da")]
    for cx, cy, rx, ry in ((6, 10, 4.5, 3.2), (11, 11, 3.2, 2.6), (8.5, 6.5, 3.2, 2.6)):
        for y in range(16):
            for x in range(16):
                dx, dy = (x - cx) / rx, (y - cy) / ry
                if dx * dx + dy * dy <= 1:
                    t = dx * 0.6 + dy * 0.8
                    px[x, y] = stone[3] if t < -0.6 else stone[2] if t < 0.1 else stone[1]
    return outline(img, "#25272b")


FISH_ROUND = [   # ikan bertubuh lebar, menghadap kiri
    "................",
    "................",
    "................",
    "......DDDD......",
    "....DDBBBBDD..FF",
    "...DBEBBBBBBDFF.",
    "..DBBBBBBBBBBF..",
    "..WBBBBBBBBBBDF.",
    "...bbbbbbbbbbDFF",
    "....bbbbbbbbD..F",
    "......FbbF......",
    ".......F........",
]

FISH_LONG = [    # ikan bertubuh panjang (lele, belut, gabus)
    "................",
    "................",
    "................",
    "................",
    "...........FF...",
    "..DDDDDDDDDDDDF.",
    ".DBEBBBBBBBBBBDF",
    "WBBBBBBBBBBBBBBF",
    ".bbbbbbbbbbbbbF.",
    "..bbbbbbbbbbbF..",
    "....F....F......",
]

FISH_STYLE = {   # bentuk, palet (D punggung, B badan, b perut, F sirip)
    "carp":      (FISH_ROUND, {"D": "#b8602a", "B": "#e8892e", "b": "#f6c27a", "F": "#d0703a"}),
    "mujair":    (FISH_ROUND, {"D": "#3a4a5a", "B": "#5a6e80", "b": "#a8b8c0", "F": "#4a5868"}),
    "tilapia":   (FISH_ROUND, {"D": "#4a5a6a", "B": "#7a909a", "b": "#d0d8d8", "F": "#c05050"}),
    "gourami":   (FISH_ROUND, {"D": "#6a5a3a", "B": "#a89060", "b": "#e0d0a0", "F": "#8a7040"}),
    "catfish":   (FISH_LONG,  {"D": "#3a3428", "B": "#5e5440", "b": "#a89a7a", "F": "#4a4232"}),
    "eel":       (FISH_LONG,  {"D": "#3a4020", "B": "#6a7032", "b": "#b8b070", "F": "#4a5028"}),
    "snakehead": (FISH_LONG,  {"D": "#2a3a2a", "B": "#4e6a48", "b": "#c0c8a0", "F": "#3a4a32"}),
}


def make_fish(fish_id):
    rows, pal = FISH_STYLE[fish_id]
    pal = {**pal, "E": "#1c1c1c", "W": "#fff0d0"}
    img = icon(rows, pal)
    if fish_id == "catfish":                         # kumis lele
        px = img.load()
        for x, y in ((0, 8), (1, 9), (2, 10), (0, 5)):
            px[x, y] = C("#2a241a")
    return img


# --------------------------------------------------------------------------- MVP 4: LAMPU, TANDA QUEST, HATI

def make_lamppost():
    """Tiang lampu besi 16x32: alas batu, tiang, lentera kaca kuning hangat."""
    img = new(16, 32)
    px = img.load()
    iron, iron_l, iron_d = C("#3c3a48"), C("#5e5c70"), C("#2a2834")
    stone, stone_l, stone_d = C("#8d8a86"), C("#b0ada6"), C("#66625e")
    for y in range(27, 31):                                   # alas batu
        for x in range(4, 12):
            px[x, y] = stone_l if y == 27 else stone_d if y == 30 or x == 11 else stone
    for y in range(11, 27):                                   # tiang
        px[7, y] = iron_l
        px[8, y] = iron
    for x in range(6, 10):                                    # cincin hias
        px[x, 18] = iron_d
        px[x, 25] = iron_d
    for x in range(5, 11):                                    # dudukan lentera
        px[x, 10] = iron
    glow, glow_core, glow_d = C("#ffcf6a"), C("#fff3c4"), C("#e89a3a")
    for y in range(4, 10):                                    # kaca lentera
        for x in range(5, 11):
            core = 6 <= x <= 9 and 5 <= y <= 8
            px[x, y] = glow_core if core and 7 <= x <= 8 else glow if core else glow_d
    for y in range(4, 10):                                    # rangka
        px[5, y] = iron
        px[10, y] = iron_d
    for x in range(4, 12):                                    # atap lentera
        px[x, 3] = iron
    for x in range(5, 11):
        px[x, 2] = iron_l if x < 8 else iron
    px[7, 1] = iron
    px[8, 1] = iron
    outline(img, INK)
    out = new(16, 32)
    shadow(out, 3, 12, 29, 31, 60)
    out.alpha_composite(img)
    return out


def make_marker(symbol):
    """Tanda di atas kepala warga, 10x14: '!' kuning = quest baru, '?' biru = quest bisa diselesaikan."""
    if symbol == "!":
        rows = [
            "...LLLL...",
            "...LMMM...",
            "...LMMM...",
            "...LMMD...",
            "...LMMD...",
            "...MMMD...",
            "....MD....",
            "....MD....",
            "..........",
            "...LMMD...",
            "...MMDD...",
        ]
        pal = {"L": "#fff6b0", "M": "#ffd23f", "D": "#e0921a"}
    else:
        rows = [
            "..LLLLL...",
            ".LMMMMMD..",
            ".MMD..MMD.",
            "......MMD.",
            ".....MMD..",
            "....MMD...",
            "....MD....",
            "..........",
            "....LMD...",
            "....MDD...",
        ]
        pal = {"L": "#d8f4ff", "M": "#5cc8ff", "D": "#2a86c8"}
    img = new(10, 14)
    grid(rows, pal, img, oy=1)
    outline(img, INK)
    return img


def make_heart(full):
    """Hati pertemanan 9x8 (termasuk garis tepi)."""
    rows = [
        ".LM.MM.",
        "LMMMMMD",
        "MMMMMMD",
        ".MMMMD.",
        "..MMD..",
        "...D...",
    ]
    pal = {"L": "#ffc4cc", "M": "#e8405a", "D": "#a82640"} if full else           {"L": "#c9b9a6", "M": "#9c8b78", "D": "#76685a"}
    img = new(9, 8)
    grid(rows, pal, img, ox=1, oy=1)
    outline(img, INK)
    return img


# =========================================================================== MVP 5: SKALA BARU & DUNIA LEBIH HIDUP
# Pemain = 16x32. Supaya masuk akal, rumah sekarang ~3,5x tinggi pemain, pintu sedikit lebih tinggi dari pemain,
# pohon ~2x tinggi pemain.

def shade(c, k):
    """k > 0 = lebih terang, k < 0 = lebih gelap. c = tuple RGBA."""
    r, g, b, a = c
    if k >= 0:
        return (int(r + (255 - r) * k), int(g + (255 - g) * k), int(b + (255 - b) * k), a)
    k = -k
    return (int(r * (1 - k)), int(g * (1 - k)), int(b * (1 - k)), a)


def rect(px, x0, y0, x1, y1, c):
    """Kotak penuh, x1/y1 ikut (inklusif)."""
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            px[x, y] = c


def hash01(*v):
    """Angka acak 0..1 yang selalu sama untuk input yang sama (pola tidak berubah tiap generate)."""
    h = 2166136261
    for n in v:
        h = ((h ^ (n & 0xffffffff)) * 16777619) & 0xffffffff
    return (h % 10007) / 10007


WALL_STYLES = {
    "plank": [C("#8f6c44"), C("#b08a5a"), C("#cba673"), C("#e3c48f")],
    "plank_dark": [C("#5e3d24"), C("#7a5232"), C("#94663e"), C("#ad7d4e")],
    "plaster": [C("#bfa983"), C("#dccaa2"), C("#ecdfbe"), C("#faf1da")],
    "brick": [C("#6e3426"), C("#8d4434"), C("#a9573f"), C("#c47458")],
}
TIMBER = [C("#3a2314"), C("#563620"), C("#76502f"), C("#93683f")]
STONE = [C("#5f5c58"), C("#7d7a74"), C("#9a968e"), C("#b7b2a8"), C("#d2cdc2")]
GLASS = [C("#335f8c"), C("#4f86b8"), C("#78aedb"), C("#b9dcf2")]


def draw_window(px, cx, wy, ww, wh, shutter=None, flowers=True, rng=None):
    """Jendela berbingkai krem, kaca bergradasi + kilau, kusen silang, daun jendela, dan kotak bunga."""
    x0, x1 = cx - ww // 2, cx + ww // 2 - 1
    y1 = wy + wh - 1
    frame, frame_d = C("#f3ead8"), C("#cdbf9f")
    if shutter:
        for y in range(wy, y1 + 1):
            for x in list(range(x0 - 4, x0)) + list(range(x1 + 1, x1 + 5)):
                px[x, y] = shutter[1] if (y - wy) % 3 == 2 else shutter[2]
            px[x0 - 4, y] = shutter[3]
            px[x1 + 4, y] = shutter[0]
    rect(px, x0, wy, x1, y1, frame)
    for x in range(x0, x1 + 1):
        px[x, y1] = frame_d
    gx0, gx1, gy0, gy1 = x0 + 2, x1 - 2, wy + 2, y1 - 2
    midx, midy = (gx0 + gx1) // 2, (gy0 + gy1) // 2
    for y in range(gy0, gy1 + 1):
        t = (y - gy0) / max(1, gy1 - gy0)
        for x in range(gx0, gx1 + 1):
            if x in (midx, midx + 1) or y == midy:
                continue
            px[x, y] = GLASS[2] if t < 0.3 else GLASS[1] if t < 0.75 else GLASS[0]
    for i in range(min(4, gy1 - gy0)):                     # kilau diagonal
        if gx0 + 1 + i <= gx1:
            px[gx0 + 1 + i, gy0 + 3 - i if gy0 + 3 - i >= gy0 else gy0] = GLASS[3]
    for y in range(wy - 1, wy + 1):                         # ambang atas
        for x in range(x0 - 1, x1 + 2):
            px[x, y] = TIMBER[2] if y == wy - 1 else frame_d
    if flowers:
        for x in range(x0 - 1, x1 + 2):                     # kotak bunga
            px[x, y1 + 1] = C("#8a5a34")
            px[x, y1 + 2] = C("#6e4528")
            px[x, y1 + 3] = C("#55341e")
        colors = [C("#e2534a"), C("#f2c94c"), C("#f08fb0"), C("#ffffff")]
        for i, x in enumerate(range(x0, x1 + 1)):
            px[x, y1] = C("#4f8c3a") if i % 2 else C("#6aa84a")
            if i % 3 == 1:
                c = colors[(i // 3 + cx) % len(colors)]
                px[x, y1 - 1] = c
                px[x, y1] = shade(c, -0.25)


def make_house_v2(w_tiles, roof_px, roof, wall="plank", door_x=None, chimney=True, gable=True,
                  seed=1, window_w=16, window_h=16, shutters=True, open_door=False):
    """
    Rumah besar. Lebar = w_tiles tile, alas (tile 'B') selalu 4 tile, atap menjulang roof_px di atasnya.
    Mengembalikan (gambar, info) — info berisi posisi jendela & cerobong untuk kode C#.
    """
    W, H = w_tiles * 16, 64 + roof_px
    img = new(W, H)
    px = img.load()
    rng = random.Random(seed)
    walls = WALL_STYLES[wall]
    eave = roof_px + 18                    # garis bawah atap
    wall_top, found_top = eave + 2, H - 5
    if door_x is None:
        door_x = (w_tiles // 2) * 16
    door_top = H - 28
    info = {"size": (W, H), "windows": [], "chimney": None}

    # 1. cerobong (digambar dulu, atap menimpa bagian bawahnya)
    if chimney:
        cx = W - 30
        for y in range(2, 28):
            for x in range(cx, cx + 10):
                mortar = (y % 4 == 3) or ((x - cx + (y // 4) * 3) % 5 == 0)
                c = C("#c9a58a") if mortar else C("#9c5b45")
                if x >= cx + 8:
                    c = shade(c, -0.3)
                elif x <= cx + 1:
                    c = shade(c, 0.12)
                px[x, y] = c
        rect(px, cx - 1, 0, cx + 10, 2, C("#5e3528"))
        rect(px, cx + 2, 0, cx + 7, 0, C("#1a100c"))
        for x in range(cx - 1, cx + 11):
            px[x, 2] = C("#3a2018")
        info["chimney"] = (cx + 5, 0)

    # 2. atap utama: trapesium genteng
    top = 6
    for y in range(top, eave + 1):
        t = (y - top) / (eave - top)
        x0, x1 = round(12 - 12 * t), round(W - 13 + 12 * t)
        row, ly = (y - top) // 5, (y - top) % 5
        for x in range(max(0, x0), min(W - 1, x1) + 1):
            sx = (x + (row % 2) * 4) % 8
            if ly == 4:
                c = roof[0] if sx in (0, 7) else roof[1]
            elif sx == 0:
                c = roof[1]
            elif ly == 0:
                c = roof[3]
            else:
                c = roof[2]
            r = hash01((x + (row % 2) * 4) // 8, row, seed)
            if ly < 4 and sx != 0 and r < 0.12:
                c = shade(c, -0.12) if r < 0.06 else shade(c, 0.1)
            xr = (x - x0) / max(1, x1 - x0)
            if x <= x0 + 1:
                c = shade(c, 0.18)
            elif xr > 0.92:
                c = shade(c, -0.18)
            px[x, y] = c
    for _ in range(W // 5):                                   # lumut di genteng bawah
        x, y = rng.randrange(6, W - 6), rng.randrange(eave - 12, eave - 1)
        if px[x, y][3]:
            px[x, y] = C("#6f8a3a") if rng.random() < 0.6 else C("#93ad52")
    for x in range(12, W - 12):                               # bubungan
        px[x, top] = roof[4]
        px[x, top + 1] = roof[3] if x % 4 else roof[4]
        px[x, top + 2] = roof[1]

    # 3. gable depan (segitiga di atas pintu) — rumah terlihat punya "wajah"
    if gable:
        gcx = door_x + 8
        gw = min(24, W // 4)
        apex = top + 12
        for y in range(apex, eave + 1):
            half = round(gw * (y - apex) / (eave - apex))
            for x in range(gcx - half - 2, gcx + half + 3):
                edge = half - abs(x - gcx)
                if edge < -2 or x < 0 or x >= W:
                    continue
                if edge <= 0:
                    c = TIMBER[3] if edge == 0 else TIMBER[1] if edge == -1 else TIMBER[0]
                else:
                    c = walls[2] if (x - gcx) % 5 else walls[1]
                    if x < gcx:
                        c = shade(c, 0.06)
                    if y < apex + 4:
                        c = shade(c, -0.25)
                px[x, y] = c
        wy = apex + (eave - apex) * 6 // 10
        for y in range(wy - 5, wy + 6):                       # jendela bulat
            for x in range(gcx - 5, gcx + 6):
                d = math.hypot(x - gcx, y - wy)
                if d <= 5.2:
                    px[x, y] = C("#f3ead8") if d > 3.6 else (GLASS[2] if y < wy else GLASS[1])
        px[gcx - 1, wy - 2] = GLASS[3]
        px[gcx, wy] = C("#f3ead8")
        info["windows"].append((gcx, wy))

    # 4. lis atap + dinding
    for x in range(W):
        px[x, eave] = C("#5a3a22")
        px[x, eave + 1] = C("#3e2615")
    for y in range(wall_top, found_top):
        ly = y - wall_top
        for x in range(1, W - 1):
            if wall in ("plank", "plank_dark"):
                c = walls[2]
                if ly % 4 == 3:
                    c = walls[1]
                elif ly % 4 == 0:
                    c = walls[3]
                if (x + (ly // 4) * 7) % 23 == 0 and ly % 4 != 3:
                    c = walls[1]
            elif wall == "brick":
                row = ly // 4
                bx = (x + (row % 2) * 4) % 8
                if ly % 4 == 3 or bx == 0:
                    c = C("#d6c3a2")
                else:
                    r = hash01((x + (row % 2) * 4) // 8, row, 3)
                    c = walls[1] if r < 0.25 else walls[3] if r > 0.85 else walls[2]
                    if ly % 4 == 0:
                        c = shade(c, 0.12)
            else:
                r = hash01(x, y, 9)
                c = walls[1] if r < 0.08 else walls[3] if r > 0.9 else walls[2]
            px[x, y] = c
    if wall == "plaster":                                     # rangka kayu
        mid = wall_top + (found_top - wall_top) // 2 - 6
        for x in range(1, W - 1):
            px[x, mid] = TIMBER[2]
            px[x, mid + 1] = TIMBER[1]
    for y in range(wall_top, found_top):                     # tiang sudut
        for x, c in ((1, TIMBER[3]), (2, TIMBER[2]), (3, TIMBER[1]), (W - 4, TIMBER[2]), (W - 3, TIMBER[1]), (W - 2, TIMBER[0])):
            px[x, y] = c
    for i, k in enumerate((-0.4, -0.28, -0.16, -0.07)):     # bayangan di bawah atap
        for x in range(1, W - 1):
            px[x, wall_top + i] = shade(px[x, wall_top + i], k)

    # 5. jendela
    wy = wall_top + 7
    centers = [door_x // 2 + 1, (door_x + 16 + W) // 2]
    shut = [shade(roof[2], -0.2), roof[1], roof[2], roof[3]] if shutters else None
    for cx in centers:
        draw_window(px, cx, wy, window_w, window_h, shut, rng=rng)
        info["windows"].append((cx, wy + window_h // 2))

    # 6. kusen pintu + kanopi kecil (daun pintunya objek terpisah di C#)
    rect(px, door_x - 3, door_top - 4, door_x + 18, H - 1, TIMBER[1])
    for y in range(door_top - 4, H):
        px[door_x - 3, y] = TIMBER[2]
        px[door_x + 18, y] = TIMBER[0]
    rect(px, door_x - 3, door_top - 4, door_x + 18, door_top - 3, TIMBER[2])
    if open_door:                                             # toko: pintu terbuka, dalamnya hangat
        for y in range(door_top - 2, H):
            for x in range(door_x - 1, door_x + 17):
                t = (y - door_top) / 28
                px[x, y] = C("#6a4224") if t > 0.7 else C("#4a2c18")
        rect(px, door_x + 1, door_top + 3, door_x + 14, door_top + 4, C("#8a5a34"))   # rak
        for i, x in enumerate(range(door_x + 1, door_x + 15, 3)):
            px[x, door_top + 2] = [C("#e2534a"), C("#f2c94c"), C("#6aa84a"), C("#e8892e"), C("#8a6ac8")][i % 5]
        rect(px, door_x - 1, door_top - 2, door_x + 1, H - 1, C("#7a5232"))          # daun pintu terbuka
    else:
        rect(px, door_x - 1, door_top - 2, door_x + 16, H - 1, C("#22140b"))
    for x in range(door_x - 6, door_x + 22):                 # kanopi pintu
        for y in range(door_top - 9, door_top - 4):
            if 0 <= x < W:
                px[x, y] = roof[3] if y == door_top - 9 else roof[0] if y == door_top - 5 else roof[2]
    for y in range(door_top - 5, door_top + 2):              # penyangga kanopi
        px[door_x - 5, y] = TIMBER[1]
        px[door_x + 20, y] = TIMBER[0]

    # 7. pondasi batu
    for y in range(found_top, H):
        for x in range(0, W):
            if door_x - 3 <= x <= door_x + 18:
                continue
            row = (y - found_top) // 3
            seam = (x + row * 5) % 9 == 0 or (y - found_top) % 3 == 2
            c = STONE[1] if seam else STONE[3] if (y - found_top) % 3 == 0 else STONE[2]
            if hash01(x // 9, row, 5) < 0.3 and not seam:
                c = shade(c, -0.1)
            px[x, y] = c

    outline(img, INK)
    return img, info


def make_shop_v2():
    """Toko bibit 128x108 (8 tile lebar). Etalase lebar + tenda bergaris + papan nama bergambar tunas."""
    img, info = make_house_v2(8, 44, SHOP_ROOF, wall="plank_dark", door_x=56, chimney=False, gable=False,
                              seed=8, window_w=34, window_h=18, shutters=False, open_door=True)
    px = img.load()
    W, H = img.size
    eave = 44 + 18
    # tenda bergaris di atas tiap etalase
    for cx in (info["windows"][0][0], info["windows"][1][0]):
        x0, x1 = cx - 21, cx + 20
        for y in range(eave + 3, eave + 10):
            for x in range(x0, x1 + 1):
                stripe = ((x - x0) // 4) % 2
                c = AWNING[0] if stripe == 0 else AWNING[2]
                if y == eave + 9:
                    c = AWNING[1] if stripe == 0 else AWNING[3]
                if y == eave + 3:
                    c = shade(c, 0.15)
                px[x, y] = c
        for x in range(x0, x1 + 1, 4):                        # rumbai
            px[x + 1, eave + 10] = AWNING[1]
            px[x + 2, eave + 10] = AWNING[1]
    # isi etalase: peti sayur
    veg = [C("#f1e0b0"), C("#e0453a"), C("#e8892e"), C("#6a3a8a"), C("#f2d14b"), C("#5c9c45")]
    for wx, wyc in info["windows"]:
        for i in range(4):
            bx = wx - 14 + i * 7
            by = wyc + 3
            rect(px, bx, by, bx + 5, by + 2, C("#8a5a34"))
            for j in range(3):
                px[bx + 1 + j * 2, by - 1] = veg[(i + j + wx) % len(veg)]
                px[bx + j * 2, by - 1] = shade(veg[(i + j + wx) % len(veg)], -0.2)
    # papan nama di atap
    bx0, bx1, by0, by1 = W // 2 - 22, W // 2 + 21, eave - 20, eave - 6
    rect(px, bx0, by0, bx1, by1, C("#e9d6a8"))
    for x in range(bx0, bx1 + 1):
        px[x, by0] = TIMBER[3]
        px[x, by1] = TIMBER[1]
    for y in range(by0, by1 + 1):
        px[bx0, y] = TIMBER[2]
        px[bx1, y] = TIMBER[0]
    cx, cy = W // 2, (by0 + by1) // 2 + 3                     # gambar tunas + 2 kantong bibit
    for y in range(cy - 4, cy + 2):
        px[cx, y] = C("#3f7a37")
    for dx, dy in ((-1, -4), (-2, -5), (-3, -5), (1, -3), (2, -4), (3, -4), (-2, -4), (2, -3)):
        px[cx + dx, cy + dy] = C("#6aa84a")
    rect(px, cx - 4, cy + 1, cx + 4, cy + 3, C("#8a5a34"))
    for sx in (cx - 15, cx + 9):
        rect(px, sx, cy - 4, sx + 5, cy + 3, C("#d9b46a"))
        rect(px, sx + 1, cy - 2, sx + 4, cy + 1, C("#e2534a") if sx < cx else C("#5c9c45"))
    for x in (bx0 + 4, bx1 - 4):                              # rantai papan
        for y in range(by1 + 1, by1 + 4):
            px[x, y] = C("#3a3a40")
    outline(img, INK)
    return img, info


def make_door_tall(open_):
    """Pintu 16x28: sedikit lebih tinggi dari pemain (32 termasuk topi)."""
    img = new(16, 28)
    px = img.load()
    if open_:
        for y in range(28):
            for x in range(16):
                px[x, y] = C("#2a180c") if y < 20 else C("#4a2c16")
        for y in range(20, 28):                               # cahaya dari dalam
            for x in range(3, 16):
                px[x, y] = C("#8a5a2e") if (x + y) % 5 else C("#a06a36")
        rect(px, 0, 0, 2, 27, C("#7a4a2a"))                   # daun pintu terbuka
        for y in range(28):
            px[3, y] = C("#4e2e18")
        return img
    plank = [C("#7e4e2e"), C("#93603a"), C("#a87046")]
    for y in range(28):
        for x in range(16):
            c = plank[(x // 4) % 2 + (1 if x % 4 == 1 else 0)]
            if x % 4 == 3:
                c = C("#5a361e")
            px[x, y] = c
    for y in (8, 20):                                         # palang
        rect(px, 0, y, 15, y + 1, C("#5e381e"))
        for x in range(16):
            px[x, y - 1] = C("#b07c4e")
    for y in range(2, 7):                                     # jendela kecil melengkung
        for x in range(4, 12):
            if not (y == 2 and x in (4, 11)):
                px[x, y] = GLASS[2] if y < 4 else GLASS[1]
    px[5, 3] = GLASS[3]
    rect(px, 12, 14, 13, 15, C("#f0c75a"))                     # gagang
    px[13, 15] = C("#8a6520")
    return img


# ---- pohon besar ----

PINE = [C(c) for c in ("#17332e", "#1f4a3c", "#2b6048", "#3b7a52", "#5e9e64")]


def make_oaks():
    """Pohon rindang 48x64 (3 tile lebar, 4 tile tinggi). Batang tepat di tengah tile bawah."""
    rng = random.Random(31)
    canopy = new(48, 64)
    clumps = []
    for cy, xs, r in ((11, [18, 25, 32], 8.0), (19, [10, 18, 27, 37], 8.6), (28, [9, 19, 29, 39], 9.0),
                      (36, [15, 25, 34], 8.4)):
        for cx in xs:
            clumps.append((cx + rng.uniform(-1, 1), cy + rng.uniform(-1, 1), r + rng.uniform(-0.6, 0.6)))
    clump_foliage(canopy, clumps)
    cp = canopy.load()
    for _ in range(30):
        x, y = rng.randrange(5, 43), rng.randrange(4, 40)
        if cp[x, y][3] and cp[x, y] in (LEAF[3], LEAF[2]):
            cp[x, y] = LEAF[4]
    trunk = new(48, 64)
    tp = trunk.load()
    bark = [C("#46301c"), C("#634429"), C("#82593a"), C("#a07650")]
    for y in range(36, 62):
        w = 4 if y < 54 else 5 + (y - 54) // 3
        for x in range(24 - w, 24 + w):
            c = bark[1]
            if x <= 24 - w + 1:
                c = bark[2]
            if x >= 24 + w - 2:
                c = bark[0]
            if (x * 3 + y * 5) % 11 == 0:
                c = bark[0]
            tp[x, y] = c
    for y in range(44, 52):
        tp[22, y] = bark[3]
    frames = {}
    for i, amt in enumerate((0, 2.0, -2.0)):
        tree = new(48, 64)
        tree.alpha_composite(trunk)
        tree.alpha_composite(sway_rows(canopy, amt, 46))
        outline(tree, "#1c2a19")
        out = new(48, 64)
        shadow(out, 8, 40, 56, 63, 75)
        out.alpha_composite(tree)
        frames[f"tree_{i}"] = out
    return frames


def make_pines():
    """Pohon cemara 32x64: tingkat-tingkat daun runcing."""
    canopy = new(32, 64)
    cp = canopy.load()
    tiers = [(4, 18, 7), (12, 28, 10), (21, 38, 13), (30, 50, 15)]   # (puncak, dasar, setengah-lebar dasar)
    for apex, base, half in tiers:
        for y in range(apex, base + 1):
            t = (y - apex) / (base - apex)
            hw = half * t
            for x in range(32):
                dx = x - 16
                jag = (x % 3 == 0) and y >= base - 1
                if abs(dx + 0.5) <= hw and not jag:
                    light = (-dx / max(1, hw)) * 0.6 + (1 - t) * 0.5
                    c = PINE[4] if light > 0.75 else PINE[3] if light > 0.3 else PINE[2] if light > -0.25 else PINE[1]
                    if y >= base - 1:
                        c = PINE[0]
                    cp[x, y] = c
    trunk = new(32, 64)
    tp = trunk.load()
    for y in range(48, 62):
        for x in range(14, 18):
            tp[x, y] = C("#7a5232") if x == 14 else C("#4e3420") if x == 17 else C("#634429")
    frames = {}
    for i, amt in enumerate((0, 1.5, -1.5)):
        tree = new(32, 64)
        tree.alpha_composite(trunk)
        tree.alpha_composite(sway_rows(canopy, amt, 50))
        outline(tree, "#10221c")
        out = new(32, 64)
        shadow(out, 5, 26, 57, 63, 75)
        out.alpha_composite(tree)
        frames[f"pine_{i}"] = out
    return frames


# ---- tile baru: batu alun-alun & jembatan kayu ----

COBBLE = [C(c) for c in ("#5c5650", "#7f776c", "#968d80", "#ada395", "#c7bfb0")]


def cobble_tile(seed):
    """Batu bulat-bulat tersusun. Nat (garis gelap) di tepi atas/kiri, jadi tile menyambung mulus."""
    rng = random.Random(seed)
    img = new(16, 16)
    px = img.load()
    rect(px, 0, 0, 15, 15, COBBLE[0])
    for row in range(4):
        y0 = row * 4
        cuts = sorted(rng.sample(range(2, 15), 2))
        starts = [0] + cuts
        ends = cuts + [16]
        off = rng.randrange(0, 4)
        for s, e in zip(starts, ends):
            base = rng.choice([COBBLE[2], COBBLE[2], COBBLE[3], COBBLE[1]])
            for y in range(y0 + 1, y0 + 4):
                for x in range(s + 1, e):
                    xx = (x + off) % 16
                    c = base
                    if y == y0 + 1:
                        c = shade(base, 0.18)
                    elif y == y0 + 3:
                        c = shade(base, -0.15)
                    if x == s + 1 and y != y0 + 3:
                        c = shade(base, 0.08)
                    px[xx, y] = c
    for _ in range(3):
        px[rng.randrange(16), rng.randrange(16)] = COBBLE[4]
    return img


PLANK = [C(c) for c in ("#4a2e1a", "#6e4528", "#8a5a34", "#a6703f", "#c08a55")]


def bridge_tile(horizontal):
    """Lantai papan. horizontal=True: jalan ke kiri-kanan (papan melintang atas-bawah)."""
    img = new(16, 16)
    px = img.load()
    for y in range(16):
        for x in range(16):
            u, v = (x, y) if horizontal else (y, x)
            board = u // 4
            c = PLANK[2] if board % 2 else PLANK[3]
            if u % 4 == 3:
                c = PLANK[0]
            elif u % 4 == 0:
                c = shade(c, 0.1)
            if (v + board * 5) % 13 == 0 and u % 4 == 1:
                c = PLANK[1]                                   # mata kayu
            px[x, y] = c
    for u in range(16):                                       # paku
        for v in (2, 13):
            if u % 4 == 1:
                px[(u, v) if horizontal else (v, u)] = C("#3a3a40")
    return img


def bridge_edge(side):
    """Balok tepi + tiang, digambar di sisi yang bertetangga air."""
    img = new(16, 16)
    px = img.load()
    for i in range(16):
        for d in range(3):
            c = PLANK[0] if d == 2 else PLANK[1] if d == 1 else PLANK[4]
            if side == "n":
                px[i, d] = c
            elif side == "s":
                px[i, 15 - d] = PLANK[0] if d == 0 else PLANK[1] if d == 1 else PLANK[3]
            elif side == "w":
                px[d, i] = c
            else:
                px[15 - d, i] = PLANK[0] if d == 0 else PLANK[1]
    for p in (1, 13):                                         # tiang
        for d in range(4):
            if side in ("n", "s"):
                y = d if side == "n" else 15 - d
                px[p, y] = PLANK[1]
                px[p + 1, y] = PLANK[0]
            else:
                x = d if side == "w" else 15 - d
                px[x, p] = PLANK[1]
                px[x, p + 1] = PLANK[0]
    return img


# ---- objek interaktif & hiasan ----

def make_mailbox(flag_up):
    img = new(16, 28)
    px = img.load()
    shadow(img, 3, 12, 24, 27, 60)
    rect(px, 7, 13, 8, 26, TIMBER[2])
    for y in range(13, 27):
        px[8, y] = TIMBER[1]
    body, body_l, body_d = C("#3f7d8c"), C("#62a3b0"), C("#2b5966")
    for y in range(4, 14):
        for x in range(2, 14):
            if y == 4 and x in (2, 13):
                continue
            c = body
            if y <= 5 or x == 2:
                c = body_l
            if x == 13 or y == 13:
                c = body_d
            px[x, y] = c
    rect(px, 3, 7, 4, 11, C("#22414a"))                       # pintu kotak
    if flag_up:
        rect(px, 14, 2, 14, 9, C("#3a3a40"))
        rect(px, 12, 2, 13, 5, C("#e2453a"))
        px[12, 2] = C("#ff8070")
    else:
        rect(px, 14, 8, 14, 9, C("#3a3a40"))
        rect(px, 10, 9, 14, 10, C("#a8302a"))
    outline(img, INK)
    return img


def make_well():
    """Sumur 32x40: alas 2x1 tile, atap kecil di atas."""
    img = new(32, 40)
    px = img.load()
    shadow(img, 1, 30, 33, 39, 70)
    for y in range(22, 37):                                   # dinding batu bulat
        for x in range(2, 30):
            row = (y - 22) // 3
            seam = (x + row * 4) % 7 == 0 or (y - 22) % 3 == 2
            c = STONE[1] if seam else STONE[3] if (y - 22) % 3 == 0 else STONE[2]
            if x <= 4:
                c = shade(c, 0.1)
            if x >= 27:
                c = shade(c, -0.2)
            px[x, y] = c
    for y in range(18, 25):                                   # bibir sumur (elips) + air
        for x in range(1, 31):
            d = ((x - 15.5) / 15) ** 2 + ((y - 21.5) / 3.6) ** 2
            if d <= 1:
                inner = ((x - 15.5) / 12) ** 2 + ((y - 21.5) / 2.2) ** 2 <= 1
                px[x, y] = (C("#1e3c5c") if y > 21 else C("#28507a")) if inner else STONE[4] if y < 21 else STONE[2]
    for x in (4, 27):                                         # tiang
        for y in range(4, 23):
            px[x, y] = TIMBER[2]
            px[x + 1, y] = TIMBER[1]
    for x in range(5, 27):                                    # poros + tali + ember
        px[x, 9] = TIMBER[1]
    for y in range(10, 16):
        px[16, y] = C("#c9b48a")
    rect(px, 14, 15, 18, 18, C("#7a7f86"))
    rect(px, 14, 15, 18, 15, C("#a8aeb5"))
    for y in range(0, 7):                                     # atap kecil
        half = 6 + y * 2
        for x in range(16 - half, 16 + half):
            if 0 <= x < 32:
                c = SHOP_ROOF[2] if (x + y) % 4 else SHOP_ROOF[1]
                if y == 0:
                    c = SHOP_ROOF[4]
                if y == 6:
                    c = SHOP_ROOF[0]
                px[x, y] = c
    outline(img, INK)
    return img


def make_fountain(frame):
    """Air mancur 48x48: kolam batu segi-delapan + pilar + semburan yang bergerak (3 frame)."""
    img = new(48, 48)
    px = img.load()
    shadow(img, 1, 46, 40, 47, 70)
    for y in range(22, 44):                                   # kolam
        for x in range(1, 47):
            d = ((x - 23.5) / 23) ** 2 + ((y - 31) / 12) ** 2
            if d > 1:
                continue
            inner = ((x - 23.5) / 19) ** 2 + ((y - 30) / 8) ** 2 <= 1
            if inner:
                ripple = math.sin((x * 0.7 + y * 1.3) + frame * 2.1)
                c = WATER[2] if ripple > 0.3 else WATER[1]
                if ripple > 0.9:
                    c = WATER[4]
            else:
                c = STONE[3] if y < 30 else STONE[2] if y < 38 else STONE[1]
                if (x + y) % 9 == 0:
                    c = STONE[1]
            px[x, y] = c
    for y in range(12, 32):                                   # pilar
        for x in range(21, 27):
            px[x, y] = STONE[4] if x == 21 else STONE[1] if x == 26 else STONE[3]
    for y in range(10, 15):                                   # mangkuk atas
        for x in range(15, 33):
            if ((x - 23.5) / 9) ** 2 + ((y - 12) / 2.6) ** 2 <= 1:
                px[x, y] = STONE[4] if y < 12 else STONE[2]
    rng = random.Random(frame)
    for i in range(14):                                       # semburan
        a = -math.pi / 2 + (i - 6.5) * 0.18
        for s in range(0, 10):
            t = s / 9
            x = 24 + math.cos(a) * 12 * t
            y = 10 - 8 * t + 16 * t * t + (frame % 3) * 0.6
            if 0 <= int(x) < 48 and 0 <= int(y) < 48 and rng.random() < 0.8:
                px[int(x), int(y)] = WATER[4] if rng.random() < 0.5 else WATER[3]
    for _ in range(10):                                       # percikan di kolam
        x, y = rng.randrange(8, 40), rng.randrange(25, 36)
        if px[x, y] in (WATER[1], WATER[2]):
            px[x, y] = WATER[4]
    outline(img, INK)
    return img


def make_bench():
    img = new(32, 22)
    px = img.load()
    shadow(img, 1, 30, 17, 21, 60)
    iron = C("#34343c")
    for x in (3, 4, 27, 28):
        for y in range(4, 20):
            px[x, y] = iron
    for y in (4, 7):                                          # sandaran
        rect(px, 2, y, 29, y + 1, PLANK[3] if y == 4 else PLANK[2])
    for y in (12, 14):                                        # dudukan
        rect(px, 1, y, 30, y + 1, PLANK[4] if y == 12 else PLANK[3])
    rect(px, 1, 16, 30, 16, PLANK[1])
    outline(img, INK)
    return img


def make_notice_board():
    """Papan pengumuman 32x32 (alas 2x1)."""
    img = new(32, 32)
    px = img.load()
    shadow(img, 2, 29, 27, 31, 65)
    for x in (4, 26):
        for y in range(6, 30):
            px[x, y] = TIMBER[2]
            px[x + 1, y] = TIMBER[1]
    rect(px, 2, 8, 29, 23, C("#a2784a"))                      # papan gabus
    rect(px, 3, 9, 28, 22, C("#c49a64"))
    for x in range(1, 31):                                    # atap kecil
        px[x, 5] = SHOP_ROOF[3]
        px[x, 6] = SHOP_ROOF[2]
        px[x, 7] = SHOP_ROOF[0]
    papers = [(5, 10, 9, 17, "#f6f0e0"), (12, 11, 18, 18, "#f2e3b0"), (20, 10, 26, 16, "#e6f0f6"), (19, 17, 24, 21, "#f6dada")]
    for x0, y0, x1, y1, col in papers:
        rect(px, x0, y0, x1, y1, C(col))
        for y in range(y0 + 2, y1, 2):
            for x in range(x0 + 1, x1):
                if (x + y) % 5:
                    px[x, y] = C("#8a8478")
        px[(x0 + x1) // 2, y0] = C("#e2453a")                 # paku payung
    outline(img, INK)
    return img


def make_crate():
    img = new(16, 18)
    px = img.load()
    shadow(img, 1, 14, 14, 17, 60)
    rect(px, 1, 2, 14, 15, PLANK[3])
    for y in range(2, 16):
        for x in range(1, 15):
            if (y - 2) % 4 == 3:
                px[x, y] = PLANK[1]
    rect(px, 1, 2, 14, 2, PLANK[4])
    for i in range(12):                                       # silang
        px[2 + i, 3 + i] = PLANK[1]
        px[13 - i, 3 + i] = PLANK[1]
    for y in range(2, 16):
        px[1, y] = PLANK[4]
        px[14, y] = PLANK[0]
    outline(img, INK)
    return img


def make_barrel():
    img = new(16, 20)
    px = img.load()
    shadow(img, 1, 14, 16, 19, 60)
    for y in range(2, 18):
        bulge = 1 if 5 <= y <= 14 else 0
        for x in range(2 - bulge, 14 + bulge):
            c = PLANK[3] if (x // 3) % 2 else PLANK[2]
            if x <= 3:
                c = PLANK[4]
            if x >= 12:
                c = PLANK[1]
            px[x, y] = c
    for y in (4, 15):                                         # simpai besi
        for x in range(1, 15):
            if px[x, y][3]:
                px[x, y] = C("#4a4a52")
    for y in range(1, 4):                                     # tutup
        for x in range(3, 13):
            px[x, y] = PLANK[4] if y == 1 else PLANK[3]
    outline(img, INK)
    return img


def make_scarecrow():
    img = new(16, 32)
    px = img.load()
    shadow(img, 3, 12, 28, 31, 60)
    for y in range(10, 30):
        px[8, y] = TIMBER[2]
    rect(px, 1, 13, 14, 14, TIMBER[2])                        # lengan
    rect(px, 4, 12, 11, 22, C("#5a7ab0"))                     # baju
    rect(px, 4, 12, 11, 12, C("#7a9ad0"))
    for x in (2, 13):
        px[x, 15] = C("#e6c86a")                              # jerami di tangan
        px[x, 16] = C("#c9a84a")
    rect(px, 5, 4, 10, 10, C("#e8d090"))                      # kepala karung
    px[6, 6] = INK_T
    px[9, 6] = INK_T
    for x in range(6, 10):
        px[x, 8] = C("#8a6a40")
    rect(px, 3, 2, 12, 3, C("#8a5a34"))                       # topi
    rect(px, 5, 0, 10, 2, C("#a06a3a"))
    outline(img, INK)
    return img


INK_T = C("#2e1f16")


def make_woodpile():
    img = new(32, 22)
    px = img.load()
    shadow(img, 1, 30, 17, 21, 60)
    for row, (y, n, off) in enumerate(((13, 5, 1), (8, 4, 4), (3, 3, 7))):
        for i in range(n):
            cx, cy = off + 3 + i * 6, y + 2
            for yy in range(cy - 3, cy + 3):
                for xx in range(cx - 3, cx + 3):
                    d = math.hypot(xx - cx + 0.5, yy - cy + 0.5)
                    if d <= 3:
                        px[xx, yy] = C("#e3c08a") if d < 1.3 else C("#c9a06a") if d < 2.3 else C("#7a5232")
    outline(img, INK)
    return img


def make_forage():
    """Barang liar yang bisa dipungut (juga dipakai sebagai ikon)."""
    out = {}
    m = new(16, 16)
    px = m.load()
    shadow(m, 3, 12, 13, 15, 50)
    for cx, cy, r, col in ((6, 8, 4, "#c8553a"), (11, 10, 3, "#d86a4a")):
        for y in range(cy - r, cy + 1):
            for x in range(cx - r, cx + r + 1):
                if ((x - cx) / r) ** 2 + ((y - cy) / r) ** 2 <= 1:
                    px[x, y] = shade(C(col), 0.2) if y < cy - r // 2 else C(col)
        px[cx - 1, cy - r + 1] = C("#fff0e0")
        px[cx + 1, cy - 1] = C("#fff0e0")
        for y in range(cy + 1, cy + 5):
            px[cx, y] = C("#efe6d0")
            px[cx - 1, y] = C("#d6ccb4")
    outline(m, INK)
    out["forage_mushroom"] = m

    b = new(16, 16)
    px = b.load()
    shadow(b, 2, 13, 13, 15, 50)
    for x, y in ((4, 6), (5, 9), (8, 5), (9, 8), (11, 6), (12, 9), (7, 11), (10, 11)):
        px[x + 1, y + 1] = C("#2f5f31")
    for cx, cy in ((5, 7), (9, 6), (7, 10), (11, 9)):
        for y in range(cy - 1, cy + 2):
            for x in range(cx - 1, cx + 2):
                px[x, y] = C("#4a2a8a")
        px[cx, cy] = C("#6a4ab8")
        px[cx - 1, cy - 1] = C("#b8a8f0")
    outline(b, INK)
    out["forage_berries"] = b

    f = new(16, 16)
    px = f.load()
    shadow(f, 3, 12, 13, 15, 50)
    for y in range(7, 15):
        px[8, y] = C("#3f7a37")
    px[7, 11] = C("#5c9c45")
    px[6, 10] = C("#5c9c45")
    for i in range(8):
        a = i * math.pi / 4
        x, y = 8 + round(math.cos(a) * 3), 5 + round(math.sin(a) * 3)
        px[x, y] = C("#f6f0a8")
        px[(x + 8) // 2, (y + 5) // 2] = C("#fff8d0")
    px[8, 5] = C("#e8892e")
    outline(f, INK)
    out["forage_wildflower"] = f
    return out


def make_birds():
    """Burung pipit 8x8: mematuk (idle 0/1) dan terbang (fly 0/1)."""
    body, belly, wing, beak = C("#8a6a48"), C("#e6d2b0"), C("#5e4630"), C("#e8a83a")
    frames = {}
    for i in range(2):
        img = new(8, 8)
        px = img.load()
        head_y = 2 if i == 0 else 4
        rect(px, 2, 4, 6, 6, body)
        rect(px, 3, 5, 5, 6, belly)
        rect(px, 4, 4, 5, 4, wing)
        rect(px, 0, 3, 1, 4, wing)                            # ekor
        rect(px, 5, head_y, 6, head_y + 1, body)
        px[7, head_y + 1] = beak
        px[6, head_y] = C("#1a1410")
        px[3, 7] = beak
        px[5, 7] = beak
        frames[f"bird_idle_{i}"] = img
        fly = new(8, 8)
        px = fly.load()
        rect(px, 2, 3, 5, 5, body)
        rect(px, 3, 4, 4, 5, belly)
        px[6, 3] = body
        px[7, 3] = beak
        if i == 0:
            for x in range(1, 6):
                px[x, 1 + abs(x - 3) // 2] = wing
        else:
            for x in range(1, 6):
                px[x, 6 - abs(x - 3) // 2] = wing
        frames[f"bird_fly_{i}"] = fly
    return frames


def make_emotes():
    """Gelembung emosi 14x14 di atas kepala warga."""
    out = {}

    def bubble():
        img = new(14, 14)
        px = img.load()
        for y in range(0, 11):
            for x in range(0, 14):
                if (x in (0, 13) and y in (0, 10)):
                    continue
                px[x, y] = C("#fffaf0")
        rect(px, 5, 11, 7, 11, C("#fffaf0"))
        px[6, 12] = C("#fffaf0")
        return img, px

    img, px = bubble()
    for x, y in ((3, 3), (4, 3), (8, 3), (9, 3), (2, 4), (3, 4), (4, 4), (5, 4), (7, 4), (8, 4), (9, 4), (10, 4)):
        px[x, y] = C("#e8405a")
    for y, (a, b) in enumerate(((2, 10), (3, 9), (4, 8), (5, 7), (6, 6)), start=5):
        for x in range(a, b + 1):
            px[x, y] = C("#e8405a")
    px[3, 4] = C("#ffc4cc")
    outline(img, INK)
    out["emote_heart"] = img

    img, px = bubble()                                        # not musik = senang
    rect(px, 8, 2, 8, 7, C("#3a6ac8"))
    rect(px, 9, 2, 10, 3, C("#3a6ac8"))
    rect(px, 6, 7, 8, 8, C("#3a6ac8"))
    rect(px, 3, 4, 3, 8, C("#3a6ac8"))
    rect(px, 2, 8, 3, 8, C("#3a6ac8"))
    outline(img, INK)
    out["emote_happy"] = img

    img, px = bubble()                                        # tetes keringat = sedih / tidak suka
    for y in range(2, 9):
        hw = 0 if y < 4 else 1 if y < 6 else 2
        for x in range(7 - hw, 8 + hw):
            px[x, y] = C("#5cb8f0")
    px[6, 6] = C("#d8f4ff")
    outline(img, INK)
    out["emote_sad"] = img

    img, px = bubble()                                        # "..."
    for x in (3, 6, 9):
        rect(px, x, 5, x + 1, 6, C("#5a4a3a"))
    outline(img, INK)
    out["emote_dots"] = img
    return out


# --------------------------------------------------------------------------- HAMA & UI MINIGAME

PEST_PAL = {"a": "#2c2c34", "b": "#4a4a58", "w": "#fff2d8", "s": "#c9a22e", "S": "#8a6a2e",
            "g": "#6ab04c", "G": "#4a8a34", "e": "#e8e8f0", "m": "#dcdcc8", "d": "#b8b8a0",
            "o": "#e8923a", "O": "#b8681e", "r": "#c83828"}


def make_pests():
    """Sprite hama 12x10 untuk minigame Tangkap Hama (juga dipakai sebagai tanda di ladang)."""
    out = {}

    aphid = [  # kutu daun: bulat kecil hijau-kuning dengan kaki
        "..b....b....",
        "...b..b.....",
        "..ssssss....",
        ".ssssssss...",
        "sSssssssss..",
        "sSsswsssss..",
        ".sssssssss..",
        "..ss.ss.ss..",
        "...b..b..b..",
    ]
    out["pest_aphid"] = outline(grid(aphid, PEST_PAL), "#1a1a20")

    snail = [  # bekicot: cangkang bulat cokelat + badan
        "....mmmm....",
        "...mddddm...",
        "..mddOOOdm..",
        ".mddOOOOOdm.",
        ".mdOOOwOOdm.",
        ".mdOOOOOOdm.",
        ".mmddddddm..",
        "GGGGGGGGGG..",
        "gGGGGGGGGge.",
        ".gg......ee.",
    ]
    out["pest_snail"] = outline(grid(snail, PEST_PAL), "#241a10")

    crow = [  # burung: kepala gelap + paruh kuning
        "........ss..",
        ".......ssss.",
        "..sssssssws.",
        ".ssssssssss.",
        "ssssssssss..",
        "sssbbbbsss..",
        ".ssbbbbss...",
        "..ssssss....",
        "....s..s....",
        "....o..o....",
    ]
    out["pest_crow"] = outline(grid(crow, {**PEST_PAL, "s": "#3a3a44", "b": "#6a6a78", "w": "#ffffff"}), "#181820")

    mold = [  # jamur: bercak putih berbulu
        "...www.ww...",
        "..wwwwwwww..",
        ".wWwwwWwwww.",
        ".wwwwwwwWww.",
        "..wwWwwwww..",
        "...wwwwww...",
        "....wwww....",
        ".....ww.....",
    ]
    out["pest_mold"] = outline(grid(mold, {**PEST_PAL, "w": "#e8e8dc", "W": "#ffffff"}), "#8a8a7a")

    return out


def make_lesson_icons():
    """Ikon kecil untuk sel minigame grid: daun (jangan diklik) dan kuncup (bonus)."""
    out = {}
    leaf = [
        "..gGGg...",
        ".gGGGGGg.",
        "gGGwGGGGg",
        "GGGwGGGGg",
        "GGGGwGGGg",
        "gGGGwGGGg",
        ".gGGwGGg.",
        "..gGGGg..",
        "....G....",
    ]
    out["lesson_leaf"] = outline(grid(leaf, PEST_PAL), "#1e3320")

    bud = [
        "....s....",
        "...sss...",
        "..ssGss..",
        ".ssGGGss.",
        ".sGGwGGs.",
        "..sGGGs..",
        "...sss...",
        "....s....",
    ]
    out["lesson_bud"] = outline(grid(bud, PEST_PAL), "#1e3320")

    return out


def make_stars():
    """Bintang untuk hasil minigame dan Buku Tani."""
    out = {}
    rows = [
        "....ss....",
        "....ss....",
        "ssssssssss",
        ".ssssssss.",
        "..ssssss..",
        "..ssssss..",
        ".ss..ss.s.",
        "ss....s...",
        "s.........",
    ]
    pal = {"s": "#f8d440", "e": "#c8a020"}
    star = grid(rows, pal)
    px = star.load()
    px[4, 3] = C("#fff4c0")
    px[5, 4] = C("#fff4c0")
    out["star_full"] = outline(star, "#7a5a10")
    out["star_empty"] = outline(grid(rows, {"s": "#c9c2b4", "e": "#a8a094"}), "#6a655c")
    return out


# --------------------------------------------------------------------------- SFX (WAV)

def write_wav(path, samples, rate=22050):
    """Tulis sampel float -1..1 sebagai WAV mono 16-bit."""
    import struct
    import wave
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        frames = bytearray()
        for s in samples:
            s = max(-1.0, min(1.0, s))
            frames += struct.pack("<h", int(s * 32000))
        w.writeframes(bytes(frames))


def synth(duration, fn, rate=22050, fade=0.02):
    """Sampel dari fungsi waktu->nilai, dengan fade in/out pendek supaya tidak 'klik'."""
    n = int(duration * rate)
    out = []
    for i in range(n):
        t = i / rate
        v = fn(t)
        # fade pendek di awal dan akhir
        if t < fade:
            v *= t / fade
        if t > duration - fade:
            v *= max(0.0, (duration - t) / fade)
        out.append(v)
    return out


def env_exp(t, k=6.0):
    """Volume turun cepat (untuk pukulan/pop)."""
    return math.exp(-k * t)


def tone(freq, dur, k=6.0, rate=22050):
    return synth(dur, lambda t: math.sin(2 * math.pi * freq * t) * env_exp(t, k), rate)


def noise(dur, k=8.0, rate=22050):
    rng = random.Random(7)
    return synth(dur, lambda t: (rng.random() * 2 - 1) * env_exp(t, k), rate)


def make_sfx(out_dir):
    """Efek suara 8-bit sederhana. Volume rendah supaya tidak mengagetkan anak-anak."""
    os.makedirs(out_dir, exist_ok=True)

    def mix(*parts):
        n = max(len(p) for p in parts)
        out = []
        for i in range(n):
            v = sum(p[i] for p in parts if i < len(p))
            out.append(max(-1.0, min(1.0, v * 0.9)))
        return out

    # dunia
    write_wav(os.path.join(out_dir, "hoe.wav"), mix(noise(0.16, 9), tone(180, 0.16, 7)), 22050)
    write_wav(os.path.join(out_dir, "water.wav"), mix(noise(0.22, 5), tone(500, 0.22, 3, 22050)), 22050)
    write_wav(os.path.join(out_dir, "plant.wav"), mix(tone(660, 0.10, 8), tone(990, 0.14, 6)), 22050)
    write_wav(os.path.join(out_dir, "harvest.wav"), mix(tone(784, 0.12, 6), tone(1046, 0.18, 5)), 22050)
    write_wav(os.path.join(out_dir, "coin.wav"), mix(tone(1318, 0.09, 6), tone(1760, 0.14, 5)), 22050)
    write_wav(os.path.join(out_dir, "wood.wav"), mix(noise(0.14, 10), tone(140, 0.14, 8)), 22050)
    write_wav(os.path.join(out_dir, "stone.wav"), mix(noise(0.12, 12), tone(110, 0.12, 9)), 22050)
    write_wav(os.path.join(out_dir, "door.wav"), mix(tone(220, 0.16, 5), tone(165, 0.2, 4)), 22050)
    write_wav(os.path.join(out_dir, "eat.wav"), mix(noise(0.1, 10), tone(330, 0.12, 7)), 22050)

    # UI
    write_wav(os.path.join(out_dir, "ui_open.wav"), mix(tone(523, 0.08, 7), tone(784, 0.10, 6)), 22050)
    write_wav(os.path.join(out_dir, "ui_close.wav"), mix(tone(392, 0.08, 7), tone(262, 0.12, 6)), 22050)
    write_wav(os.path.join(out_dir, "select.wav"), tone(880, 0.05, 10), 22050)

    # minigame
    write_wav(os.path.join(out_dir, "pop.wav"), mix(tone(1046, 0.08, 9), noise(0.05, 14)), 22050)
    write_wav(os.path.join(out_dir, "good.wav"), mix(tone(784, 0.07, 8), tone(1175, 0.12, 6)), 22050)
    write_wav(os.path.join(out_dir, "wrong.wav"), mix(tone(233, 0.16, 5), tone(196, 0.2, 4)), 22050)
    write_wav(os.path.join(out_dir, "win.wav"),
              mix(tone(523, 0.10, 4), tone(659, 0.10, 4), tone(784, 0.10, 4), tone(1046, 0.22, 3)), 22050)
    write_wav(os.path.join(out_dir, "star.wav"), mix(tone(1568, 0.10, 7), tone(2093, 0.14, 5)), 22050)
    write_wav(os.path.join(out_dir, "combo.wav"), mix(tone(988, 0.06, 8), tone(1318, 0.09, 6)), 22050)

    print("SFX:", out_dir)


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

    # karakter (petani + warga kota)
    for name, img in make_player().items():
        save(img, "Characters", name)
    for npc_id, pal in NPC_PALETTES.items():
        for name, img in make_character("npc_" + npc_id, pal).items():
            save(img, "Characters", name)

    # objek
    # MVP 5: bangunan skala baru (lihat juga posisi jendela yang dicetak untuk kode C#)
    buildings = {
        "house": make_house_v2(7, 48, ROOF, "plank", seed=1),
        "house_blue": make_house_v2(7, 44, ROOF_BLUE, "plaster", seed=2),
        "house_red": make_house_v2(5, 40, ROOF_RED, "brick", seed=3, gable=False),
        "shop": make_shop_v2(),
    }
    for name, (img, info) in buildings.items():
        save(img, "Objects", name)
        print(f"  {name}: {info}")
    save(make_counter(), "Objects", "counter")
    save(make_sign(), "Objects", "sign")
    save(make_door(), "Objects", "door")
    save(make_bin(), "Objects", "bin")
    for mask in range(16):
        save(make_hoedirt(False, mask), "Objects", f"hoedirt_dry_{mask}")
        save(make_hoedirt(True, mask), "Objects", f"hoedirt_wet_{mask}")

    # alam
    for name, img in {**make_oaks(), **make_pines(), **make_tufts(), **make_flowers(), **make_butterflies()}.items():
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

    # MVP 3: dalam rumah
    for i in range(4):
        save(floor_tile(500 + i), "Tiles", f"floor_{i}")
    save(wall_front(), "Tiles", "wall_front")
    save(wall_top(), "Tiles", "wall_top")
    save(make_window(), "Objects", "window")
    save(make_fireplace(0), "Objects", "fireplace_0")
    save(make_fireplace(1), "Objects", "fireplace_1")
    save(make_bed(), "Objects", "bed")
    save(make_table(), "Objects", "table")
    save(make_plant(), "Objects", "plant")
    save(make_rug(), "Objects", "rug")
    save(make_doormat(), "Objects", "doormat")

    # MVP 3: tunggul, ranting, kerikil
    save(make_stump(), "Nature", "stump")
    for v in range(2):
        save(make_twig(v), "Nature", f"twig_{v}")
        save(make_pebble(v), "Nature", f"pebble_{v}")

    # MVP 3: alat baru, bahan, ikan
    save(make_axe(), "Items", "tool_axe")
    save(make_pickaxe(), "Items", "tool_pickaxe")
    save(make_rod(), "Items", "tool_fishing_rod")
    save(make_wood(), "Items", "resource_wood")
    save(make_stone_item(), "Items", "resource_stone")
    for fish_id in FISH_STYLE:
        save(make_fish(fish_id), "Items", f"fish_{fish_id}")

    # MVP 4: lampu, tanda quest, hati
    save(make_lamppost(), "Objects", "lamppost")
    save(make_marker("!"), "UI", "marker_new")
    save(make_marker("?"), "UI", "marker_turnin")
    save(make_heart(True), "UI", "heart_full")
    save(make_heart(False), "UI", "heart_empty")

    # MVP 5: tile, pintu, objek interaktif, barang liar, burung, emosi
    for i in range(4):
        save(cobble_tile(600 + i), "Tiles", f"cobble_{i}")
    save(bridge_tile(True), "Tiles", "bridge_h")
    save(bridge_tile(False), "Tiles", "bridge_v")
    for side in "nswe":
        save(bridge_edge(side), "Tiles", f"bridge_edge_{side}")
    save(make_door_tall(False), "Objects", "door_tall")
    save(make_door_tall(True), "Objects", "door_tall_open")
    save(make_mailbox(False), "Objects", "mailbox_0")
    save(make_mailbox(True), "Objects", "mailbox_1")
    save(make_well(), "Objects", "well")
    for f in range(3):
        save(make_fountain(f), "Objects", f"fountain_{f}")
    save(make_bench(), "Objects", "bench")
    save(make_notice_board(), "Objects", "noticeboard")
    save(make_crate(), "Objects", "crate")
    save(make_barrel(), "Objects", "barrel")
    save(make_scarecrow(), "Objects", "scarecrow")
    save(make_woodpile(), "Objects", "woodpile")
    for name, img in make_forage().items():
        save(img, "Items", name)
    for name, img in make_birds().items():
        save(img, "Nature", name)
    for name, img in make_emotes().items():
        save(img, "UI", name)
    for name, img in make_pests().items():
        save(img, "Nature", name)
    for name, img in make_lesson_icons().items():
        save(img, "Nature", name)
    for name, img in make_stars().items():
        save(img, "UI", name)

    make_sfx(os.path.join(OUT, "SFX"))

    print("Selesai.")


if __name__ == "__main__":
    main()
