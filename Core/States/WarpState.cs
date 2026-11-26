namespace FarmGame.Core.States;

/// <summary>
/// Pindah lokasi dengan layar gelap sebentar:
///   1. FadeOut : layar gelap
///   2. (sekali) : pemain dipindah ke lokasi tujuan
///   3. FadeIn  : layar terang, kembali ke PlayingState
/// Jam game tetap berhenti selama perpindahan.
/// </summary>
public class WarpState : GameState
{
    private const float FadeSeconds = 0.25f;

    private readonly Warp _warp;
    private bool _arrived;
    private float _timer;

    public WarpState(Game game, Warp warp) : base(game)
    {
        _warp = warp;
    }

    public override void Enter()
    {
        Game.Time.Pause();
        Game.Player.SetMovementIntent(Vector2.Zero, false);
    }

    public override void Update(float deltaSeconds)
    {
        _timer += deltaSeconds;

        if (!_arrived && _timer >= FadeSeconds)
        {
            GameLocation from = Game.CurrentLocation;
            Game.ChangeLocation(Game.GetLocation(_warp.TargetLocation), _warp.TargetTile);
            if (from.IsOutdoors)
                Game.AnnounceLocation();   // MVP 5: spanduk nama tempat (tidak tiap kali keluar rumah)
            _arrived = true;
            _timer = 0;
        }
        else if (_arrived && _timer >= FadeSeconds)
        {
            Game.ChangeState(new PlayingState(Game));
            Game.PlaySound("door");
        }
    }

    public override void Draw(Graphics g)
    {
        Game.DrawWorldAndHud(g);

        float darkness = _arrived ? 1f - _timer / FadeSeconds : _timer / FadeSeconds;
        Game.Hud.DrawFade(g, Math.Clamp(darkness, 0f, 1f));
    }
}
