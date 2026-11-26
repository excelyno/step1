namespace FarmGame.UI;

/// <summary>
/// Jurnal (MVP 4, tombol J): kiri = daftar quest aktif dengan cerita & kemajuan,
/// kanan = warga desa dengan hati pertemanan. Hanya MENGGAMBAR.
/// Versi baru: halaman Buku Tani (Tab) berisi bintang minigame bercocok tanam.
/// </summary>
public class JournalMenu
{
    private const int BookRowHeight = 44;

    private readonly IAssetProvider _assets;
    private readonly Font _title = new("Segoe UI", 16f, FontStyle.Bold);
    private readonly Font _heading = new("Segoe UI", 11f, FontStyle.Bold);
    private readonly Font _text = new("Segoe UI", 9.5f);
    private readonly Font _small = new("Segoe UI", 8.5f, FontStyle.Italic);

    public JournalMenu(IAssetProvider assets)
    {
        _assets = assets;
    }

    /// <summary>Kotak satu baris Buku Tani di layar (dipakai JournalState untuk klik mouse).</summary>
    public static Rectangle BookRowRect(int index)
    {
        var panel = new Rectangle(70, 40, GameConfig.ScreenWidth - 140, GameConfig.ScreenHeight - 80);
        return new Rectangle(panel.X + 24, panel.Y + 92 + index * BookRowHeight, panel.Width - 48, BookRowHeight - 6);
    }

    public void Draw(Graphics g, Game game, bool showBook = false)
    {
        if (showBook)
        {
            DrawBook(g, game);
            return;
        }

        var panel = new Rectangle(70, 40, GameConfig.ScreenWidth - 140, GameConfig.ScreenHeight - 80);
        UiPanel.Draw(g, panel);
        using SolidBrush ink = new(UiPanel.Ink);
        using SolidBrush faded = new(Color.FromArgb(140, 105, 75));

        g.DrawString("Jurnal", _title, ink, panel.X + 24, panel.Y + 14);
        g.DrawString($"Cerita: {game.Quests.CompletedCount}/{game.Quests.TotalCount} selesai", _small, faded, panel.X + 112, panel.Y + 22);
        g.DrawString("Tab = Buku Tani    J / Esc = tutup", _small, faded, panel.Right - 210, panel.Y + 22);

        int split = panel.X + (int)(panel.Width * 0.58f);
        using (Pen line = new(Color.FromArgb(200, 170, 120), 2))
            g.DrawLine(line, split, panel.Y + 56, split, panel.Bottom - 22);

        DrawQuests(g, game, new Rectangle(panel.X + 24, panel.Y + 56, split - panel.X - 40, panel.Height - 76), ink, faded);
        DrawFriends(g, game, new Rectangle(split + 18, panel.Y + 56, panel.Right - split - 40, panel.Height - 76), ink, faded);
    }

    /// <summary>Halaman Buku Tani: daftar minigame + bintang + tingkat petani.</summary>
    private void DrawBook(Graphics g, Game game)
    {
        var panel = new Rectangle(70, 40, GameConfig.ScreenWidth - 140, GameConfig.ScreenHeight - 80);
        UiPanel.Draw(g, panel);
        using SolidBrush ink = new(UiPanel.Ink);
        using SolidBrush faded = new(Color.FromArgb(140, 105, 75));
        using SolidBrush gold = new(Color.FromArgb(150, 100, 10));

        g.DrawString("Buku Tani", _title, ink, panel.X + 24, panel.Y + 14);
        g.DrawString("Semua yang kamu pelajari tentang bercocok tanam", _small, faded, panel.X + 120, panel.Y + 22);
        g.DrawString("Tab = jurnal    J / Esc = tutup", _small, faded, panel.Right - 210, panel.Y + 22);

        IReadOnlyList<CropLesson> lessons = game.LessonData.All;
        for (int i = 0; i < lessons.Count; i++)
        {
            CropLesson lesson = lessons[i];
            Rectangle row = BookRowRect(i);
            int stars = game.Lessons.StarsFor(lesson.Id);
            bool seen = game.Lessons.HasSeen(lesson.Id);

            using (SolidBrush rowBack = new(Color.FromArgb(60, 120, 90, 50)))
                g.FillRectangle(rowBack, row);

            // Tombol angka di kiri
            g.DrawString((i + 1).ToString(), _heading, faded, row.X + 10, row.Y + row.Height / 2 - 8);

            string label = lesson.IsEvent ? lesson.Title + " (setiap ada hama)" : lesson.Title;
            g.DrawString(label, _heading, ink, row.X + 30, row.Y + 6);
            g.DrawString(seen ? lesson.Fact : "Belum dicoba. " + lesson.Fact, _small, faded,
                         new RectangleF(row.X + 30, row.Y + 22, row.Width - 160, 20));

            for (int s = 0; s < 3; s++)
            {
                var center = new PointF(row.Right - 70 + s * 22, row.Y + row.Height / 2f);
                var dest = new RectangleF(center.X - 8, center.Y - 8, 16, 16);
                _assets.DrawSprite(g, s < stars ? "star_full" : "star_empty", dest);
            }
        }

        g.DrawString($"Tingkat: {game.Lessons.GradeName}  ({game.Lessons.TotalStars} bintang)",
                     _title, gold, panel.X + 24, panel.Bottom - 60);
        g.DrawString("1..5 atau klik baris = coba lagi (latihan, kebun tidak berubah)",
                     _small, faded, panel.X + 24, panel.Bottom - 32);
    }

