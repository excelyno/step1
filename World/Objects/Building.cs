namespace FarmGame.World.Objects;

/// <summary>
/// Gambar bangunan (dekorasi saja). Tabrakan diurus huruf B di file map, jadi bangunan tidak solid
/// dan TIDAK masuk indeks tile. Pintu dan meja toko adalah objek terpisah.
/// MVP 2: dipakai rumah di kebun, rumah warga, dan toko.
/// MVP 4: jendela menyala saat gelap (ILightSource) dan bayangan jatuh ke kanan-bawah.
/// MVP 5: ukuran sebenarnya (rumah = 7x4 tile + atap 3 tile, 4x tinggi pemain), asap cerobong,
///        dan atap tembus pandang kalau pemain berjalan di belakangnya.
/// </summary>
public class Building : GameObject, ILightSource
{
    private const int ShadowLength = 10;          // piksel logis
    private const float SmokeEverySeconds = 0.45f;

    private readonly string _spriteKey;
    private readonly int _roofExtraHeight;   // atap menjulang di atas tile bangunan (piksel)
    private readonly IReadOnlyList<Vector2> _windowLights;   // posisi jendela, relatif ke pojok kiri-atas gambar
    private readonly Vector2? _chimney;      // ujung cerobong, relatif ke pojok kiri-atas gambar
    private float _smokeTimer;

    public Point TopLeftTile { get; }
    public Size SizeInTiles { get; }

    public Building(Point topLeftTile, Size sizeInTiles, int roofExtraHeight, string spriteKey,
                    IReadOnlyList<Vector2>? windowLights = null, Vector2? chimney = null)
    {
        TopLeftTile = topLeftTile;
        SizeInTiles = sizeInTiles;
        _roofExtraHeight = roofExtraHeight;
        _spriteKey = spriteKey;
        _windowLights = windowLights ?? Array.Empty<Vector2>();
        _chimney = chimney;
        PlaceAt(TileMap.TileToWorld(topLeftTile.X, topLeftTile.Y));
    }

    // ---------- Ukuran gambar bangunan (dari Tools/generate_assets.py) ----------
    // Rumah besar 112 px lebar (7 tile). Jendela: satu di loteng, dua di dinding.

    /// <summary>Rumah pemain (kayu, atap hijau). Gambar 112x112.</summary>
    public static Building House(Point topLeft) => new(topLeft, new Size(7, 4), 48, "house",
        new[] { new Vector2(56, 46), new Vector2(25, 83), new Vector2(88, 83) }, new Vector2(87, 2));

    /// <summary>Rumah warga biru (plester). Gambar 112x108.</summary>
    public static Building BlueHouse(Point topLeft) => new(topLeft, new Size(7, 4), 44, "house_blue",
        new[] { new Vector2(56, 44), new Vector2(25, 79), new Vector2(88, 79) }, new Vector2(87, 2));

    /// <summary>Rumah warga merah kecil (bata). Gambar 80x104.</summary>
    public static Building RedHouse(Point topLeft) => new(topLeft, new Size(5, 4), 40, "house_red",
        new[] { new Vector2(17, 75), new Vector2(64, 75) }, new Vector2(55, 2));

    /// <summary>Toko bibit. Gambar 128x108, pintu terbuka (hiasan).</summary>
    public static Building Shop(Point topLeft) => new(topLeft, new Size(8, 4), 44, "shop",
        new[] { new Vector2(29, 80), new Vector2(100, 80) });

    public override SizeF HitboxSize => new(SizeInTiles.Width * GameConfig.TileSize, SizeInTiles.Height * GameConfig.TileSize);

    /// <summary>Tidak solid: dinding sudah ditangani tile 'B' di map.</summary>
    public override bool IsSolid => false;

    public override bool FadesWhenBehind => true;

    public override RectangleF SpriteBounds =>
        new(Position.X, Position.Y - _roofExtraHeight, HitboxSize.Width, HitboxSize.Height + _roofExtraHeight);

    /// <summary>Satu piksel lebih awal dari pintu, supaya pintu selalu tergambar di atas bangunan.</summary>
    public override int DrawOrder => (int)Bounds.Bottom - 1;

    /// <summary>Semua tile yang ditutup bangunan (dipakai untuk cek kecocokan dengan map).</summary>
    public IEnumerable<Point> CoveredTiles
    {
        get
        {
            for (int x = 0; x < SizeInTiles.Width; x++)
                for (int y = 0; y < SizeInTiles.Height; y++)
                    yield return new Point(TopLeftTile.X + x, TopLeftTile.Y + y);
        }
    }

    /// <summary>Asap mengepul dari cerobong.</summary>
    public override void Update(float deltaSeconds)
    {
        if (_chimney == null || Location?.Services == null)
            return;
        _smokeTimer -= deltaSeconds;
        if (_smokeTimer > 0)
            return;
        _smokeTimer = SmokeEverySeconds;
        RectangleF sprite = SpriteBounds;
        Location.Services.SpawnEffect(EffectKind.Smoke, new Vector2(sprite.X + _chimney.Value.X, sprite.Y + _chimney.Value.Y));
    }

    public IEnumerable<Light> GetLights()
    {
        RectangleF sprite = SpriteBounds;
        foreach (Vector2 window in _windowLights)
            yield return new Light(new Vector2(sprite.X + window.X, sprite.Y + window.Y), 34, Light.Warm, 0.75f);
    }

    /// <summary>
    /// Bayangan bangunan: cahaya dari kiri-atas, jadi bayangan jatuh ke kanan dan ke bawah.
    /// Digambar sebelum semua objek, supaya tidak menutupi apa pun.
    /// </summary>
    public override void DrawShadow(Graphics g)
    {
        RectangleF b = Bounds;
        float s = ShadowLength;
        PointF[] shape =
        {
            new(b.Right, b.Top - 6),
            new(b.Right + s, b.Top - 6 + s),
            new(b.Right + s, b.Bottom + s - 3),
            new(b.Left + s, b.Bottom + s - 3),
            new(b.Left + 2, b.Bottom),
            new(b.Right, b.Bottom),
        };
        using SolidBrush brush = new(Color.FromArgb(70, 20, 12, 30));
        g.FillPolygon(brush, shape);
    }

    public override void Draw(Graphics g, IAssetProvider assets)
    {
        assets.DrawSprite(g, _spriteKey, SpriteBounds);
    }
}
