namespace FarmGame.UI;

/// <summary>
/// Menggambar SATU slot barang (latar, bingkai, ikon, jumlah, bar kecil).
/// Dipakai menu kotak kirim supaya tampilan slot sama di semua tempat.
/// Hanya menggambar; tidak tahu aturan game apa pun.
/// </summary>
public static class ItemSlotView
{
    private static readonly Font CountFont = new("Segoe UI", 9f, FontStyle.Bold);

    public static void Draw(Graphics g, IAssetProvider assets, Rectangle slot, ItemStack? stack,
                            bool selected = false, bool hovered = false)
    {
        using SolidBrush back = new(selected ? Color.FromArgb(255, 250, 214, 140)
                                             : Color.FromArgb(242, 236, 214, 170));
        g.FillRectangle(back, slot);

        using Pen border = new(selected ? Color.OrangeRed : Color.FromArgb(150, 100, 60), selected ? 3 : 2);
        g.DrawRectangle(border, slot);

        if (hovered)
        {
            var inner = new Rectangle(slot.X + 1, slot.Y + 1, slot.Width - 2, slot.Height - 2);
            using Pen hover = new(Color.FromArgb(255, 110, 180, 240), 2);
            g.DrawRectangle(hover, inner);
        }

        if (stack == null)
            return;

        var icon = new RectangleF(slot.X + 6, slot.Y + 5, 32, 32);
        assets.DrawSprite(g, stack.Item.SpriteKey, icon);

        if (stack.Quantity > 1)
            DrawCount(g, stack.Quantity.ToString(), slot);

        float? meter = stack.Item.MeterRatio;
        if (meter != null)
        {
            using SolidBrush back2 = new(Color.FromArgb(60, 40, 30));
            using SolidBrush water = new(Color.DeepSkyBlue);
            g.FillRectangle(back2, slot.X + 6, slot.Bottom - 8, slot.Width - 12, 4);
            g.FillRectangle(water, slot.X + 6, slot.Bottom - 8, (slot.Width - 12) * meter.Value, 4);
        }
    }

    private static void DrawCount(Graphics g, string text, Rectangle slot)
    {
        SizeF size = g.MeasureString(text, CountFont);
        float x = slot.Right - size.Width - 3;
        float y = slot.Bottom - size.Height - 1;
        g.DrawString(text, CountFont, Brushes.Black, x + 1, y + 1);
        g.DrawString(text, CountFont, Brushes.White, x, y);
    }
}
