namespace FarmGame.UI;

/// <summary>
/// Menggambar layar minigame bercocok tanam: panel, papan permainan, fakta "Tahukah kamu?",
/// dan bintang hasil. Hanya MENGGAMBAR; aturan permainan ada di Farming/*LessonWidget.
/// </summary>
public class CropLessonView
{
    private const int PanelWidth = 620;
    private const int PanelHeight = 410;
    private static readonly int PanelX = (GameConfig.ScreenWidth - PanelWidth) / 2;
    private static readonly int PanelY = 84;
    private const int BoardSize = 240;

    private readonly IAssetProvider _assets;
    private readonly Font _title = new("Segoe UI", 15f, FontStyle.Bold);
    private readonly Font _heading = new("Segoe UI", 10f, FontStyle.Bold);
    private readonly Font _text = new("Segoe UI", 9f);
    private readonly Font _small = new("Segoe UI", 8.5f, FontStyle.Italic);
    private readonly Font _big = new("Segoe UI", 18f, FontStyle.Bold);

    public CropLessonView(IAssetProvider assets)
    {
        _assets = assets;
    }

    /// <summary>Papan permainan (kotak). Semua widget memakai ruang 0..1 di dalam kotak ini.</summary>
    public static Rectangle BoardRect => new(PanelX + 30, PanelY + 96, BoardSize, BoardSize);

    /// <summary>Posisi mouse -> koordinat papan 0..1 (boleh di luar 0..1 kalau mouse di luar papan).</summary>
    public static Vector2 Pointer(Point mouse)
    {
        Rectangle b = BoardRect;
        return new Vector2((mouse.X - b.X) / (float)b.Width, (mouse.Y - b.Y) / (float)b.Height);
    }

    public void Draw(Graphics g, CropLessonState state, Point mouse)
    {
        using (SolidBrush dim = new(Color.FromArgb(150, 0, 0, 0)))
            g.FillRectangle(dim, 0, 0, GameConfig.ScreenWidth, GameConfig.ScreenHeight);

        var panel = new Rectangle(PanelX, PanelY, PanelWidth, PanelHeight);
        UiPanel.Draw(g, panel);

        using SolidBrush ink = new(UiPanel.Ink);
        using SolidBrush faded = new(Color.FromArgb(150, 105, 75));
        using SolidBrush green = new(Color.FromArgb(60, 130, 50));
        using SolidBrush gold = new(Color.FromArgb(150, 105, 10));

        g.DrawString(state.Lesson.Title, _title, ink, PanelX + 24, PanelY + 12);
        g.DrawString("Minigame bercocok tanam", _small, faded, PanelX + 24, PanelY + 38);

        g.DrawString("Cara main", _heading, ink, PanelX + 24, PanelY + 62);
        g.DrawString(state.Lesson.Goal, _text, ink, new RectangleF(PanelX + 24, PanelY + 78, PanelWidth - 48, 34));

        DrawBoard(g, state);
        DrawSideInfo(g, state, ink, faded, green);
        DrawFooter(g, state, ink, faded, gold);

        if (state.Phase == CropLessonState.LessonPhase.Intro)
            DrawIntroVeil(g, ink);
        else if (state.Phase == CropLessonState.LessonPhase.Result)
            DrawResult(g, state, ink, gold);
    }

    // ---------- Papan ----------

    private void DrawBoard(Graphics g, CropLessonState state)
    {
        Rectangle board = BoardRect;
        using (SolidBrush boardBack = new(Color.FromArgb(60, 90, 62, 38)))
            g.FillRectangle(boardBack, board);

        switch (state.Widget)
        {
            case TimingLessonWidget timing:
                DrawTimingBoard(g, timing, board);
                break;
            case HoldLessonWidget hold:
                DrawHoldBoard(g, hold, board);
                break;
            case DragLessonWidget drag:
                DrawDragBoard(g, drag, board);
                break;
            case GridLessonWidget grid:
                DrawGridBoard(g, grid, state.Lesson, board);
                break;
        }
    }

