namespace FarmGame.UI;

/// <summary>
/// Tampilan di atas dunia (koordinat layar, tidak ikut kamera):
/// jam + tanggal + uang (kanan atas), energi (kanan bawah), toolbar (bawah tengah),
/// layar gelap dan ringkasan akhir hari.
/// </summary>
public class Hud
{
    private const int SlotSize = 48;

    private readonly IAssetProvider _assets;
    private readonly Font _small = new("Segoe UI", 9f, FontStyle.Bold);
    private readonly Font _normal = new("Segoe UI", 11f, FontStyle.Bold);
    private readonly Font _big = new("Segoe UI", 20f, FontStyle.Bold);

    // Label tombol di bawah tiap slot
    private static readonly string[] SlotLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" };

    public Hud(IAssetProvider assets)
    {
        _assets = assets;
    }

    public void Draw(Graphics g, Player player, TimeManager time)
    {
        DrawClock(g, player, time);
        DrawEnergy(g, player.Stamina);
        DrawToolbar(g, player.Inventory);
    }

    // ---------- Jam, tanggal, uang ----------

    private void DrawClock(Graphics g, Player player, TimeManager time)
    {
        var box = new Rectangle(GameConfig.ScreenWidth - 178, 8, 170, 78);
        using SolidBrush panel = new(Color.FromArgb(200, 60, 40, 25));
        using SolidBrush gold = new(Color.Gold);
        g.FillRectangle(panel, box);

        g.DrawString(time.Date.ToString(), _normal, Brushes.White, box.X + 10, box.Y + 4);
        g.DrawString(time.ClockText, _big, Brushes.White, box.X + 8, box.Y + 22);
        g.DrawString($"{player.Wallet.Gold} g", _normal, gold, box.X + 100, box.Y + 34);
    }

    // ---------- Energi ----------

    private void DrawEnergy(Graphics g, Stamina stamina)
    {
        var box = new Rectangle(GameConfig.ScreenWidth - 30, GameConfig.ScreenHeight - 168, 22, 160);
        using SolidBrush panel = new(Color.FromArgb(200, 40, 30, 20));
        g.FillRectangle(panel, box);

        // Warna: hijau > 50%, kuning > 20%, merah
        Color color = Color.FromArgb(90, 200, 90);
        if (stamina.Ratio <= 0.5f) color = Color.Gold;
        if (stamina.Ratio <= 0.2f) color = Color.FromArgb(220, 60, 50);

        int fill = (int)((box.Height - 6) * stamina.Ratio);
        using SolidBrush bar = new(color);
        g.FillRectangle(bar, box.X + 3, box.Bottom - 3 - fill, box.Width - 6, fill);   // terisi dari bawah

        g.DrawString("E", _small, Brushes.White, box.X + 5, box.Y - 18);
    }

    // ---------- Toolbar ----------

    private void DrawToolbar(Graphics g, Inventory inventory)
    {
        int totalWidth = inventory.Capacity * SlotSize;
        int x0 = (GameConfig.ScreenWidth - totalWidth) / 2;
        int y = GameConfig.ScreenHeight - SlotSize - 8;

        using SolidBrush slotBrush = new(Color.FromArgb(210, 230, 200, 150));
        using Pen border = new(Color.FromArgb(120, 80, 50), 2);
        using Pen selected = new(Color.OrangeRed, 3);

        for (int i = 0; i < inventory.Capacity; i++)
        {
            var slot = new Rectangle(x0 + i * SlotSize, y, SlotSize, SlotSize);
            g.FillRectangle(slotBrush, slot);
            g.DrawRectangle(border, slot);

            ItemStack? stack = inventory.GetSlot(i);
            if (stack != null)
                DrawStack(g, stack, slot);

            g.DrawString(SlotLabels[i], _small, Brushes.DimGray, slot.X + 2, slot.Y);
        }

        // Sorotan slot terpilih
        var chosen = new Rectangle(x0 + inventory.SelectedIndex * SlotSize, y, SlotSize, SlotSize);
        g.DrawRectangle(selected, chosen);

        // Nama item terpilih di atas toolbar
        if (inventory.SelectedItem != null)
            DrawTextWithShadow(g, inventory.SelectedItem.Name, _normal, x0, y - 24);
    }

    private void DrawStack(Graphics g, ItemStack stack, Rectangle slot)
    {
        // Ikon (polimorfisme: tiap item punya SpriteKey sendiri)
        var icon = new RectangleF(slot.X + 8, slot.Y + 8, 32, 32);   // 16px x2 = tajam
        _assets.DrawSprite(g, stack.Item.SpriteKey, icon);

        // Jumlah (kalau lebih dari 1)
        if (stack.Quantity > 1)
            DrawTextWithShadow(g, stack.Quantity.ToString(), _small, slot.Right - 22, slot.Bottom - 17);

        // Bar kecil (hanya penyiram yang punya)
        float? meter = stack.Item.MeterRatio;
        if (meter != null)
        {
            using SolidBrush back = new(Color.FromArgb(60, 40, 30));
            using SolidBrush water = new(Color.DeepSkyBlue);
            g.FillRectangle(back, slot.X + 6, slot.Bottom - 8, slot.Width - 12, 4);
            g.FillRectangle(water, slot.X + 6, slot.Bottom - 8, (slot.Width - 12) * meter.Value, 4);
        }
    }

    // ---------- Akhir hari ----------

    /// <summary>Lapisan hitam di seluruh layar. darkness 0 = tidak ada, 1 = hitam penuh.</summary>
    public void DrawFade(Graphics g, float darkness)
    {
        if (darkness <= 0)
            return;
        using SolidBrush black = new(Color.FromArgb((int)(255 * darkness), 0, 0, 0));
        g.FillRectangle(black, 0, 0, GameConfig.ScreenWidth, GameConfig.ScreenHeight);
    }

    public void DrawDaySummary(Graphics g, DaySummary summary)
    {
        List<string> lines = new();
        lines.Add($"Hari selesai: {summary.EndedDate}");
        if (summary.Reason == SleepReason.PassedOut)
            lines.Add("Kamu pingsan karena kelelahan...");
        lines.Add("");

        lines.Add("Hasil pengiriman:");
        if (summary.SoldItems.Count == 0)
            lines.Add("   (tidak ada)");
        foreach (ShipmentLine line in summary.SoldItems)
            lines.Add($"   {line.ItemName} x{line.Quantity}  =  {line.Total} g");
        lines.Add("");

        lines.Add($"Total pendapatan : {summary.TotalEarned} g");
        lines.Add($"Uang sekarang    : {summary.GoldAfter} g");
        lines.Add($"Energi pulih     : {summary.EnergyRatio * 100:0}%");
        lines.Add("");
        lines.Add($"Selamat pagi! {summary.NewDate}");
        lines.Add("Tekan Enter / C untuk lanjut");

        int x = GameConfig.ScreenWidth / 2 - 180, y = 90;
        foreach (string text in lines)
        {
            g.DrawString(text, _normal, Brushes.White, x, y);
            y += 26;
        }
    }

    private void DrawTextWithShadow(Graphics g, string text, Font font, float x, float y)
    {
        g.DrawString(text, font, Brushes.Black, x + 1, y + 1);
        g.DrawString(text, font, Brushes.White, x, y);
    }
}
