namespace FarmGame.UI;

/// <summary>
/// Gambar menu toko di tengah layar (koordinat layar). Hanya MENGGAMBAR;
/// pilihan dan pembelian diurus ShopState.
/// </summary>
public class ShopMenu
{
    private const int PanelWidth = 520;
    private const int RowHeight = 44;
    private const int MaxVisibleRows = 6;

    private readonly IAssetProvider _assets;
    private readonly Font _title = new("Segoe UI", 15f, FontStyle.Bold);
    private readonly Font _normal = new("Segoe UI", 11f, FontStyle.Bold);
    private readonly Font _small = new("Segoe UI", 9f);

    public ShopMenu(IAssetProvider assets)
    {
        _assets = assets;
    }

    /// <summary>Baris barang di bawah mouse, atau -1. Tata letak sama dengan Draw.</summary>
    public static int HitTestRow(Point mouse, int stockCount, int selected)
    {
        if (stockCount == 0)
            return -1;

        int rows = Math.Max(1, Math.Min(stockCount, MaxVisibleRows));
        int x = (GameConfig.ScreenWidth - PanelWidth) / 2;
        int y = 40;
        var list = new Rectangle(x + 12, y + 48, PanelWidth - 24, rows * RowHeight + 8);

        if (!list.Contains(mouse))
            return -1;

        int first = Math.Clamp(selected - MaxVisibleRows + 1, 0, Math.Max(0, stockCount - MaxVisibleRows));
        int row = (mouse.Y - list.Y - 4) / RowHeight;
        int index = first + row;
        return row >= 0 && row < rows && index >= 0 && index < stockCount ? index : -1;
    }

    public void Draw(Graphics g, Shop shop, List<ShopEntry> stock, int selected, int gold, int hover = -1)
    {
        int rows = Math.Max(1, Math.Min(stock.Count, MaxVisibleRows));
        int height = 90 + rows * RowHeight + 40;
        int x = (GameConfig.ScreenWidth - PanelWidth) / 2;
        int y = 40;

        // Panel kayu
        using SolidBrush panel = new(Color.FromArgb(235, 92, 62, 38));
        using SolidBrush paper = new(Color.FromArgb(245, 238, 222, 186));
        using Pen frame = new(Color.FromArgb(60, 36, 20), 3);
        g.FillRectangle(panel, x, y, PanelWidth, height);
        g.DrawRectangle(frame, x, y, PanelWidth, height);

        // Judul + uang. Judul dipotong (ellipsis) supaya tidak menabrak angka uang di kanan.
        using SolidBrush goldBrush = new(Color.Gold);
        string goldText = $"{gold} g";
        float goldWidth = g.MeasureString(goldText, _title).Width;
        var titleClip = new RectangleF(x + 16, y + 8, PanelWidth - goldWidth - 48, 30);
        using StringFormat titleFormat = new() { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString($"{shop.Name} {shop.OwnerName}", _title, Brushes.White, titleClip, titleFormat);
        g.DrawString(goldText, _title, goldBrush, x + PanelWidth - goldWidth - 16, y + 10);

        // Daftar barang
        var list = new Rectangle(x + 12, y + 48, PanelWidth - 24, rows * RowHeight + 8);
        g.FillRectangle(paper, list);

        // Gulir: baris terpilih selalu terlihat (dipakai juga untuk indikator gulir)
        int first = Math.Clamp(selected - MaxVisibleRows + 1, 0, Math.Max(0, stock.Count - MaxVisibleRows));

        if (stock.Count == 0)
        {
            g.DrawString("Tidak ada bibit untuk musim ini.", _normal, Brushes.SaddleBrown, list.X + 12, list.Y + 14);
        }
        else
        {
            for (int i = 0; i < rows; i++)
            {
                int index = first + i;
                var row = new Rectangle(list.X + 4, list.Y + 4 + i * RowHeight, list.Width - 8, RowHeight - 2);
                DrawRow(g, stock[index], row, index == selected, index == hover, gold);
            }
        }

        // Petunjuk tombol — dua baris pendek supaya muat di dalam panel
        g.DrawString("W/S atau mouse pilih  ·  klik kiri / C = beli  ·  Shift = 5", _small, Brushes.White, x + 16, y + height - 44);
        g.DrawString("X / Esc / klik kanan = keluar", _small, Brushes.White, x + 16, y + height - 26);

        // Indikator gulir: ada barang di luar 6 baris yang terlihat
        if (stock.Count > MaxVisibleRows)
        {
            bool canUp = first > 0;
            bool canDown = first + rows < stock.Count;
            using SolidBrush arrow = new(Color.FromArgb(230, 255, 235, 190));
            string up = canUp ? "▲" : " ";
            string down = canDown ? "▼" : " ";
            g.DrawString(up, _normal, arrow, x + PanelWidth - 34, y + 50);
            g.DrawString(down, _normal, arrow, x + PanelWidth - 34, y + height - 76);
        }
    }

    private void DrawRow(Graphics g, ShopEntry entry, Rectangle row, bool isSelected, bool isHovered, int gold)
    {
        if (isSelected || isHovered)
        {
            using SolidBrush highlight = new(isSelected ? Color.FromArgb(255, 250, 214, 140)
                                                        : Color.FromArgb(220, 240, 228, 205));
            g.FillRectangle(highlight, row);
        }
        if (isSelected)
        {
            using Pen border = new(Color.OrangeRed, 2);
            g.DrawRectangle(border, row);
        }
        else if (isHovered)
        {
            using Pen hover = new(Color.FromArgb(255, 110, 180, 240), 2);
            g.DrawRectangle(hover, row);
        }

        // Ikon 32x32 (sprite 16px diperbesar 2x = tetap tajam)
        _assets.DrawSprite(g, entry.Item.SpriteKey, new RectangleF(row.X + 6, row.Y + 5, 32, 32));

        // Harga: merah kalau uang tidak cukup. Lebar harga dicadangkan, sisanya untuk nama & info.
        Brush priceBrush = entry.Price <= gold ? Brushes.DarkGreen : Brushes.Firebrick;
        string price = $"{entry.Price} g";
        float width = g.MeasureString(price, _normal).Width;
        g.DrawString(price, _normal, priceBrush, row.Right - width - 10, row.Y + 11);

        // Nama & info dipotong (ellipsis) supaya tidak menimpa kolom harga / keluar baris
        float textWidth = row.Width - 48 - width - 24;
        var clip = new RectangleF(row.X + 48, row.Y + 2, textWidth, RowHeight - 6);
        using StringFormat trim = new() { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString(entry.Item.Name, _normal, Brushes.Black, new RectangleF(clip.X, clip.Y, clip.Width, 18), trim);
        g.DrawString(entry.Info, _small, Brushes.DimGray, new RectangleF(clip.X, clip.Y + 19, clip.Width, 16), trim);
    }
}