    private void DrawTimingBoard(Graphics g, TimingLessonWidget widget, Rectangle board)
    {
        var bar = new Rectangle(board.X + 8, board.Y + 96, board.Width - 16, 30);
        using SolidBrush barBack = new(Color.FromArgb(80, 60, 45));
        using SolidBrush zone = new(Color.FromArgb(120, 90, 200, 90));
        g.FillRectangle(barBack, bar);
        g.FillRectangle(zone, bar.X + (int)(widget.ZoneStart * bar.Width), bar.Y, (int)(widget.ZoneWidth * bar.Width), bar.Height);

        float needleX = bar.X + widget.Position * bar.Width;
        g.FillRectangle(Brushes.White, needleX - 2, bar.Y - 7, 4, bar.Height + 14);

        if (widget.Flash > 0)
        {
            int alpha = (int)(widget.Flash / 0.3f * 170);
            using Pen flash = new(widget.LastSuccess ? Color.FromArgb(alpha, 120, 255, 120) : Color.FromArgb(alpha, 255, 80, 60), 4);
            g.DrawRectangle(flash, bar.X - 3, bar.Y - 3, bar.Width + 6, bar.Height + 6);
        }

        g.DrawString("Tekan saat penanda di zona hijau", _text, Brushes.White, board.X + 10, board.Y + 62);
        DrawPips(g, board, widget.Hits, widget.HitsNeeded, widget.Mistakes, widget.MaxMistakes);
    }

    private void DrawHoldBoard(Graphics g, HoldLessonWidget widget, Rectangle board)
    {
        var bar = new Rectangle(board.X + 8, board.Y + 96, board.Width - 16, 30);
        using SolidBrush barBack = new(Color.FromArgb(80, 60, 45));
        using SolidBrush fill = new(Color.FromArgb(230, 240, 170, 90));
        using SolidBrush zone = new(Color.FromArgb(90, 90, 200, 90));
        g.FillRectangle(barBack, bar);
        g.FillRectangle(zone, bar.X + (int)(widget.TargetMin * bar.Width), bar.Y,
                        (int)((widget.TargetMax - widget.TargetMin) * bar.Width), bar.Height);
        g.FillRectangle(fill, bar.X, bar.Y + 2, (int)(widget.Value * bar.Width), bar.Height - 4);

        using Pen marker = new(Color.White, 3);
        float x = bar.X + widget.Value * bar.Width;
        g.DrawLine(marker, x, bar.Y - 6, x, bar.Bottom + 6);

        if (widget.Flash > 0)
        {
            int alpha = (int)(widget.Flash / 0.3f * 170);
            using Pen flash = new(widget.LastSuccess ? Color.FromArgb(alpha, 120, 255, 120) : Color.FromArgb(alpha, 255, 80, 60), 4);
            g.DrawRectangle(flash, bar.X - 3, bar.Y - 3, bar.Width + 6, bar.Height + 6);
        }

        g.DrawString(widget.IsHolding ? "Tahan... lepas di zona hijau!" : "Tahan C atau klik kiri",
                     _text, Brushes.White, board.X + 10, board.Y + 62);
        DrawPips(g, board, widget.Rounds, widget.RoundsNeeded, widget.Mistakes, widget.MaxMistakes);
    }

    private void DrawDragBoard(Graphics g, DragLessonWidget widget, Rectangle board)
    {
        PointF center = new(board.X + board.Width / 2f, board.Y + board.Height / 2f);
        float radius = DragLessonWidget.RingRadius * board.Width;
        float band = widget.Band * board.Width;

        using (Pen ring = new(Color.FromArgb(90, 90, 170, 90), band * 2f))
            g.DrawEllipse(ring, center.X - radius, center.Y - radius, radius * 2, radius * 2);

        // Sudah digaruk: busur emas
        if (widget.Progress > 0.01f)
        {
            using Pen done = new(Color.FromArgb(235, 240, 190, 60), band * 1.2f);
            g.DrawArc(done, center.X - radius, center.Y - radius, radius * 2, radius * 2, -90, 360 * widget.Progress);
        }

        _assets.DrawSprite(g, "produce_potato", new RectangleF(center.X - 22, center.Y - 22, 44, 44));

        if (widget.Marker is { } marker)
        {
            float mx = center.X + marker.X * board.Width;
            float my = center.Y + marker.Y * board.Height;
            using SolidBrush hand = new(Color.FromArgb(240, 255, 245, 200));
            g.FillEllipse(hand, mx - 6, my - 6, 12, 12);
            g.DrawEllipse(Pens.SaddleBrown, mx - 6, my - 6, 12, 12);
        }

        g.DrawString("Tahan klik, putari tanah. Jangan sentuh umbinya!",
                     _text, Brushes.White, new RectangleF(board.X + 6, board.Bottom - 34, board.Width - 12, 30));
    }

    private void DrawGridBoard(Graphics g, GridLessonWidget widget, CropLesson lesson, Rectangle board)
    {
        for (int row = 0; row < widget.Rows; row++)
        {
            for (int col = 0; col < widget.Cols; col++)
            {
                Rectangle cell = CellRect(board, widget.Rows, widget.Cols, col, row);
                DrawGridCell(g, widget, lesson, cell, col, row);
            }
        }
    }

