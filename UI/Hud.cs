namespace FarmGame.UI;

/// <summary>
/// Tampilan di atas dunia (koordinat layar, tidak ikut kamera):
/// jam + tanggal + uang (kanan atas), energi (kanan bawah), toolbar (bawah tengah),
/// layar gelap dan ringkasan akhir hari.
/// </summary>
public class Hud
{
    public const int SlotSize = 48;

    /// <summary>Kotak satu slot toolbar di layar (dipakai PlayingState untuk klik mouse).</summary>
    public static Rectangle ToolbarSlotRect(int index, int capacity)
    {
        int totalWidth = VisibleSlots(capacity) * SlotSize;
        int x0 = (GameConfig.ScreenWidth - totalWidth) / 2;
        int y = GameConfig.ScreenHeight - SlotSize - 14;
        return new Rectangle(x0 + index * SlotSize, y, SlotSize, SlotSize);
    }

    /// <summary>Seluruh bingkai toolbar (termasuk tepi). Klik di sini tidak boleh memakai alat.</summary>
    public static Rectangle ToolbarAreaRect(int capacity)
    {
        int totalWidth = VisibleSlots(capacity) * SlotSize;
        int x0 = (GameConfig.ScreenWidth - totalWidth) / 2;
        int y = GameConfig.ScreenHeight - SlotSize - 14;
        return new Rectangle(x0 - 10, y - 10, totalWidth + 20, SlotSize + 20);
    }

    private readonly IAssetProvider _assets;
    private readonly Font _small = new("Segoe UI", 9f, FontStyle.Bold);
    private readonly Font _normal = new("Segoe UI", 11f, FontStyle.Bold);
    private readonly Font _big = new("Segoe UI", 20f, FontStyle.Bold);

    // Label tombol di bawah tiap slot
    private static readonly string[] SlotLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=" };

    /// <summary>
    /// Jumlah slot yang DIGAMBAR di toolbar. Toolbar dirancang untuk 12 slot di tengah bawah;
    /// kalau tas diperbesar lewat upgrade, sisanya tetap ada dan dilihat lewat menu tas (tombol B).
    /// Semua tempat lain (hover, klik, sorot) WAJIB memakai nilai ini supaya tidak meleset —
    /// dulu crash IndexOutOfRange karena label hanya 12 tapi loop mengikuti Capacity.
    /// </summary>
    public const int VisibleToolbarSlots = 12;

    /// <summary>Jumlah slot toolbar yang tampil: Capacity di-clamp ke jumlah slot fisik toolbar.</summary>
    public static int VisibleSlots(int capacity) => Math.Min(capacity, VisibleToolbarSlots);

    /// <summary>Label tombol slot ke-i (null = tanpa label).</summary>
    public static string? SlotLabel(int index) =>
        index >= 0 && index < SlotLabels.Length ? SlotLabels[index] : null;

    // ---------- MVP 5: widget beranimasi ----------
    private const float BannerSeconds = 3f;
    private const float SavedSeconds = 2.2f;

    private float _shownGold = -1;        // angka uang yang ditampilkan (menggelinding ke angka sebenarnya)
    private float _goldFlash;             // kilat hijau/merah saat uang berubah
    private bool _goldWentUp;
    private string? _bannerTitle;
    private string? _bannerSubtitle;
    private float _bannerTimer;
    private float _savedTimer;
    private float _clock;                 // waktu berjalan untuk animasi kecil

    public Hud(IAssetProvider assets)
    {
        _assets = assets;
    }

    /// <summary>Spanduk besar di atas tengah: "Sel, 2 Musim Semi" atau nama lokasi.</summary>
    public void ShowBanner(string title, string? subtitle = null)
    {
        _bannerTitle = title;
        _bannerSubtitle = subtitle;
        _bannerTimer = BannerSeconds;
    }

    /// <summary>Tanda kecil "Tersimpan" di kiri bawah.</summary>
    public void ShowSaved() => _savedTimer = SavedSeconds;

