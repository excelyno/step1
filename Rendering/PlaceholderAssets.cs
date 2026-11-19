namespace FarmGame.Rendering;

/// <summary>
/// Grafis sementara: tile = kotak warna, karakter = kotak warna + penanda arah hadap.
/// Nama sprite yang tidak dikenal digambar magenta supaya gampang ketahuan.
/// </summary>
public class PlaceholderAssets : IAssetProvider
{
    private readonly Dictionary<TileType, Color> _tileColors = new()
    {
        { TileType.Void,     Color.Black },
        { TileType.Grass,    Color.FromArgb(106, 168, 79) },
        { TileType.Dirt,     Color.FromArgb(181, 136, 99) },
        { TileType.Path,     Color.FromArgb(214, 196, 150) },
        { TileType.Water,    Color.FromArgb(74, 134, 196) },
        { TileType.Fence,    Color.FromArgb(120, 84, 50) },
        { TileType.Building, Color.FromArgb(150, 60, 60) },
    };

    private readonly Dictionary<string, Color> _spriteColors = new()
    {
        { "player", Color.FromArgb(60, 90, 200) },
    };

    public void DrawTile(Graphics g, TileType type, RectangleF dest)
    {
        Color color = _tileColors.ContainsKey(type) ? _tileColors[type] : Color.Magenta;

        using SolidBrush brush = new(color);
        using Pen outline = new(Color.FromArgb(40, 0, 0, 0), 1f / GameConfig.Scale);
        g.FillRectangle(brush, dest);
        g.DrawRectangle(outline, dest.X, dest.Y, dest.Width, dest.Height);
    }

    public void DrawSprite(Graphics g, string spriteKey, RectangleF dest)
    {
        // "player_down" -> nama = "player", arah = "down"
        string[] parts = spriteKey.Split('_');
        string name = parts[0];
        Color color = _spriteColors.ContainsKey(name) ? _spriteColors[name] : Color.Magenta;

        using SolidBrush brush = new(color);
        g.FillRectangle(brush, dest);

        if (parts.Length > 1)
            DrawFacingMarker(g, parts[1], dest);
    }

    /// <summary>Kotak kecil gelap di sisi arah hadap, supaya arah karakter terlihat walau belum ada art.</summary>
    private void DrawFacingMarker(Graphics g, string direction, RectangleF r)
    {
        const float s = 4f;
        RectangleF marker;

        if (direction == "up") marker = new RectangleF(r.X + (r.Width - s) / 2, r.Y, s, s);
        else if (direction == "down") marker = new RectangleF(r.X + (r.Width - s) / 2, r.Bottom - s, s, s);
        else if (direction == "left") marker = new RectangleF(r.X, r.Y + (r.Height - s) / 2, s, s);
        else if (direction == "right") marker = new RectangleF(r.Right - s, r.Y + (r.Height - s) / 2, s, s);
        else return;

        using SolidBrush brush = new(Color.FromArgb(30, 30, 30));
        g.FillRectangle(brush, marker);
    }
}
