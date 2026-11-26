namespace FarmGame.Core.States;

/// <summary>
/// Layar judul (MVP 5). Latarnya kebun sungguhan: kamera bergeser pelan di atas kebun pagi hari,
/// kupu-kupu dan burung tetap hidup. Pilihan: Lanjutkan (kalau ada simpanan), Main Baru, Keluar.
/// </summary>
public class TitleState : GameState
{
    private const float PanSpeed = 9f;   // piksel logis per detik

    private readonly MenuList _menu = new();
    private readonly bool _hasSave;
    private readonly Font _logoFont = new("Segoe UI Black", 54f, FontStyle.Bold);
    private readonly Font _taglineFont = new("Segoe UI", 13f, FontStyle.Italic | FontStyle.Bold);
    private readonly Font _smallFont = new("Segoe UI", 9f, FontStyle.Bold);
    private float _clock;
    private float _panX;

    public TitleState(Game game) : base(game)
    {
        _hasSave = SaveSystem.HasSave;
        _menu.Add("Lanjutkan", _hasSave).Add("Main Baru").Add("Keluar");
    }

    public override void Enter()
    {
        Game.Time.Pause();
        Game.Player.SetMovementIntent(Vector2.Zero, false);
    }

    public override void Update(float deltaSeconds)
    {
        _clock += deltaSeconds;

        // Kamera bergeser ke kanan lalu balik lagi (bolak-balik)
        int maxX = Game.CurrentLocation.Map.PixelWidth - GameConfig.ViewWidth;
        _panX = (_clock * PanSpeed) % (maxX * 2);
        float x = _panX < maxX ? _panX : maxX * 2 - _panX;
        Game.Camera.SetPosition(new Vector2(x, 4 * GameConfig.TileSize));
        Game.CurrentLocation.Update(deltaSeconds);

        int chosen = _menu.Update(deltaSeconds, Game.Input);
        if (chosen == 0)
        {
            if (!Game.ContinueGame())
                Game.Notify("File simpanan rusak atau tidak bisa dibaca.", false);
        }
        else if (chosen == 1)
        {
            Game.StartNewGame();
        }
        else if (chosen == 2)
        {
            Game.RequestExit();
        }
    }

    public override void Draw(Graphics g)
    {
        Game.DrawWorldOnly(g);

        // Lapisan gelap tipis supaya tulisan terbaca
        using (SolidBrush dim = new(Color.FromArgb(70, 10, 20, 30)))
            g.FillRectangle(dim, 0, 0, GameConfig.ScreenWidth, GameConfig.ScreenHeight);

        DrawLogo(g);
        _menu.Draw(g, GameConfig.ScreenWidth / 2, 280);

        string hint = _hasSave ? "W/S pilih  -  Enter pilih" : "Belum ada simpanan. Pilih Main Baru untuk mulai.";
        SizeF size = g.MeasureString(hint, _smallFont);
        g.DrawString(hint, _smallFont, Brushes.White, (GameConfig.ScreenWidth - size.Width) / 2, GameConfig.ScreenHeight - 34);
        Game.Toasts.Draw(g);
    }

    /// <summary>Judul "TANDUR" dengan bayangan tebal, naik-turun pelan, dan daun kecil di atas huruf.</summary>
    private void DrawLogo(Graphics g)
    {
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        const string title = "TANDUR";
        float bob = MathF.Sin(_clock * 1.6f) * 4f;
        SizeF size = g.MeasureString(title, _logoFont);
        float x = (GameConfig.ScreenWidth - size.Width) / 2;
        float y = 70 + bob;

        using SolidBrush outline = new(Color.FromArgb(70, 40, 20));
        for (int dx = -3; dx <= 3; dx += 3)
            for (int dy = -3; dy <= 6; dy += 3)
                g.DrawString(title, _logoFont, outline, x + dx, y + dy);
        using LinearGradientBrush fill = new(new RectangleF(x, y, size.Width, size.Height),
            Color.FromArgb(255, 236, 140), Color.FromArgb(240, 150, 50), LinearGradientMode.Vertical);
        g.DrawString(title, _logoFont, fill, x, y);

        // daun kecil di atas huruf pertama
        GraphicsState state = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform(x + 28, y + 12);
        g.RotateTransform(-30 + MathF.Sin(_clock * 2.5f) * 8f);
        using SolidBrush leaf = new(Color.FromArgb(120, 200, 80));
        g.FillEllipse(leaf, -6, -18, 12, 20);
        g.Restore(state);

        const string tagline = "Kehidupan di kebun Kakek Wiryo";
        SizeF tag = g.MeasureString(tagline, _taglineFont);
        using SolidBrush shadow = new(Color.FromArgb(160, 0, 0, 0));
        g.DrawString(tagline, _taglineFont, shadow, (GameConfig.ScreenWidth - tag.Width) / 2 + 2, y + size.Height - 6);
        g.DrawString(tagline, _taglineFont, Brushes.White, (GameConfig.ScreenWidth - tag.Width) / 2, y + size.Height - 8);
    }
}
