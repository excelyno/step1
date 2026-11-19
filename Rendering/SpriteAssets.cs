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

    public SpriteAssets()
    {
        _noBleed.SetWrapMode(WrapMode.TileFlipXY);

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

        foreach (string name in new[] { "grass", "dirt", "path", "water" })
            _variantCount[name] = _images.Keys.Count(key => key.StartsWith(name + "_") && char.IsDigit(key[^1]));
    }

    // ------------------------------------------------------------------ sprite

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
        else ok = DrawVariant(g, "grass", x, y, dest);   // Grass, Fence, Building (rumah digambar di atasnya)

        if (!ok)
        {
            _fallback.DrawTile(g, map, x, y, dest);
            return;
        }

        // 2. Hiasan sesuai tetangga (autotile sederhana)
        if (type == TileType.Dirt || type == TileType.Path)
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
        if (!IsType(map, x, y - 1, TileType.Water)) TryDraw(g, "edge_water_n", dest);
        if (!IsType(map, x, y + 1, TileType.Water)) TryDraw(g, "edge_water_s", dest);
        if (!IsType(map, x - 1, y, TileType.Water)) TryDraw(g, "edge_water_w", dest);
        if (!IsType(map, x + 1, y, TileType.Water)) TryDraw(g, "edge_water_e", dest);
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

    // ------------------------------------------------------------------ bantuan

    /// <summary>Pilih variasi tile dari posisi (selalu sama untuk tile yang sama, tapi beda antar tile).</summary>
    private bool DrawVariant(Graphics g, string name, int x, int y, RectangleF dest)
    {
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
        g.DrawImage(image, rect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, _noBleed);
        return true;
    }
}