    /// <summary>Dipanggil tiap frame (semua mode), supaya animasi tetap jalan.</summary>
    public void Update(float deltaSeconds, Player player)
    {
        _clock += deltaSeconds;
        _bannerTimer = Math.Max(0, _bannerTimer - deltaSeconds);
        _savedTimer = Math.Max(0, _savedTimer - deltaSeconds);
        _goldFlash = Math.Max(0, _goldFlash - deltaSeconds);

        int gold = player.Wallet.Gold;
        if (_shownGold < 0)
            _shownGold = gold;
        if (Math.Abs(gold - _shownGold) >= 0.5f)
        {
            if (_goldFlash <= 0)
                _goldWentUp = gold > _shownGold;
            _goldFlash = 0.4f;
            // makin jauh selisihnya, makin cepat menggelinding (selesai kira-kira 0,6 detik)
            float step = Math.Max(30f, Math.Abs(gold - _shownGold) * 5f) * deltaSeconds;
            _shownGold = gold > _shownGold ? Math.Min(gold, _shownGold + step) : Math.Max(gold, _shownGold - step);
        }
    }

    /// <summary>Langsung tampilkan angka sebenarnya (setelah memuat simpanan).</summary>
    public void ResetGold() => _shownGold = -1;

    public void Draw(Graphics g, Player player, TimeManager time, bool showToolbar = true, int hoverSlot = -1)
    {
        DrawClock(g, player, time);
        DrawEnergy(g, player.Stamina);
        if (showToolbar)
            DrawToolbar(g, player.Inventory, hoverSlot);
        DrawBanner(g);
        DrawSavedIndicator(g);
    }

