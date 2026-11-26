namespace FarmGame.UI;

/// <summary>
/// Gambar menu kotak kirim (koordinat layar). Kiri = tas, kanan = isi kotak.
/// Hanya MENGGAMBAR dan menyediakan tata letak slot (HitTest) untuk mouse.
/// Pilihan, pemindahan barang, dan drag & drop diurus ShippingBinState.
/// </summary>
public class ShippingBinMenu
{
    public const int SlotSize = 44;
    public const int Gap = 4;
    public const int Pitch = SlotSize + Gap;
    public const int Columns = 6;

    public const int PanelWidth = 660;
    public const int PanelHeight = 436;
    public const int PanelX = (GameConfig.ScreenWidth - PanelWidth) / 2;
    public const int PanelY = 100;   // di bawah jam HUD supaya tidak menutupinya

    private const int LeftX = PanelX + 24;
    private const int RightX = PanelX + 356;
    private const int GridY = PanelY + 82;

    private readonly IAssetProvider _assets;
    private readonly Font _title = new("Segoe UI", 15f, FontStyle.Bold);
    private readonly Font _heading = new("Segoe UI", 10f, FontStyle.Bold);
    private readonly Font _text = new("Segoe UI", 9f);
    private readonly Font _small = new("Segoe UI", 8.5f, FontStyle.Italic);

    public ShippingBinMenu(IAssetProvider assets)
    {
        _assets = assets;
    }

    /// <summary>Kotak satu slot di layar. inBin = true untuk grid kotak, false untuk grid tas.</summary>
    public static Rectangle SlotRect(bool inBin, int index)
    {
        int column = index % Columns;
        int row = index / Columns;
        return new Rectangle((inBin ? RightX : LeftX) + column * Pitch, GridY + row * Pitch, SlotSize, SlotSize);
    }

    /// <summary>Slot mana yang ada di bawah mouse? null kalau tidak kena slot mana pun.</summary>
    public static (bool InBin, int Index)? HitTest(Point mouse, int bagCapacity, int binCapacity)
    {
        for (int i = 0; i < bagCapacity; i++)
        {
            if (SlotRect(false, i).Contains(mouse))
                return (false, i);
        }
        for (int i = 0; i < binCapacity; i++)
        {
            if (SlotRect(true, i).Contains(mouse))
                return (true, i);
        }
        return null;
    }

    public void Draw(Graphics g, ShippingBin bin, Inventory bag, bool focusBin, int selected,
                     (bool InBin, int Index)? hover, ItemStack? dragStack, Point mouse)
    {
        var panel = new Rectangle(PanelX, PanelY, PanelWidth, PanelHeight);
        UiPanel.Draw(g, panel);

        using SolidBrush ink = new(UiPanel.Ink);
        using SolidBrush faded = new(Color.FromArgb(145, 105, 75));
        using SolidBrush gold = new(Color.FromArgb(150, 100, 15));

        g.DrawString("Kotak Kirim", _title, ink, PanelX + 24, PanelY + 14);
        g.DrawString("Barang di dalam kotak dijual otomatis saat kamu tidur.", _small, faded,
                     PanelX + 24, PanelY + 42);

        DrawHeaders(g, bin, ink, faded);

        for (int i = 0; i < bag.Capacity; i++)
        {
            bool isSelected = !focusBin && i == selected;
            bool isHovered = hover.HasValue && !hover.Value.InBin && hover.Value.Index == i;
            ItemSlotView.Draw(g, _assets, SlotRect(false, i), bag.GetSlot(i), isSelected, isHovered);
        }

        for (int i = 0; i < bin.Contents.Capacity; i++)
        {
            bool isSelected = focusBin && i == selected;
            bool isHovered = hover.HasValue && hover.Value.InBin && hover.Value.Index == i;
            ItemSlotView.Draw(g, _assets, SlotRect(true, i), bin.Contents.GetSlot(i), isSelected, isHovered);
        }

        DrawInfo(g, bin, bag, focusBin, selected, ink, faded, gold);
        DrawHelp(g, faded);

        if (dragStack != null)
            DrawDragGhost(g, dragStack, mouse);

        if (dragStack == null && hover.HasValue)
        {
            ItemStack? hovered = hover.Value.InBin ? bin.Contents.GetSlot(hover.Value.Index)
                                                   : bag.GetSlot(hover.Value.Index);
            if (hovered != null)
                DrawTooltip(g, hovered, mouse);
        }
    }

