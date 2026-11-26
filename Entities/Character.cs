namespace FarmGame.Entities;

/// <summary>
/// Kelas induk makhluk yang bisa bergerak dan punya arah hadap.
/// Sekarang dipakai Player. Nanti NPC, monster, dan hewan juga turunan dari sini,
/// jadi logika gerak + tabrakan cukup ditulis sekali di kelas ini.
/// </summary>
public abstract class Character : GameObject
{
    public string Name { get; }
    public Direction Facing { get; protected set; } = Direction.Down;
    public bool IsMoving { get; private set; }

    // Animasi jalan: frame 0 = diam, frame 1..4 = siklus langkah
    private const int WalkFrames = 4;
    private const float WalkFramesPerSecond = 8f;
    private float _walkTimer;

    public int AnimationFrame => IsMoving ? 1 + (int)(_walkTimer * WalkFramesPerSecond) % WalkFrames : 0;

    /// <summary>Awalan nama sprite, contoh "player" menjadi "player_down".</summary>
    protected abstract string SpritePrefix { get; }

    // Hitbox kecil di kaki (12x8). Gambar 16x32 berdiri di atasnya.
    public override SizeF HitboxSize => new(12f, 8f);

    /// <summary>MVP 5: makhluk saling menabrak (pemain tidak bisa menembus warga, dan sebaliknya).</summary>
    public override bool IsSolid => true;

    // MVP 5: "corner correction". Kalau menabrak sudut dan hanya meleset beberapa piksel,
    // karakter digeser sedikit ke samping supaya meluncur masuk celah (trik klasik game top-down).
    private const float CornerNudgePixels = 6f;
    public override RectangleF SpriteBounds => new(Position.X - 2f, Position.Y - 24f, 16f, 32f);

    protected Character(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Menggerakkan karakter sambil mengecek tabrakan.
    /// Sumbu X dan Y dicek terpisah supaya karakter bisa "meluncur" di sepanjang dinding.
    /// </summary>
    protected void Move(Vector2 direction, float speed, float deltaSeconds)
    {
        IsMoving = direction != Vector2.Zero;
        if (!IsMoving || Location == null)
        {
            _walkTimer = 0;
            return;
        }

        // Lari = kaki bergerak lebih cepat
        _walkTimer += deltaSeconds * speed / GameConfig.PlayerWalkSpeed;

        // Arah hadap mengikuti sumbu yang paling dominan
        if (Math.Abs(direction.X) > Math.Abs(direction.Y))
            Facing = direction.X > 0 ? Direction.Right : Direction.Left;
        else
            Facing = direction.Y > 0 ? Direction.Down : Direction.Up;

        Vector2 delta = direction * speed * deltaSeconds;
        TryMove(new Vector2(delta.X, 0));   // sumbu X dulu
        TryMove(new Vector2(0, delta.Y));   // lalu sumbu Y
    }

    private void TryMove(Vector2 delta)
    {
        if (delta == Vector2.Zero)
            return;

        if (CanStep(delta))
        {
            Position += delta;
            return;
        }

        // Menabrak. 1) Beri tahu yang ditabrak (semak bergoyang). 2) Coba geser di sudut.
        RectangleF wanted = CollisionBox;
        wanted.Offset(delta.X, delta.Y);
        if (Location!.FindBlocker(wanted, this) is IBumpable bumped)
            bumped.OnBumped(this);

        if (TryCornerNudge(delta))
            return;

        // Merapat pas ke dinding: coba setengah langkah, seperempat, dst.
        float fraction = 0.5f;
        while (fraction > 0.01f)
        {
            if (CanStep(delta * fraction))
            {
                Position += delta * fraction;
                return;
            }
            fraction /= 2f;
        }
    }

    private bool CanStep(Vector2 step)
    {
        RectangleF target = CollisionBox;
        target.Offset(step.X, step.Y);
        return Location!.CanOccupy(target, this);
    }

    /// <summary>Gerak lurus tertahan sudut? Cari geseran samping terkecil (1..6 px) yang membuka jalan.</summary>
    private bool TryCornerNudge(Vector2 delta)
    {
        bool horizontal = delta.Y == 0;
        for (float nudge = 1; nudge <= CornerNudgePixels; nudge++)
        {
            foreach (float sign in new[] { -1f, 1f })
            {
                Vector2 side = horizontal ? new Vector2(0, nudge * sign) : new Vector2(nudge * sign, 0);
                if (CanStep(side) && CanStep(side + delta))
                {
                    // geser maksimal 1 px per frame ke samping -> terasa halus, bukan teleport
                    Vector2 slide = horizontal ? new Vector2(0, sign) : new Vector2(sign, 0);
                    if (CanStep(slide))
                        Position += slide;
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>Semua karakter digambar dengan pola nama yang sama: "awalan_arah_frame", contoh "player_down_2".</summary>
    public override void Draw(Graphics g, IAssetProvider assets)
    {
        string spriteKey = SpritePrefix + "_" + Facing.ToString().ToLower() + "_" + AnimationFrame;
        assets.DrawSprite(g, spriteKey, SpriteBounds);
    }
}