    private void DrawGridCell(Graphics g, GridLessonWidget widget, CropLesson lesson, Rectangle cell, int col, int row)
    {
        GridLessonWidget.Cell kind = widget.CellAt(col, row);
        bool done = widget.IsDone(col, row);

        using SolidBrush back = new(kind == GridLessonWidget.Cell.Empty
            ? Color.FromArgb(40, 90, 62, 38)
            : Color.FromArgb(245, 236, 214, 170));
        g.FillRectangle(back, cell);

        using Pen border = new(Color.FromArgb(150, 100, 60), 2);
        if (kind != GridLessonWidget.Cell.Empty)
            g.DrawRectangle(border, cell);

        // Sorotan: kursor keyboard (oranye) atau mouse (biru)
        bool keyboardHere = widget.CursorCol == col && widget.CursorRow == row;
        if (keyboardHere)
        {
            using Pen cursor = new(Color.OrangeRed, 2);
            g.DrawRectangle(cursor, cell.X + 1, cell.Y + 1, cell.Width - 2, cell.Height - 2);
        }

        if (kind == GridLessonWidget.Cell.Empty)
            return;

        var icon = new RectangleF(cell.X + 5, cell.Y + 5, cell.Width - 10, cell.Height - 10);

        if (lesson.IsEvent)
            DrawEventIcons(g, kind == GridLessonWidget.Cell.Good, icon);
        else if (kind == GridLessonWidget.Cell.Good)
            _assets.DrawSprite(g, $"crop_{lesson.CropId}_{(widget.IsOrderMode ? 3 : 4)}", icon);
        else
            _assets.DrawSprite(g, widget.IsOrderMode ? "lesson_leaf" : $"crop_{lesson.CropId}_2", icon);

        // Nomor urut untuk mode "order"
        if (widget.IsOrderMode && kind == GridLessonWidget.Cell.Good)
        {
            int number = widget.OrderAt(col, row);
            using SolidBrush badge = new(Color.FromArgb(235, 60, 90, 40));
            g.FillEllipse(badge, cell.X + 2, cell.Y + 2, 16, 16);
            g.DrawString(number.ToString(), _small, Brushes.White, cell.X + 6, cell.Y + 3);
        }

        if (done)
        {
            using SolidBrush veil = new(Color.FromArgb(170, 250, 240, 215));
            g.FillRectangle(veil, cell);
            using Pen check = new(Color.FromArgb(220, 60, 140, 60), 3);
            g.DrawLines(check, new[]
            {
                new PointF(cell.X + 8, cell.Y + cell.Height / 2f),
                new PointF(cell.X + cell.Width / 2f - 2, cell.Bottom - 9),
                new PointF(cell.Right - 8, cell.Y + 9),
            });
        }
    }

    /// <summary>Sel event hama: sprite hama (harus diklik) atau daun/kuncup (jangan diklik).</summary>
    private void DrawEventIcons(Graphics g, bool pest, RectangleF area)
    {
        if (pest)
            _assets.DrawSprite(g, "pest_aphid", area);
        else
            _assets.DrawSprite(g, "lesson_leaf", area);
    }

    private static Rectangle CellRect(Rectangle board, int rows, int cols, int col, int row)
    {
        const int gap = 6;
        int cellW = (board.Width - gap * (cols + 1)) / cols;
        int cellH = (board.Height - gap * (rows + 1)) / rows;
        int size = Math.Max(16, Math.Min(cellW, cellH));
        int totalW = size * cols + gap * (cols - 1);
        int totalH = size * rows + gap * (rows - 1);
        int x0 = board.X + (board.Width - totalW) / 2;
        int y0 = board.Y + (board.Height - totalH) / 2;
        return new Rectangle(x0 + col * (size + gap), y0 + row * (size + gap), size, size);
    }

    private void DrawPips(Graphics g, Rectangle board, int done, int needed, int mistakes, int maxMistakes)
    {
        int y = board.Bottom - 34;
        for (int i = 0; i < needed; i++)
        {
            Brush fill = i < done ? Brushes.LimeGreen : Brushes.DimGray;
            g.FillEllipse(fill, board.X + 8 + i * 24, y, 16, 16);
        }

        using Pen missPen = new(Color.FromArgb(235, 200, 70, 60), 3);
        using Pen emptyPen = new(Color.FromArgb(100, 90, 90, 90), 3);
        for (int i = 0; i < maxMistakes; i++)
        {
            float mx = board.Right - 20 - i * 24;
            Pen pen = i < mistakes ? missPen : emptyPen;
            g.DrawLine(pen, mx, y + 1, mx + 14, y + 15);
            g.DrawLine(pen, mx + 14, y + 1, mx, y + 15);
        }
    }

    // ---------- Keterangan kanan & footer ----------