    private void DrawQuests(Graphics g, Game game, Rectangle area, Brush ink, Brush faded)
    {
        g.DrawString("Quest aktif", _heading, ink, area.X, area.Y);
        float y = area.Y + 26;

        if (game.Quests.Active.Count == 0)
        {
            g.DrawString("Tidak ada. Cari warga dengan tanda \"!\" di atas kepalanya.", _text, faded,
                         new RectangleF(area.X, y, area.Width, 40));
            return;
        }

        foreach (QuestData quest in game.Quests.Active)
        {
            bool done = game.Quests.IsObjectiveMet(quest, game.Player.Inventory);
            _assets.DrawSprite(g, done ? "marker_turnin" : "marker_new", new RectangleF(area.X, y + 1, 10, 14));
            g.DrawString(quest.Title, _heading, ink, area.X + 16, y);
            y += 20;

            var descArea = new RectangleF(area.X + 16, y, area.Width - 16, 60);
            SizeF size = g.MeasureString(quest.Description, _text, (int)descArea.Width);
            g.DrawString(quest.Description, _text, ink, descArea);
            y += size.Height + 2;

            string progress = game.Quests.ProgressText(quest, game.Player.Inventory, game.Npcs, game.Items);
            using SolidBrush progressBrush = new(done ? Color.FromArgb(60, 130, 50) : Color.FromArgb(150, 90, 40));
            g.DrawString((done ? "Siap! " : "") + progress, _text, progressBrush, area.X + 16, y);
            y += 28;

            if (y > area.Bottom - 40)
                break;
        }
    }

    private void DrawFriends(Graphics g, Game game, Rectangle area, Brush ink, Brush faded)
    {
        g.DrawString("Warga Desa Tandur", _heading, ink, area.X, area.Y);
        float y = area.Y + 28;

        foreach (NpcData npc in game.Npcs.All)
        {
            // Wajah warga (sprite badan bagian atas)
            var portraitBox = new RectangleF(area.X, y, 32, 32);
            using (SolidBrush back = new(Color.FromArgb(232, 212, 170)))
                g.FillRectangle(back, portraitBox);
            GraphicsState state = g.Save();
            g.SetClip(portraitBox);
            _assets.DrawSprite(g, $"npc_{npc.Id}_down_0", new RectangleF(area.X, y + 2, 32, 64));
            g.Restore(state);

            g.DrawString(npc.Name, _heading, ink, area.X + 40, y - 2);
            g.DrawString(npc.Role, _small, faded, area.X + 40, y + 16);

            int hearts = game.Player.Relationships.HeartsWith(npc.Id);
            for (int i = 0; i < Relationships.MaxHearts; i++)
                _assets.DrawSprite(g, i < hearts ? "heart_full" : "heart_empty", new RectangleF(area.X + 40 + i * 15, y + 34, 13, 13));

            bool talked = game.Player.Relationships.HasTalkedToday(npc.Id);
            g.DrawString(talked ? "Sudah disapa hari ini" : "Belum disapa hari ini", _small, faded, area.X + 40, y + 48);
            y += 74;
        }

        g.DrawString("Tip: pegang hasil panen/ikan lalu tekan X di depan warga untuk memberi hadiah.", _small, faded,
                     new RectangleF(area.X, area.Bottom - 34, area.Width, 34));
    }
}
