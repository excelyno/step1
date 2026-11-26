namespace FarmGame.Core.States;

/// <summary>
/// Mode memancing (MVP 3). Urutannya:
///   1. Waiting : kail di air, tunggu 1,5 - 4 detik. Tarik terlalu cepat = ikan kabur.
///   2. Bite    : tanda "!" muncul. Ada 1 detik untuk menekan C.
///   3. Reeling : minigame ReelMinigame (jarum + zona hijau).
/// X / Esc kapan saja = berhenti. Jam berhenti selama memancing.
/// </summary>
public class FishingState : GameState
{
    public enum Phase { Waiting, Bite, Reeling }

    private const float BiteWindowSeconds = 1f;
    private static readonly Random Rng = new();

    private FishData? _fish;
    private float _waitSeconds;
    private float _timer;

    public Point WaterTile { get; }
    public Phase CurrentPhase { get; private set; } = Phase.Waiting;
    public ReelMinigame? Minigame { get; private set; }

    /// <summary>Total waktu sejak kail dilempar (untuk animasi pelampung).</summary>
    public float ElapsedSeconds { get; private set; }

    public FishingState(Game game, Point waterTile) : base(game)
    {
        WaterTile = waterTile;
    }

    public override void Enter()
    {
        Game.Time.Pause();
        Game.Player.SetMovementIntent(Vector2.Zero, false);
        _fish = Game.Fish.PickRandom(Game.Date.Season, Rng);
        _waitSeconds = 1.5f + (float)Rng.NextDouble() * 2.5f;
    }

    public override void Update(float deltaSeconds)
    {
        InputManager input = Game.Input;
        Game.Toasts.Update(deltaSeconds);
        _timer += deltaSeconds;
        ElapsedSeconds += deltaSeconds;

        if (input.MenuClosePressed)
        {
            Finish("Kamu berhenti memancing.", false);
            return;
        }

        if (_fish == null)
        {
            Finish("Sepertinya tidak ada ikan di musim ini.", false);
            return;
        }

        if (CurrentPhase == Phase.Waiting)
            UpdateWaiting(input);
        else if (CurrentPhase == Phase.Bite)
        {
            Game.PlaySound("select");
            UpdateBite(input);
        }
        else
            UpdateReeling(input, deltaSeconds, _fish);
    }

    private void UpdateWaiting(InputManager input)
    {
        if (input.UseToolPressed)
        {
            Finish("Terlalu cepat menarik! Ikannya kabur.", false);
            return;
        }

        if (_timer >= _waitSeconds)
        {
            CurrentPhase = Phase.Bite;
            _timer = 0;
        }
    }

    private void UpdateBite(InputManager input)
    {
        if (input.UseToolPressed)
        {
            Minigame = new ReelMinigame(_fish!.Difficulty, Rng);
            CurrentPhase = Phase.Reeling;
            return;
        }

        if (_timer >= BiteWindowSeconds)
            Finish("Ikannya lepas... Tekan C lebih cepat saat ada tanda !", false);
    }

    private void UpdateReeling(InputManager input, float deltaSeconds, FishData fish)
    {
        Minigame!.Update(deltaSeconds);
        if (input.UseToolPressed)
            Minigame.Press();

        if (Minigame.IsLost)
        {
            Finish("Senarnya putus...", false);
        }
        else if (Minigame.IsWon)
        {
            Item caught = Game.Items.Create(fish.Id);
            if (Game.Player.Inventory.TryAdd(caught) > 0)
                Finish($"Dapat {caught.Name}, tapi tas penuh. Ikannya dilepas.", false);
            else
                Finish($"Dapat {caught.Name}!", true);
        }
    }

    /// <summary>Tampilkan pesan lalu kembali bermain.</summary>
    private void Finish(string message, bool success)
    {
        Game.Toasts.Push(message, success);
        Game.PlaySound(success ? "good" : "wrong");
        Game.ChangeState(new PlayingState(Game));
    }

    public override void Draw(Graphics g)
    {
        Game.DrawWorldAndHud(g);
        Game.FishingView.Draw(g, this, Game.Player, Game.Camera);
        Game.Toasts.Draw(g);
    }
}
