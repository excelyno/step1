namespace FarmGame.Core.States;

/// <summary>
/// Mode pergantian hari, urutannya:
///   1. FadeOut  : layar pelan-pelan gelap
///   2. (sekali)  : Game.EndDay() -> jual barang, ganti tanggal, tanaman tumbuh
///   3. Summary  : tampilkan ringkasan, tunggu Enter / C
///   4. FadeIn   : layar terang lagi, kembali ke PlayingState
/// </summary>
public class DayEndState : GameState
{
    private enum Phase { FadeOut, Summary, FadeIn }

    private const float FadeSeconds = 0.8f;
    private const float MinSummarySeconds = 0.5f;   // supaya ringkasan tidak langsung terlewat

    private readonly SleepReason _reason;
    private Phase _phase = Phase.FadeOut;
    private float _timer;
    private DaySummary? _summary;

    public DayEndState(Game game, SleepReason reason) : base(game)
    {
        _reason = reason;
    }

    public override void Enter()
    {
        Game.Time.Pause();
        Game.Player.SetMovementIntent(Vector2.Zero, false);   // pemain berhenti
    }

    public override void Update(float deltaSeconds)
    {
        _timer += deltaSeconds;

        if (_phase == Phase.FadeOut && _timer >= FadeSeconds)
        {
            _summary = Game.EndDay(_reason);   // dipanggil TEPAT sekali
            _phase = Phase.Summary;
            _timer = 0;
        }
        else if (_phase == Phase.Summary && _timer >= MinSummarySeconds && Game.Input.ConfirmPressed)
        {
            _phase = Phase.FadeIn;
            _timer = 0;
        }
        else if (_phase == Phase.FadeIn && _timer >= FadeSeconds)
        {
            Game.ChangeState(new PlayingState(Game));
        }
    }

    public override void Draw(Graphics g)
    {
        Game.DrawWorldAndHud(g);

        // Seberapa gelap layar: 0 = terang, 1 = hitam
        float darkness = 1f;
        if (_phase == Phase.FadeOut) darkness = _timer / FadeSeconds;
        if (_phase == Phase.FadeIn) darkness = 1f - _timer / FadeSeconds;
        Game.Hud.DrawFade(g, Math.Clamp(darkness, 0f, 1f));

        if (_phase == Phase.Summary && _summary != null)
            Game.Hud.DrawDaySummary(g, _summary);
    }
}
