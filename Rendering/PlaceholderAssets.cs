namespace FarmGame.Rendering;

/// <summary>
/// Grafis sementara: semua digambar kotak warna, tanpa file gambar.
/// Format nama sprite: "kategori_nama[_detail]", contoh "player_down", "crop_parsnip_2", "tool_hoe".
/// Nama yang tidak dikenal digambar magenta supaya gampang ketahuan.
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

    private readonly Font _letterFont = new("Segoe UI", 7f, FontStyle.Bold, GraphicsUnit.Pixel);
    private readonly StringFormat _center = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

    public void DrawTile(Graphics g, TileMap map, int x, int y, RectangleF dest)
    {
        TileType type = map.GetTile(x, y)!.Type;
        Color color = _tileColors.ContainsKey(type) ? _tileColors[type] : Color.Magenta;

        using SolidBrush brush = new(color);
        using Pen outline = new(Color.FromArgb(40, 0, 0, 0), 1f / GameConfig.Scale);
        g.FillRectangle(brush, dest);
        g.DrawRectangle(outline, dest.X, dest.Y, dest.Width, dest.Height);
    }

    public void DrawSprite(Graphics g, string spriteKey, RectangleF dest)
    {
        string[] parts = spriteKey.Split('_');
        string category = parts[0];

        if (category == "player")
        {
            Fill(g, Color.FromArgb(60, 90, 200), dest);
            DrawFacingMarker(g, parts[1], dest);   // "player_down_2" -> "down"
        }
        else if (category == "hoedirt")
        {
            bool wet = parts[1] == "wet";
            Fill(g, wet ? Color.FromArgb(95, 62, 40) : Color.FromArgb(140, 95, 60), dest);
        }
        else if (category == "crop")
        {
            DrawCrop(g, parts, dest);
        }
        else if (category == "bin")
        {
            Fill(g, Color.FromArgb(150, 110, 60), new RectangleF(dest.X, dest.Y + dest.Height * 0.25f, dest.Width, dest.Height * 0.75f));
            DrawLetter(g, "$", dest);
        }
        else if (category == "tree" || category == "bush")
        {
            Fill(g, Color.FromArgb(50, 110, 45), dest);
        }
        else if (category == "rock")
        {
            Fill(g, Color.Gray, dest);
        }
        else if (category == "tuft" || category == "flowers" || category == "lily" || category == "butterfly")
        {
            // hiasan kecil: tidak digambar di mode placeholder
        }
        else if (category == "house")
        {
            // kotak placeholder: rumah sudah terlihat dari tile Building, tidak perlu digambar
        }
        else if (category == "door")
        {
            Fill(g, Color.FromArgb(90, 50, 30), new RectangleF(dest.X + dest.Width * 0.2f, dest.Y, dest.Width * 0.6f, dest.Height));
        }
        else if (category == "tool")
        {
            DrawIcon(g, Color.FromArgb(150, 150, 160), parts[1], dest);
        }
        else if (category == "seed")
        {
            DrawIcon(g, Color.FromArgb(170, 130, 80), parts[1], dest);
        }
        else if (category == "produce")
        {
            DrawIcon(g, ColorFromName(parts[1]), parts[1], dest);
        }
        else
        {
            Fill(g, Color.Magenta, dest);
        }
    }

    /// <summary>Tanaman: kotak hijau makin besar tiap tahap. Tahap siap panen diberi "buah" bulat.</summary>
    private void DrawCrop(Graphics g, string[] parts, RectangleF r)
    {
        if (parts[1] == "dead")
        {
            Fill(g, Color.FromArgb(120, 100, 70), new RectangleF(r.X + r.Width * 0.3f, r.Y + r.Height * 0.4f, r.Width * 0.4f, r.Height * 0.5f));
            return;
        }

        int stage = int.Parse(parts[^1]);
        float size = r.Width * (0.2f + stage * 0.17f);   // 20% .. 88% lebar tile
        Fill(g, Color.FromArgb(60, 150, 50), new RectangleF(r.X + (r.Width - size) / 2, r.Bottom - size - 1, size, size));

        if (stage >= Crop.StageCount - 1)
        {
            using SolidBrush fruit = new(ColorFromName(parts[1]));
            g.FillEllipse(fruit, r.X + r.Width * 0.3f, r.Y + r.Height * 0.15f, r.Width * 0.4f, r.Width * 0.4f);
        }
    }

    /// <summary>Ikon item: kotak warna + huruf pertama nama.</summary>
    private void DrawIcon(Graphics g, Color color, string name, RectangleF r)
    {
        Fill(g, color, r);
        DrawLetter(g, name[..1].ToUpper(), r);
    }

    private void DrawLetter(Graphics g, string text, RectangleF r)
    {
        // Ukuran huruf mengikuti tinggi kotak (dipakai di dunia 16px DAN di toolbar 28px)
        using Font font = new("Segoe UI", Math.Max(4f, r.Height * 0.55f), FontStyle.Bold, GraphicsUnit.Pixel);
        g.DrawString(text, font, Brushes.White, r, _center);
    }

    /// <summary>Warna tetap dari nama, supaya tiap tanaman punya warna sendiri tanpa ditulis manual.</summary>
    private static Color ColorFromName(string name)
    {
        int h = 17;
        foreach (char c in name)
            h = unchecked(h * 31 + c);
        return Color.FromArgb(120 + Math.Abs(h % 120), 80 + Math.Abs(h / 7 % 140), 60 + Math.Abs(h / 13 % 120));
    }

    private static void Fill(Graphics g, Color color, RectangleF r)
    {
        using SolidBrush brush = new(color);
        g.FillRectangle(brush, r);
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

        Fill(g, Color.FromArgb(30, 30, 30), marker);
    }
}
