namespace FarmGame.UI;

/// <summary>
/// Gambar menu tas (tombol B): grid slot pemain, nama & info barang terpilih,
/// dan petunjuk tombol. Hanya MENGGAMBAR + HitTest; interaksi diurus BackpackState.
/// </summary>
public class BackpackMenu
{
    public const int SlotSize = 48;
    public const int Gap = 6;
    public const int Pitch = SlotSize + Gap;
    public const int Columns = 6;

    private const int TitleHeight = 64;

    private readonly IAssetProvider _assets;
    private readonly Font _title = new("Segoe UI", 15f, FontStyle.Bold);
    private readonly Font _normal = new("Segoe UI", 11f, FontStyle.Bold);
    private readonly Font _text = new("Segoe UI", 9f);
    private readonly Font _small = new("Segoe UI", 8.5f, FontStyle.Italic);

    public BackpackMenu(IAssetProvider assets)
    {
        _assets = assets;
    }

    /// <summary>Ukuran panel mengikuti jumlah slot (1..4 baris).</summary>
    public static Rectangle PanelRect(int capacity)
    {
        int rows = Math.Max(1, (capacity + Columns - 1) / Columns);
        int width = Columns * Pitch + 24 * 2;
        int height = TitleHeight + rows * Pitch + 76;
        return new Rectangle((GameConfig.ScreenWidth - width) / 2,
                             Math.Max(16, (GameConfig.ScreenHeight - height) / 2 - 20),
                             width, height);
    }

    /// <summary>Kotak slot ke-i di layar.</summary>
    public static Rectangle SlotRect(int index, int capacity)
    {
        Rectangle panel = PanelRect(capacity);
        int column = index % Columns;
        int row = index / Columns;
        return new Rectangle(panel.X + 24 + column * Pitch, panel.Y + TitleHeight + row * Pitch, SlotSize, SlotSize);
    }

    /// <summary>Slot di bawah mouse, atau -1.</summary>
    public static int HitTest(Point mouse, int capacity)
    {
        for (int i = 0; i < capacity; i++)
        {
            if (SlotRect(i, capacity).Contains(mouse))
                return i;
        }
        return -1;
    }

    public void Draw(Graphics g, Inventory inventory, int selected, int hover = -1)
    {
        Rectangle panel = PanelRect(inventory.Capacity);
        UiPanel.Draw(g, panel);
        using SolidBrush ink = new(UiPanel.Ink);
        using SolidBrush faded = new(Color.FromArgb(145, 105, 75));

        string levelName = inventory.BackpackLevel switch
        {
            0 => "Tas Kanvas",
            1 => "Tas Besar",
            _ => "Tas Raksasa",
        };

        // Judul dipotong (ellipsis) supaya tidak menabrak jumlah slot di kanan
        string title = $"{levelName}  ({inventory.UsedSlots}/{inventory.Capacity} slot terpakai)";
        float titleWidth = g.MeasureString(title, _title).Width;
        var titleClip = new RectangleF(panel.X + 24, panel.Y + 10, panel.Width - 48, 30);
        using StringFormat trim = new() { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        if (titleWidth <= titleClip.Width)
            g.DrawString(title, _title, ink, panel.X + 24, panel.Y + 12);
        else
            g.DrawString(title, _title, ink, titleClip, trim);

        // Petunjuk di baris kedua, rata kiri dan di-clip ke dalam panel
        g.DrawString("B / Esc = tutup  ·  klik atau 1..9 memilih  ·  X = makan/periksa barang", _small, faded,
                     new RectangleF(panel.X + 24, panel.Y + 40, panel.Width - 48, 16), trim);

        // Slot
        for (int i = 0; i < inventory.Capacity; i++)
        {
            bool isSelected = i == selected;
            bool isHovered = i == hover;
            ItemSlotView.Draw(g, _assets, SlotRect(i, inventory.Capacity), inventory.GetSlot(i), isSelected, isHovered);
        }

        // Info barang terpilih di bawah grid (deskripsi di-clip supaya tidak keluar panel)
        ItemStack? stack = inventory.SelectedStack;
        int infoY = panel.Bottom - 62;
        if (stack != null)
        {
            _assets.DrawSprite(g, stack.Item.SpriteKey, new RectangleF(panel.X + 24, infoY, 40, 40));
            float textLeft = panel.X + 74;
            float textWidth = panel.Width - 48 - 50 - (stack.Item is Tool ? 150 : 0);
            g.DrawString(stack.Item.Name, _normal, ink, new RectangleF(textLeft, infoY - 2, textWidth, 18), trim);
            g.DrawString(stack.Item.Description, _text, faded, new RectangleF(textLeft, infoY + 16, textWidth, 30), trim);
            if (stack.Item is Tool tool)
            {
                string daya = tool.IsBroken ? "HANCUR" : $"Daya tahan: {tool.Durability}/{tool.MaxDurability} pemakaian";
                g.DrawString(daya, _small, faded, new RectangleF(panel.Right - 174, infoY + 4, 150, 16), trim);
            }
        }
        else
        {
            g.DrawString("Slot kosong. Klik barang untuk memilihnya.", _small, faded, panel.X + 24, infoY + 6);
        }
    }
}