    private void DrawSideInfo(Graphics g, CropLessonState state, Brush ink, Brush faded, Brush green)
    {
        int x = PanelX + 292;
        int y = PanelY + 96;
        int width = 296;

        g.DrawString("Tahukah kamu?", _heading, green, x, y);
        SizeF factSize = g.MeasureString(state.Lesson.Fact, _text, width);
        g.DrawString(state.Lesson.Fact, _text, ink, new RectangleF(x, y + 18, width, factSize.Height + 6));
        y += 18 + (int)factSize.Height + 10;

        g.DrawString("Tip", _heading, faded, x, y);
        SizeF tipSize = g.MeasureString(state.Lesson.Tip, _small, width);
        g.DrawString(state.Lesson.Tip, _small, faded, new RectangleF(x, y + 16, width, tipSize.Height + 6));
        y += 16 + (int)tipSize.Height + 14;

        g.DrawString($"Salah: {state.Widget.Mistakes}/{state.Widget.MaxMistakes}", _text, ink, x, y);
        y += 20;
        g.DrawString($"Bintang: {state.Widget.Stars}/3", _text, ink, x, y);
        y += 22;
        if (state.GradeBonus)
            g.DrawString("Tingkatmu: panen +1 buah", _heading, green, x, y);
    }

    private void DrawFooter(Graphics g, CropLessonState state, Brush ink, Brush faded, Brush gold)
    {
        string hint = state.Phase switch
        {
            CropLessonState.LessonPhase.Intro => "C / Enter = mulai    Spasi = sudah paham    X / Esc = batal",
            CropLessonState.LessonPhase.Result => "C / Enter = lanjut",
            _ => state.Widget switch
            {
                GridLessonWidget => "Klik sel, atau panah + C    X / Esc = batal",
                DragLessonWidget => "Tahan klik kiri lalu putari lingkaran    X / Esc = batal",
                _ => "Tekan C atau klik kiri    X / Esc = batal"
            }
        };

        SizeF size = g.MeasureString(hint, _small);
        g.DrawString(hint, _small, state.Phase == CropLessonState.LessonPhase.Result ? gold : faded,
                     PanelX + (PanelWidth - size.Width) / 2, PanelY + PanelHeight - 26);
    }

    private void DrawIntroVeil(Graphics g, Brush ink)
    {
        Rectangle board = BoardRect;
        using (SolidBrush veil = new(Color.FromArgb(190, 250, 240, 215)))
            g.FillRectangle(veil, board);

        const string text = "Siap?";
        SizeF size = g.MeasureString(text, _big);
        g.DrawString(text, _big, ink, board.X + (board.Width - size.Width) / 2, board.Y + board.Height / 2 - 34);

        using SolidBrush faded = new(Color.FromArgb(150, 105, 75));
        const string sub = "Tekan C untuk mulai";
        SizeF subSize = g.MeasureString(sub, _text);
        g.DrawString(sub, _text, faded, board.X + (board.Width - subSize.Width) / 2, board.Y + board.Height / 2 + 4);
    }

    private void DrawResult(Graphics g, CropLessonState state, Brush ink, Brush gold)
    {
        Rectangle board = BoardRect;
        using (SolidBrush veil = new(Color.FromArgb(210, 40, 30, 20)))
            g.FillRectangle(veil, board);

        int stars = state.Widget.Stars;
        string title = stars >= 3 ? "Sempurna!" : stars == 2 ? "Bagus!" : "Berhasil!";
        SizeF size = g.MeasureString(title, _big);
        g.DrawString(title, _big, Brushes.White, board.X + (board.Width - size.Width) / 2, board.Y + 54);

        // Bintang muncul satu-satu (bintang ke-i terlihat setelah i * 0,18 detik sejak layar hasil)
        for (int i = 0; i < 3; i++)
        {
            var center = new PointF(board.X + board.Width / 2f + (i - 1) * 46, board.Y + 130);
            bool shown = state.ResultSeconds > i * 0.18f;
            if (shown)
                DrawStarSprite(g, center, 20f, i < stars);
        }

        using SolidBrush soft = new(Color.FromArgb(235, 235, 225, 210));
        const string hint = "C / Enter = lanjut";
        SizeF hintSize = g.MeasureString(hint, _text);
        g.DrawString(hint, _text, soft, board.X + (board.Width - hintSize.Width) / 2, board.Bottom - 42);
    }

    /// <summary>Bintang hasil pakai sprite bintang (star_full/star_empty dari generate_assets.py).</summary>
    private void DrawStarSprite(Graphics g, PointF center, float radius, bool filled)
    {
        var dest = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        _assets.DrawSprite(g, filled ? "star_full" : "star_empty", dest);
    }
}
