namespace FarmGame.Core.States;

/// <summary>Menu jeda (MVP 5): Esc saat bermain. Jam berhenti. Lanjut / Simpan / Keluar ke judul.</summary>
public class PauseState : GameState
{
    private readonly MenuList _menu = new MenuList().Add("Lanjut Main").Add("Simpan").Add("Simpan & ke Judul");
    private readonly Font _titleFont = new("Segoe UI", 22f, FontStyle.Bold);
    private readonly Font _smallFont = new("Segoe UI", 10f, FontStyle.Bold);

    public PauseState(Game game) : base(game)
    {
    }

    public override void Enter()
    {
        Game.Time.Pause();
        Game.Player.SetMovementIntent(Vector2.Zero, false);
    }

    public override void Update(float deltaSeconds)
    {
        if (Game.Input.WasPressed(Keys.Escape))
        {
            Game.ChangeState(new PlayingState(Game));
            return;
        }

        int chosen = _menu.Update(deltaSeconds, Game.Input);
        if (chosen == 0)
        {
            Game.ChangeState(new PlayingState(Game));
        }
        else if (chosen == 1)
        {
            if (Game.SaveGame())
                Game.Notify("Permainan disimpan.");
        }
        else if (chosen == 2)
        {
            if (Game.SaveGame())
                Game.ReturnToTitle();
        }
        Game.Toasts.Update(deltaSeconds);
    }

    public override void Draw(Graphics g)
    {
        Game.DrawWorldAndHud(g, showToolbar: false);
        using (SolidBrush dim = new(Color.FromArgb(140, 0, 0, 0)))
            g.FillRectangle(dim, 0, 0, GameConfig.ScreenWidth, GameConfig.ScreenHeight);

        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        const string title = "Jeda";
        SizeF size = g.MeasureString(title, _titleFont);
        g.DrawString(title, _titleFont, Brushes.White, (GameConfig.ScreenWidth - size.Width) / 2, 150);

        _menu.Draw(g, GameConfig.ScreenWidth / 2, 220);

        string info = $"{Game.Date}  -  {Game.Time.ClockText}  -  {Game.Player.Wallet.Gold}g";
        SizeF infoSize = g.MeasureString(info, _smallFont);
        g.DrawString(info, _smallFont, Brushes.Gainsboro, (GameConfig.ScreenWidth - infoSize.Width) / 2, 390);
        Game.Toasts.Draw(g);
    }
}
