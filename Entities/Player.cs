namespace FarmGame.Entities;

/// <summary>
/// Karakter yang dikendalikan pemain.
/// Player TIDAK membaca keyboard sendiri. Game memberinya "niat gerak" lewat SetMovementIntent,
/// sehingga nanti NPC bisa memakai cara gerak yang sama.
/// </summary>
public class Player : Character
{
    private Vector2 _moveIntent;   // arah gerak yang diinginkan (panjang maksimal 1)
    private bool _wantsToRun;

    protected override string SpritePrefix => "player";

    public float CurrentSpeed => _wantsToRun ? GameConfig.PlayerRunSpeed : GameConfig.PlayerWalkSpeed;

    public Player(string name) : base(name)
    {
    }

    public void SetMovementIntent(Vector2 direction, bool run)
    {
        // Jaga panjang vektor maksimal 1, supaya jalan diagonal tidak lebih cepat
        if (direction.Length() > 1f)
            direction = Vector2.Normalize(direction);

        _moveIntent = direction;
        _wantsToRun = run;
    }

    public override void Update(float deltaSeconds)
    {
        Move(_moveIntent, CurrentSpeed, deltaSeconds);
    }
}
