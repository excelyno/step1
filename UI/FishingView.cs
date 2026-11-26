namespace FarmGame.UI;

/// <summary>
/// Gambar memancing (koordinat layar): senar + pelampung di air, tanda "!", dan panel minigame.
/// Hanya MENGGAMBAR; aturannya ada di FishingState dan ReelMinigame.
/// </summary>
public class FishingView
{
    private const int BarWidth = 420;
    private const int BarHeight = 28;

    private readonly Font _big = new("Segoe UI", 22f, FontStyle.Bold);
    private readonly Font _normal = new("Segoe UI", 11f, FontStyle.Bold);

    public void Draw(Graphics g, FishingState state, Player player, Camera camera)
    {
        DrawLineAndBobber(g, state, player, camera);

        if (state.CurrentPhase == FishingState.Phase.Bite)
            DrawExclamation(g, player, camera);

        if (state.Minigame != null)
            DrawMinigame(g, state.Minigame);
    }

    private void DrawLineAndBobber(Graphics g, FishingState state, Player player, Camera camera)
    {
        int size = GameConfig.TileSize;

        // Pelampung naik-turun pelan; saat ikan menggigit, tenggelam sedikit
        float bob = MathF.Sin(state.ElapsedSeconds * 3f) * 1f;
        if (state.CurrentPhase == FishingState.Phase.Bite)
            bob = 3f;
        Vector2 bobberWorld = new(state.WaterTile.X * size + size / 2f, state.WaterTile.Y * size + size / 2f + bob);
        Vector2 handWorld = new(player.Center.X, player.Position.Y - 10);   // kira-kira tangan pemain

        PointF bobber = camera.WorldToScreen(bobberWorld);
        PointF hand = camera.WorldToScreen(handWorld);

        using Pen line = new(Color.FromArgb(220, 240, 240, 240), 1.5f);
        g.DrawLine(line, hand, bobber);

        // Pelampung: setengah merah, setengah putih
        const float r = 6f;
        g.FillEllipse(Brushes.White, bobber.X - r, bobber.Y - r, r * 2, r * 2);
        g.FillPie(Brushes.Red, bobber.X - r, bobber.Y - r, r * 2, r * 2, 180, 180);
        g.DrawEllipse(Pens.Black, bobber.X - r, bobber.Y - r, r * 2, r * 2);
    }

    private void DrawExclamation(Graphics g, Player player, Camera camera)
    {
        PointF head = camera.WorldToScreen(new Vector2(player.Center.X, player.Position.Y - 30));
        var bubble = new RectangleF(head.X - 16, head.Y - 40, 32, 38);
        using SolidBrush yellow = new(Color.Gold);
        g.FillEllipse(yellow, bubble);
        g.DrawEllipse(Pens.Black, bubble);
        g.DrawString("!", _big, Brushes.Black, bubble.X + 7, bubble.Y - 1);
    }

    private void DrawMinigame(Graphics g, ReelMinigame game)
    {
        int x = (GameConfig.ScreenWidth - BarWidth) / 2;
        int y = 120;

        // Panel
        var panel = new Rectangle(x - 20, y - 50, BarWidth + 40, BarHeight + 110);
        using SolidBrush panelBrush = new(Color.FromArgb(225, 30, 55, 80));
        using Pen frame = new(Color.FromArgb(240, 200, 160), 3);
        g.FillRectangle(panelBrush, panel);
        g.DrawRectangle(frame, panel);
        g.DrawString("Tekan C saat jarum di zona hijau!", _normal, Brushes.White, x, y - 40);

        // Bar + zona hijau
        using SolidBrush barBrush = new(Color.FromArgb(70, 60, 50));
        using SolidBrush zoneBrush = new(Color.FromArgb(90, 200, 90));
        g.FillRectangle(barBrush, x, y, BarWidth, BarHeight);
        g.FillRectangle(zoneBrush, x + game.ZoneStart * BarWidth, y, game.ZoneWidth * BarWidth, BarHeight);

        // Jarum
        float needleX = x + game.NeedlePosition * BarWidth;
        g.FillRectangle(Brushes.White, needleX - 2, y - 6, 4, BarHeight + 12);

        // Kedip hijau/merah sebentar setelah menekan
        if (game.FlashTimer > 0)
        {
            int alpha = (int)(game.FlashTimer / 0.25f * 160);
            Color flash = game.LastPressWasHit ? Color.FromArgb(alpha, 120, 255, 120) : Color.FromArgb(alpha, 255, 80, 60);
            using Pen flashPen = new(flash, 4);
            g.DrawRectangle(flashPen, x - 3, y - 3, BarWidth + 6, BarHeight + 6);
        }

        // Kena (bulat hijau) dan meleset (silang merah)
        int iconY = y + BarHeight + 16;
        for (int i = 0; i < ReelMinigame.HitsToWin; i++)
        {
            Brush fill = i < game.Hits ? Brushes.LimeGreen : Brushes.DimGray;
            g.FillEllipse(fill, x + i * 26, iconY, 18, 18);
        }
        using Pen missPen = new(Color.FromArgb(240, 90, 70), 3);
        using Pen emptyPen = new(Color.FromArgb(90, 90, 90), 3);
        for (int i = 0; i < ReelMinigame.MissesToLose; i++)
        {
            float mx = x + BarWidth - (i + 1) * 26;
            Pen pen = i < game.Misses ? missPen : emptyPen;
            g.DrawLine(pen, mx, iconY, mx + 18, iconY + 18);
            g.DrawLine(pen, mx + 18, iconY, mx, iconY + 18);
        }
    }
}
