namespace FarmGame.Entities;

/// <summary>
/// Kupu-kupu hiasan: terbang acak di dalam sebuah area. Contoh POLIMORFISME Update():
/// GameLocation memanggil Update() yang sama untuk pemain dan kupu-kupu, perilakunya beda.
/// </summary>
public class Butterfly : GameObject
{
    private static readonly Random Rng = new();

    private readonly RectangleF _area;   // batas terbang
    private readonly int _color;         // 0 = oranye, 1 = putih-biru
    private Vector2 _velocity;
    private float _turnTimer;            // kapan ganti arah
    private float _flapTimer;            // untuk kepak sayap

    public Butterfly(Vector2 start, RectangleF area, int color)
    {
        PlaceAt(start);
        _area = area;
        _color = color;
    }

    public override SizeF HitboxSize => new(7, 6);

    /// <summary>Terbang = selalu digambar paling atas.</summary>
    public override int DrawOrder => int.MaxValue;

    public override void Update(float deltaSeconds)
    {
        _flapTimer += deltaSeconds;
        _turnTimer -= deltaSeconds;

        // Sesekali ganti arah secara acak
        if (_turnTimer <= 0)
        {
            float angle = (float)(Rng.NextDouble() * Math.PI * 2);
            float speed = 10f + (float)Rng.NextDouble() * 14f;
            _velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            _turnTimer = 0.6f + (float)Rng.NextDouble() * 1.4f;
        }

        // Keluar area? Belok kembali ke tengah area
        if (!_area.Contains(Position.X, Position.Y))
        {
            Vector2 center = new(_area.X + _area.Width / 2, _area.Y + _area.Height / 2);
            _velocity = Vector2.Normalize(center - Position) * 16f;
        }

        // Naik-turun kecil seperti mengepak
        Vector2 flutter = new(0, MathF.Sin(_flapTimer * 9f) * 6f);
        Position += (_velocity + flutter) * deltaSeconds;
    }

    public override void Draw(Graphics g, IAssetProvider assets)
    {
        int frame = (int)(_flapTimer * 10) % 2;   // sayap buka / tutup
        assets.DrawSprite(g, $"butterfly_{_color}_{frame}", Bounds);
    }
}
