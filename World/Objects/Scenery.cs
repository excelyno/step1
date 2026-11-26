namespace FarmGame.World.Objects;

/// <summary>
/// Kelas induk hiasan alam dari file map (pohon, semak, batu, rumput liar, bunga, teratai).
/// MVP 3: pohon, batu, ranting, kerikil ada di BreakableScenery.cs (bisa dihancurkan).
/// Turunan cukup bilang: nama gambarnya, ikut goyang kena angin atau tidak, dan punya berapa variasi.
/// MVP 5: bisa "bergoyang" sebentar saat diinjak atau ditabrak (Rustle).
/// </summary>
public abstract class Scenery : WorldObject
{
    // Urutan frame goyang: 0 = tegak, 1 = condong kanan, 2 = condong kiri
    private static readonly int[] SwaySequence = { 0, 1, 1, 0, 0, 2, 2, 0 };

    private const float RustleSeconds = 0.45f;
    private float _rustleTimer;

    protected Scenery(Point tile) : base(tile)
    {
    }

    /// <summary>Nama dasar gambar, contoh "tree". WAJIB diisi turunan.</summary>
    protected abstract string BaseKey { get; }

    /// <summary>Ikut bergoyang kena angin? Default: tidak.</summary>
    protected virtual bool Sways => false;

    /// <summary>Jumlah variasi gambar (misalnya warna bunga). Default: 1.</summary>
    protected virtual int Variants => 1;

    /// <summary>Contoh: "rock", "flowers_2_1" (variasi 2, frame 1), "tree_0".</summary>
    public override string SpriteKey
    {
        get
        {
            string key = BaseKey;
            if (Variants > 1)
                key += "_" + (Tile.X * 7 + Tile.Y * 13) % Variants;   // variasi tetap per posisi
            if (Sways)
                key += "_" + SwayFrame();
            return key;
        }
    }

    protected bool IsRustling => _rustleTimer > 0;

    /// <summary>
    /// MVP 5: mulai bergoyang (diinjak/ditabrak). Daun kecil beterbangan hanya kalau sebelumnya diam,
    /// supaya tidak muncul terus-menerus selama pemain berdiri di atasnya.
    /// </summary>
    protected void Rustle()
    {
        if (_rustleTimer <= 0)
            Location?.Services?.SpawnEffect(EffectKind.Leaves, new Vector2(Center.X, Bounds.Bottom - 4));
        _rustleTimer = RustleSeconds;
    }

    public override void Update(float deltaSeconds)
    {
        _rustleTimer = Math.Max(0, _rustleTimer - deltaSeconds);
    }

    /// <summary>
    /// Frame goyang dari jam komputer. Posisi X ikut dihitung, jadi angin terlihat
    /// "berjalan" dari kiri ke kanan seperti gelombang, bukan semua bergerak bersamaan.
    /// MVP 5: saat bergoyang (diinjak), frame berganti kiri-kanan dengan cepat.
    /// </summary>
    private int SwayFrame()
    {
        if (IsRustling)
            return (int)(_rustleTimer * 16) % 2 + 1;

        double seconds = Environment.TickCount64 / 1000.0 - Tile.X * 0.11 - Tile.Y * 0.04;
        int step = (int)Math.Floor(seconds * 3);
        return SwaySequence[((step % SwaySequence.Length) + SwaySequence.Length) % SwaySequence.Length];
    }
}

/// <summary>Semak: menghalangi jalan, sedikit lebih tinggi dari 1 tile. MVP 5: bergoyang kalau ditabrak.</summary>
public class Bush : Scenery, IBumpable
{
    public Bush(Point tile) : base(tile) { }

    protected override string BaseKey => "bush";
    public override RectangleF SpriteBounds => new(Position.X, Position.Y - 4, 16, 20);
    public override RectangleF CollisionBox => new(Position.X + 2, Position.Y + 3, 12, 11);

    public void OnBumped(Character by) => Rustle();

    /// <summary>Saat bergoyang, gambar digeser kiri-kanan.</summary>
    public override void Draw(Graphics g, IAssetProvider assets)
    {
        RectangleF bounds = SpriteBounds;
        if (IsRustling)
            bounds.Offset(MathF.Sin(Environment.TickCount64 / 25f), 0);
        assets.DrawSprite(g, SpriteKey, bounds);
    }
}

/// <summary>Rumput liar: bisa dilewati, bergoyang. MVP 5: bergoyang cepat kalau diinjak.</summary>
public class GrassTuft : Scenery, ITouchable
{
    public GrassTuft(Point tile) : base(tile) { }

    protected override string BaseKey => "tuft";
    protected override bool Sways => true;
    public override bool IsSolid => false;

    public void OnTouched(Character by) => Rustle();
}

/// <summary>Rumpun bunga: bisa dilewati, bergoyang, 3 warna. MVP 5: bergoyang cepat kalau diinjak.</summary>
public class Flowers : Scenery, ITouchable
{
    public Flowers(Point tile) : base(tile) { }

    protected override string BaseKey => "flowers";
    protected override bool Sways => true;
    protected override int Variants => 3;
    public override bool IsSolid => false;

    public void OnTouched(Character by) => Rustle();
}

/// <summary>Teratai di kolam: datar, selalu digambar di bawah.</summary>
public class LilyPad : Scenery
{
    public LilyPad(Point tile) : base(tile) { }

    protected override string BaseKey => "lily";
    protected override int Variants => 2;
    public override bool IsSolid => false;
    public override int DrawOrder => int.MinValue + Tile.Y;
}
