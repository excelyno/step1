using System.Drawing.Imaging;

namespace FarmGame.Rendering;

/// <summary>
/// Penggambar memakai file PNG di folder Assets (hasil Tools/generate_assets.py).
/// Nama file = sprite key, contoh "player_down_2.png", "crop_parsnip_3.png".
/// Kalau gambar tidak ada, otomatis pakai PlaceholderAssets (kotak warna) -> game tidak pernah crash karena gambar hilang.
/// </summary>
public class SpriteAssets : IAssetProvider
{
    private readonly Dictionary<string, Bitmap> _images = new();
    private readonly Dictionary<string, int> _variantCount = new();   // "grass" -> 10 variasi
    private readonly PlaceholderAssets _fallback = new();
    private readonly ImageAttributes _noBleed = new();                // cegah garis tipis di tepi gambar saat diperbesar
    private readonly ImageAttributes _faded = new();                  // MVP 5: sama, tapi tembus pandang
    private float _opacity = 1f;

    public SpriteAssets()
    {
        _noBleed.SetWrapMode(WrapMode.TileFlipXY);
        _faded.SetWrapMode(WrapMode.TileFlipXY);

        string folder = Path.Combine(AppContext.BaseDirectory, "Assets");
        if (!Directory.Exists(folder))
            return;   // tanpa folder: semua pakai placeholder

        foreach (string file in Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories))
        {
            // Salin ke format PArgb: format paling cepat untuk digambar GDI+
            using Bitmap loaded = new(file);
            Bitmap fast = new(loaded.Width, loaded.Height, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(fast))
                g.DrawImage(loaded, 0, 0, loaded.Width, loaded.Height);

            _images[Path.GetFileNameWithoutExtension(file)] = fast;
        }