    private void DrawBanner(Graphics g)
    {
        if (_bannerTimer <= 0 || _bannerTitle == null)
            return;

        // Masuk dari atas (0,35 detik), diam, lalu memudar di 0,6 detik terakhir
        float age = BannerSeconds - _bannerTimer;
        float slide = Math.Min(1f, age / 0.35f);
        slide = 1f - (1f - slide) * (1f - slide);
        float alpha = Math.Min(1f, _bannerTimer / 0.6f);

        SizeF titleSize = g.MeasureString(_bannerTitle, _big);
        int width = (int)Math.Max(260, titleSize.Width + 60);
        int height = _bannerSubtitle == null ? 52 : 70;
        int x = (GameConfig.ScreenWidth - width) / 2;
        int y = (int)(-height + (height + 18) * slide);

        // MVP 6: gambar lapisan banner SEKALI (cache), tiap frame hanya ditempel dengan alfa yang berubah
        Bitmap layer = BannerLayer(width, height, titleSize.Width);
        using System.Drawing.Imaging.ImageAttributes fade = new();
        fade.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = alpha });
        g.DrawImage(layer, new Rectangle(x, y, width, height), 0, 0, width, height, GraphicsUnit.Pixel, fade);
    }

    // MVP 6: cache bitmap spanduk. Kuncinya (teks, subteks, lebar); berubah hanya saat spanduk baru muncul.
    private Bitmap? _bannerLayer;
    private (string Title, string? Sub, int Width, int Height)? _bannerKey;

    private Bitmap BannerLayer(int width, int height, float titleWidth)
    {
        var key = (_bannerTitle ?? "", _bannerSubtitle, width, height);
        if (_bannerLayer != null && _bannerKey == key)
            return _bannerLayer;

        _bannerLayer?.Dispose();
        _bannerKey = key;
        _bannerLayer = new Bitmap(width, height);
        using Graphics lg = Graphics.FromImage(_bannerLayer);
        UiPanel.Draw(lg, new Rectangle(0, 0, width, height), paper: true);
        using SolidBrush ink = new(Color.FromArgb(70, 45, 25));
        lg.DrawString(_bannerTitle, _big, ink, (width - titleWidth) / 2, 8);
        if (_bannerSubtitle != null)
        {
            SizeF sub = lg.MeasureString(_bannerSubtitle, _small);
            using SolidBrush soft = new(Color.FromArgb(120, 85, 50));
            lg.DrawString(_bannerSubtitle, _small, soft, (width - sub.Width) / 2, 44);
        }
        return _bannerLayer;
    }

    private void DrawSavedIndicator(Graphics g)
    {
        if (_savedTimer <= 0)
            return;
        int alpha = (int)(255 * Math.Min(1f, _savedTimer / 0.5f));
        var box = new Rectangle(12, GameConfig.ScreenHeight - 50, 132, 34);
        using SolidBrush back = new(Color.FromArgb(alpha * 200 / 255, 40, 28, 20));
        g.FillRectangle(back, box);

        // Disket kecil yang berputar pelan
        GraphicsState state = g.Save();
        g.TranslateTransform(box.X + 18, box.Y + 17);
        g.RotateTransform(MathF.Sin(_clock * 6f) * 10f);
        using SolidBrush disk = new(Color.FromArgb(alpha, 90, 150, 220));
        using SolidBrush label = new(Color.FromArgb(alpha, 240, 240, 240));
        g.FillRectangle(disk, -9, -9, 18, 18);
        g.FillRectangle(label, -5, -9, 10, 7);
        g.Restore(state);

        using SolidBrush text = new(Color.FromArgb(alpha, 255, 255, 255));
        g.DrawString("Tersimpan", _normal, text, box.X + 34, box.Y + 7);
    }

    // ---------- MVP 4: pelacak quest (kiri atas) ----------

    public void DrawQuestTracker(Graphics g, IEnumerable<(string Title, string Progress)> quests)
    {
        var list = quests.Take(3).ToList();
        if (list.Count == 0)
            return;

        var box = new Rectangle(8, 8, 290, 30 + list.Count * 36);
        UiPanel.Draw(g, box, paper: false);
        g.DrawString("Quest  (J)", _small, Brushes.Gold, box.X + 14, box.Y + 9);

        int y = box.Y + 26;
        foreach (var (title, progress) in list)
        {
            _assets.DrawSprite(g, "marker_new", new RectangleF(box.X + 14, y + 2, 8, 11));
            g.DrawString(title, _small, Brushes.White, box.X + 28, y);
            using SolidBrush soft = new(Color.FromArgb(230, 210, 170));
            g.DrawString(progress, _small, soft, box.X + 28, y + 15);
            y += 36;
        }
    }

    // ---------- Jam, tanggal, uang ----------

    private void DrawClock(Graphics g, Player player, TimeManager time)
    {
        var box = new Rectangle(GameConfig.ScreenWidth - 186, 8, 178, 90);
        using SolidBrush gold = new(Color.Gold);
        UiPanel.Draw(g, box, paper: false);   // MVP 4: bingkai kayu

        g.DrawString(time.Date.ToString(), _normal, Brushes.White, box.X + 14, box.Y + 9);
        g.DrawString(time.ClockText, _big, Brushes.White, box.X + 12, box.Y + 30);
        // MVP 5: angka menggelinding + berkilat hijau (naik) / merah (turun)
        int shown = _shownGold < 0 ? player.Wallet.Gold : (int)MathF.Round(_shownGold);
        Brush goldBrush = gold;
        using SolidBrush flash = new(_goldWentUp ? Color.LightGreen : Color.Salmon);
        if (_goldFlash > 0)
            goldBrush = flash;
        g.DrawString($"{shown} g", _normal, goldBrush, box.X + 104, box.Y + 42);
    }

    // ---------- Energi ----------

    private void DrawEnergy(Graphics g, Stamina stamina)
    {
        var frame = new Rectangle(GameConfig.ScreenWidth - 38, GameConfig.ScreenHeight - 176, 30, 168);
        UiPanel.Draw(g, frame, paper: false);
        var box = new Rectangle(frame.X + 4, frame.Y + 4, 22, 160);

        // Warna: hijau > 50%, kuning > 20%, merah
        Color color = Color.FromArgb(90, 200, 90);
        if (stamina.Ratio <= 0.5f) color = Color.Gold;
        if (stamina.Ratio <= 0.2f) color = Color.FromArgb(220, 60, 50);

        int fill = (int)((box.Height - 6) * stamina.Ratio);
        using SolidBrush bar = new(color);
        g.FillRectangle(bar, box.X + 3, box.Bottom - 3 - fill, box.Width - 6, fill);   // terisi dari bawah

        g.DrawString("E", _small, Brushes.White, box.X + 5, frame.Y - 18);
    }

    // ---------- Toolbar ----------

    private void DrawToolbar(Graphics g, Inventory inventory, int hoverSlot)
    {
        // Toolbar fisik hanya 12 slot walau tas lebih besar (sisanya lewat tombol B).
        int shown = VisibleSlots(inventory.Capacity);
        int totalWidth = shown * SlotSize;
        int x0 = (GameConfig.ScreenWidth - totalWidth) / 2;
        int y = GameConfig.ScreenHeight - SlotSize - 14;

        UiPanel.Draw(g, new Rectangle(x0 - 10, y - 10, totalWidth + 20, SlotSize + 20), paper: false);   // MVP 4
        using SolidBrush slotBrush = new(Color.FromArgb(240, 236, 214, 170));
        using Pen border = new(Color.FromArgb(150, 100, 60), 2);
        using Pen selected = new(Color.OrangeRed, 3);

        for (int i = 0; i < shown; i++)
        {
            var slot = new Rectangle(x0 + i * SlotSize, y, SlotSize, SlotSize);
            g.FillRectangle(slotBrush, slot);
            g.DrawRectangle(border, slot);

            ItemStack? stack = inventory.GetSlot(i);
            if (stack != null)
                DrawStack(g, stack, slot);

            if (i == hoverSlot)
            {
                var inner = new Rectangle(slot.X + 1, slot.Y + 1, slot.Width - 2, slot.Height - 2);
                using Pen hover = new(Color.FromArgb(255, 110, 180, 240), 2);
                g.DrawRectangle(hover, inner);
            }

            string? label = SlotLabel(i);
            if (label != null)
                g.DrawString(label, _small, Brushes.DimGray, slot.X + 2, slot.Y);
        }

        // Sorotan slot terpilih (hanya kalau slot itu memang tampil di toolbar)
        if (inventory.SelectedIndex < shown)
        {
            var chosen = new Rectangle(x0 + inventory.SelectedIndex * SlotSize, y, SlotSize, SlotSize);
            g.DrawRectangle(selected, chosen);
        }

        // Indikasi tas lebih besar dari toolbar: "...+N di tas (B)"
        if (inventory.Capacity > shown)
        {
            string more = $"+{inventory.Capacity - shown} lagi di tas (B)";
            DrawTextWithShadow(g, more, _small, x0 + totalWidth + 14, y + SlotSize / 2 - 8);
        }

        // Nama item terpilih di atas toolbar (alat: + sisa daya tahan)
        if (inventory.SelectedItem != null)
        {
            string label = inventory.SelectedItem.Name;
            if (inventory.SelectedItem is Tool tool)
                label += tool.IsBroken ? "  (HANCUR)" : $"  {tool.Durability}/{tool.MaxDurability}";
            DrawTextWithShadow(g, label, _normal, x0, y - 38);
        }
    }

    private void DrawStack(Graphics g, ItemStack stack, Rectangle slot)
    {
        // Ikon (polimorfisme: tiap item punya SpriteKey sendiri)
        var icon = new RectangleF(slot.X + 8, slot.Y + 8, 32, 32);   // 16px x2 = tajam
        _assets.DrawSprite(g, stack.Item.SpriteKey, icon);

        // Jumlah (kalau lebih dari 1)
        if (stack.Quantity > 1)
            DrawTextWithShadow(g, stack.Quantity.ToString(), _small, slot.Right - 22, slot.Bottom - 17);

        // Bar kecil: penyiram = sisa air (biru), alat lain = daya tahan (hijau/kuning/merah)
        float? meter = stack.Item.MeterRatio;
        if (meter != null)
        {
            using SolidBrush back = new(Color.FromArgb(60, 40, 30));
            using SolidBrush fill = new(stack.Item.MeterColor);
            g.FillRectangle(back, slot.X + 6, slot.Bottom - 8, slot.Width - 12, 4);
            g.FillRectangle(fill, slot.X + 6, slot.Bottom - 8, (slot.Width - 12) * meter.Value, 4);
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

        // Kotak 36 slot bisa berisi banyak jenis barang: batasi baris supaya tidak keluar layar
        const int MaxShipmentLines = 8;
        lines.Add("Hasil pengiriman:");
        if (summary.SoldItems.Count == 0)
            lines.Add("   (tidak ada)");
        foreach (ShipmentLine line in summary.SoldItems.Take(MaxShipmentLines))
            lines.Add($"   {line.ItemName} x{line.Quantity}  =  {line.Total} g");
        if (summary.SoldItems.Count > MaxShipmentLines)
            lines.Add($"   ... dan {summary.SoldItems.Count - MaxShipmentLines} jenis lainnya");
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