    private void DrawHeaders(Graphics g, ShippingBin bin, Brush ink, Brush faded)
    {
        g.DrawString("Tas", _heading, ink, LeftX, GridY - 24);
        g.DrawString("klik = ambil & geser", _small, faded, LeftX + 36, GridY - 22);

        g.DrawString("Isi Kotak", _heading, ink, RightX, GridY - 24);
        string count = $"{bin.Contents.UsedSlots}/{bin.Contents.Capacity} slot";
        SizeF size = g.MeasureString(count, _small);
        g.DrawString(count, _small, faded, RightX + 284 - size.Width, GridY - 22);
    }

    private void DrawInfo(Graphics g, ShippingBin bin, Inventory bag, bool focusBin, int selected,
                          Brush ink, Brush faded, Brush gold)
    {
        int x = LeftX;
        int y = GridY + 2 * Pitch + 10;

        ItemStack? chosen = focusBin ? bin.Contents.GetSlot(selected) : bag.GetSlot(selected);
        g.DrawString("Barang dipilih", _heading, ink, x, y);
        y += 20;

        if (chosen == null)
        {
            g.DrawString("(slot kosong)", _text, faded, x, y);
            y += 20;
        }
        else
        {
            g.DrawString(chosen.Item.Name, _heading, ink, x, y);
            y += 18;
            string price = chosen.Item.IsSellable ? $"{chosen.Item.GetSellPrice()} g / buah" : "tidak bisa dijual";
            g.DrawString(price, _text, faded, x, y);
            y += 22;
        }

        g.DrawString($"Isi kotak: {bin.Contents.UsedSlots}/{bin.Contents.Capacity} slot", _text, faded, x, y);
        y += 18;
        g.DrawString($"Perkiraan pendapatan: {bin.Contents.TotalSellValue} g", _heading, gold, x, y);
    }

    private void DrawHelp(Graphics g, Brush faded)
    {
        const string help = "Drag & drop dengan mouse  ·  klik kanan = pindah semua  ·  Q/E ganti panel  ·  X/Esc tutup";
        SizeF size = g.MeasureString(help, _small);
        g.DrawString(help, _small, faded, PanelX + (PanelWidth - size.Width) / 2, PanelY + PanelHeight - 26);
    }

    /// <summary>Ikon barang yang sedang dipegang mouse, di tengah kursor.</summary>
    private void DrawDragGhost(Graphics g, ItemStack stack, Point mouse)
    {
        var box = new Rectangle(mouse.X - SlotSize / 2, mouse.Y - SlotSize / 2, SlotSize, SlotSize);
        ItemSlotView.Draw(g, _assets, box, stack);
    }

    /// <summary>Keterangan barang di bawah kursor: nama, harga, dan petunjuk pindah.</summary>
    private void DrawTooltip(Graphics g, ItemStack stack, Point mouse)
    {
        string name = stack.Item.Name;
        string price = stack.Item.IsSellable ? $"{stack.Item.GetSellPrice()} g / buah" : "tidak bisa dijual";
        string hint = "klik kanan: pindah semua";

        SizeF nameSize = g.MeasureString(name, _heading);
        SizeF priceSize = g.MeasureString(price, _text);
        SizeF hintSize = g.MeasureString(hint, _small);
        int width = (int)Math.Max(nameSize.Width, Math.Max(priceSize.Width, hintSize.Width)) + 20;
        int height = (int)(nameSize.Height + priceSize.Height + hintSize.Height) + 18;

        int x = Math.Clamp(mouse.X + 16, 4, GameConfig.ScreenWidth - width - 4);
        int y = Math.Clamp(mouse.Y + 16, 4, GameConfig.ScreenHeight - height - 4);

        using SolidBrush back = new(Color.FromArgb(235, 42, 30, 22));
        using Pen border = new(Color.FromArgb(200, 214, 152, 92), 1.5f);
        g.FillRectangle(back, x, y, width, height);
        g.DrawRectangle(border, x, y, width, height);

        g.DrawString(name, _heading, Brushes.White, x + 10, y + 6);
        g.DrawString(price, _text, Brushes.Gainsboro, x + 10, y + 6 + nameSize.Height);
        g.DrawString(hint, _small, Brushes.DarkGray, x + 10, y + 8 + nameSize.Height + priceSize.Height);
    }
}