        foreach (string name in new[] { "grass", "dirt", "path", "water", "floor", "cobble" })
            _variantCount[name] = _images.Keys.Count(key => key.StartsWith(name + "_") && char.IsDigit(key[^1]));
    }

    // ------------------------------------------------------------------ sprite

    /// <summary>MVP 5: tembus pandang memakai ColorMatrix (kanal alfa dikali opacity).</summary>
    public float Opacity
    {
        get => _opacity;
        set
        {
            float clamped = Math.Clamp(value, 0f, 1f);
            if (Math.Abs(clamped - _opacity) < 0.001f)
                return;
            _opacity = clamped;
            _faded.SetColorMatrix(new ColorMatrix { Matrix33 = clamped });
        }
    }

    public void DrawSprite(Graphics g, string spriteKey, RectangleF dest)
    {
        if (!TryDraw(g, spriteKey, dest))
            _fallback.DrawSprite(g, spriteKey, dest);
    }

    // ------------------------------------------------------------------ tile

    public void DrawTile(Graphics g, TileMap map, int x, int y, RectangleF dest)
    {
        TileType type = map.GetTile(x, y)!.Type;

        // 1. Gambar dasar
        bool ok;
        if (type == TileType.Dirt) ok = DrawVariant(g, "dirt", x, y, dest);
        else if (type == TileType.Path) ok = DrawVariant(g, "path", x, y, dest);
        else if (type == TileType.Water) ok = TryDraw(g, "water_" + WaterFrame(), dest);
        else if (type == TileType.Void) { g.FillRectangle(Brushes.Black, dest); return; }
        else if (type == TileType.Floor) ok = DrawVariant(g, "floor", x, y, dest);
        else if (type == TileType.Wall) ok = TryDraw(g, WallKey(map, x, y), dest);
        else if (type == TileType.Cobble) ok = DrawVariant(g, "cobble", x, y, dest);
        else if (type == TileType.Bridge) ok = DrawBridge(g, map, x, y, dest);
        else ok = DrawVariant(g, "grass", x, y, dest);   // Grass, Fence, Building (rumah digambar di atasnya)

        if (!ok)
        {
            _fallback.DrawTile(g, map, x, y, dest);
            return;
        }

        // 2. Hiasan sesuai tetangga (autotile sederhana)
        if (type == TileType.Dirt || type == TileType.Path || type == TileType.Cobble)
            DrawGrassEdges(g, map, x, y, dest);
        else if (type == TileType.Water)
            DrawWaterEdges(g, map, x, y, dest);
        else if (type == TileType.Fence)
            DrawFence(g, map, x, y, dest);
    }

    /// <summary>Rumput "tumpah" ke tanah/jalan di sisi yang bertetangga dengan rumput.</summary>
    private void DrawGrassEdges(Graphics g, TileMap map, int x, int y, RectangleF dest)
    {
        bool n = IsGrassy(map, x, y - 1), s = IsGrassy(map, x, y + 1);
        bool w = IsGrassy(map, x - 1, y), e = IsGrassy(map, x + 1, y);

        if (n) TryDraw(g, "edge_grass_n", dest);
        if (s) TryDraw(g, "edge_grass_s", dest);
        if (w) TryDraw(g, "edge_grass_w", dest);
        if (e) TryDraw(g, "edge_grass_e", dest);

        // Sudut dalam: hanya tetangga diagonal yang rumput
        if (!n && !w && IsGrassy(map, x - 1, y - 1)) TryDraw(g, "edge_grass_nw", dest);
        if (!n && !e && IsGrassy(map, x + 1, y - 1)) TryDraw(g, "edge_grass_ne", dest);
        if (!s && !w && IsGrassy(map, x - 1, y + 1)) TryDraw(g, "edge_grass_sw", dest);
        if (!s && !e && IsGrassy(map, x + 1, y + 1)) TryDraw(g, "edge_grass_se", dest);
    }

    /// <summary>Tepi kolam: tebing tanah di atas, buih di bawah dan samping.</summary>
    private void DrawWaterEdges(Graphics g, TileMap map, int x, int y, RectangleF dest)
    {
        // MVP 5: jembatan dihitung sebagai air, supaya sungai tidak berbuih di bawah jembatan
        if (!IsWaterish(map, x, y - 1)) TryDraw(g, "edge_water_n", dest);
        if (!IsWaterish(map, x, y + 1)) TryDraw(g, "edge_water_s", dest);
        if (!IsWaterish(map, x - 1, y)) TryDraw(g, "edge_water_w", dest);
        if (!IsWaterish(map, x + 1, y)) TryDraw(g, "edge_water_e", dest);
    }

    private static bool IsWaterish(TileMap map, int x, int y) =>
        IsType(map, x, y, TileType.Water) || IsType(map, x, y, TileType.Bridge);

    /// <summary>
    /// MVP 5: jembatan / dermaga. Di bawahnya air beriak, di atasnya papan kayu.
    /// Arah papan mengikuti arah jalan: tegak kalau ujung atasnya daratan dan (ujung bawahnya juga daratan
    /// atau kiri-kanannya air), selain itu mendatar. Pagar pinggir digambar di sisi yang bertemu air.
    /// </summary>
    private bool DrawBridge(Graphics g, TileMap map, int x, int y, RectangleF dest)
    {
        if (!TryDraw(g, "water_" + WaterFrame(), dest))
            return false;

        bool waterN = IsType(map, x, y - 1, TileType.Water), waterS = IsType(map, x, y + 1, TileType.Water);
        bool waterW = IsType(map, x - 1, y, TileType.Water), waterE = IsType(map, x + 1, y, TileType.Water);
        bool vertical = !waterN && (!waterS || (waterW && waterE));

        TryDraw(g, vertical ? "bridge_v" : "bridge_h", dest);
        if (vertical)
        {
            if (waterW) TryDraw(g, "bridge_edge_w", dest);
            if (waterE) TryDraw(g, "bridge_edge_e", dest);
        }
        else
        {
            if (waterN) TryDraw(g, "bridge_edge_n", dest);
            if (waterS) TryDraw(g, "bridge_edge_s", dest);
        }
        return true;
    }

    /// <summary>Pagar: palang ke arah pagar tetangga, lalu tiang di tengah.</summary>
    private void DrawFence(Graphics g, TileMap map, int x, int y, RectangleF dest)
    {
        if (IsType(map, x, y - 1, TileType.Fence)) TryDraw(g, "fence_n", dest);
        if (IsType(map, x, y + 1, TileType.Fence)) TryDraw(g, "fence_s", dest);
        if (IsType(map, x - 1, y, TileType.Fence)) TryDraw(g, "fence_w", dest);
        if (IsType(map, x + 1, y, TileType.Fence)) TryDraw(g, "fence_e", dest);
        TryDraw(g, "fence_post", dest);
    }

    /// <summary>
    /// MVP 3: dinding yang menghadap lantai (ada lantai di bawahnya) = sisi depan berkertas dinding.
    /// Dinding lain = bagian atas yang gelap.
    /// </summary>
    private static string WallKey(TileMap map, int x, int y)
    {
        return IsType(map, x, y + 1, TileType.Floor) ? "wall_front" : "wall_top";
    }

    // ------------------------------------------------------------------ bantuan

    // ------------------------------------------------------------------ MVP 6: tile beranimasi di atas cache

    /// <summary>
    /// MVP 6: digambar LANGSUNG tiap frame hanya untuk tile yang benar-benar bergerak (air dan jembatan),
    /// supaya cache potongan peta tetap valid dan air tetap beriak. Tile lain (rumput, tanah, jalan) statis.
    /// </summary>
    public void DrawAnimatedTiles(Graphics g, TileMap map, RectangleF view)
    {
        int size = GameConfig.TileSize;
        int x0 = Math.Max(0, TileMap.WorldToTile(view.Left));
        int y0 = Math.Max(0, TileMap.WorldToTile(view.Top));
        int x1 = Math.Min(map.Width - 1, TileMap.WorldToTile(view.Right));
        int y1 = Math.Min(map.Height - 1, TileMap.WorldToTile(view.Bottom));

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                TileType type = map.GetTile(x, y)!.Type;
                if (type != TileType.Water && type != TileType.Bridge)
                    continue;

                var dest = new RectangleF(x * size, y * size, size, size);
                if (type == TileType.Water)
                {
                    if (!TryDraw(g, "water_" + WaterFrame(), dest))
                        DrawVariant(g, "water", x, y, dest);
                }
                else
                {
                    DrawBridge(g, map, x, y, dest);
                }
            }
        }
    }

    /// <summary>Pilih variasi tile dari posisi (selalu sama untuk tile yang sama, tapi beda antar tile).</summary>
    private bool DrawVariant(Graphics g, string name, int x, int y, RectangleF dest)    {
        int count = _variantCount.GetValueOrDefault(name);
        if (count == 0)
            return false;

        int hash = (x * 73856093) ^ (y * 19349663);
        int index = (hash & 0x7fffffff) % count;
        return TryDraw(g, $"{name}_{index}", dest);
    }

    /// <summary>Frame air berganti tiap 0,4 detik.</summary>
    private int WaterFrame()
    {
        int count = Math.Max(1, _variantCount.GetValueOrDefault("water"));
        return (int)(Environment.TickCount64 / 400 % count);
    }

    private static bool IsType(TileMap map, int x, int y, TileType type)
    {
        Tile? tile = map.GetTile(x, y);
        return tile == null ? map.GetTile(Math.Clamp(x, 0, map.Width - 1), Math.Clamp(y, 0, map.Height - 1))!.Type == type
                            : tile.Type == type;
    }

    private static bool IsGrassy(TileMap map, int x, int y)
    {
        Tile? tile = map.GetTile(x, y);
        return tile != null && (tile.Type == TileType.Grass || tile.Type == TileType.Fence || tile.Type == TileType.Building);
    }

    /// <summary>Gambar PNG kalau ada. Posisi dibulatkan ke piksel supaya pixel art tidak "goyang".</summary>
    private bool TryDraw(Graphics g, string key, RectangleF dest)
    {
        if (!_images.TryGetValue(key, out Bitmap? image))
            return false;

        var rect = new Rectangle((int)MathF.Round(dest.X), (int)MathF.Round(dest.Y),
                                 (int)MathF.Round(dest.Width), (int)MathF.Round(dest.Height));
        g.DrawImage(image, rect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, _opacity < 0.999f ? _faded : _noBleed);
        return true;
    }
}
