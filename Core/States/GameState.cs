namespace FarmGame.Core.States;

/// <summary>
/// Kelas induk "mode" game (STATE PATTERN). Game hanya memanggil Update/Draw milik state aktif,
/// tanpa if-else bertumpuk. Sekarang ada 2 mode: bermain dan pergantian hari.
/// </summary>
public abstract class GameState
{
    protected Game Game { get; }

    protected GameState(Game game)
    {
        Game = game;
    }

    /// <summary>Dipanggil sekali saat state mulai aktif.</summary>
    public virtual void Enter() { }

    /// <summary>Dipanggil sekali saat state diganti.</summary>
    public virtual void Exit() { }

    public abstract void Update(float deltaSeconds);

    public abstract void Draw(Graphics g);
}
