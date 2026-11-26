namespace FarmGame.Entities;

/// <summary>
/// Burung pipit (MVP 5): mematuk-matuk di tanah, melompat kecil, dan terbang menjauh kalau pemain mendekat.
/// Setelah beberapa detik ia hinggap lagi di tempat acak di dalam areanya. Hanya hiasan: tidak solid, tidak disimpan.
/// </summary>
public class Bird : GameObject
{
    private const float ScareDistance = 34f;
    private const float FlySpeed = 70f;

    private static readonly Random Rng = new();

    private readonly RectangleF _area;
    private bool _flying;
    private Vector2 _velocity;
    private float _timer;        // waktu sampai aksi berikutnya
    private float _animTime;
    private bool _facingLeft;

    public Bird(Vector2 start, RectangleF area)
    {
        PlaceAt(start);
        _area = area;
        _timer = (float)Rng.NextDouble() * 2f;
    }

    public override SizeF HitboxSize => new(8, 4);
    public override RectangleF SpriteBounds => new(Position.X, Position.Y - 4 - (_flying ? 0 : 0), 8, 8);
    public override int DrawOrder => _flying ? int.MaxValue - 1 : (int)Bounds.Bottom;

    public override void Update(float deltaSeconds)
    {
        _animTime += deltaSeconds;
        _timer -= deltaSeconds;

        if (_flying)
        {
            Position += _velocity * deltaSeconds;
            if (_timer <= 0)
                Land();
            return;
        }

        // Pemain (atau makhluk lain) terlalu dekat? Terbang!
        if (Location != null)
        {
            foreach (GameObject obj in Location.Objects)
            {
                if (obj is Character walker && Vector2.Distance(walker.Center, Center) < ScareDistance)
                {
                    FlyAwayFrom(walker.Center);
                    return;
                }
            }
        }

        // Sesekali melompat kecil ke samping
        if (_timer <= 0)
        {
            float hop = (float)(Rng.NextDouble() * 2 - 1) * 6f;
            Vector2 next = Position + new Vector2(hop, (float)(Rng.NextDouble() * 2 - 1) * 3f);
            if (_area.Contains(next.X, next.Y))
            {
                _facingLeft = hop < 0;
                Position = next;
            }
            _timer = 0.8f + (float)Rng.NextDouble() * 1.5f;
        }
    }

    private void FlyAwayFrom(Vector2 danger)
    {
        Vector2 away = Center - danger;
        if (away == Vector2.Zero)
            away = new Vector2(1, 0);
        away = Vector2.Normalize(away);
        _velocity = new Vector2(away.X, -0.9f) * FlySpeed;   // menjauh sambil naik
        _facingLeft = _velocity.X < 0;
        _flying = true;
        _timer = 2.5f + (float)Rng.NextDouble() * 2f;
    }

    /// <summary>Hinggap lagi di titik acak (diam-diam: muncul di tanah di tempat lain).</summary>
    private void Land()
    {
        _flying = false;
        PlaceAt(new Vector2(_area.X + (float)Rng.NextDouble() * _area.Width, _area.Y + (float)Rng.NextDouble() * _area.Height));
        _timer = 1f + (float)Rng.NextDouble() * 2f;
    }

    public override void Draw(Graphics g, IAssetProvider assets)
    {
        string key = _flying
            ? "bird_fly_" + (int)(_animTime * 12) % 2
            : "bird_idle_" + ((int)(_animTime * 3) % 4 == 0 ? 1 : 0);   // sesekali menunduk mematuk

        RectangleF dest = SpriteBounds;
        if (!_facingLeft)
        {
            assets.DrawSprite(g, key, dest);
            return;
        }
        // Menghadap kiri: gambar dicerminkan
        GraphicsState state = g.Save();
        g.TranslateTransform(dest.X + dest.Width, dest.Y);
        g.ScaleTransform(-1, 1);
        assets.DrawSprite(g, key, new RectangleF(0, 0, dest.Width, dest.Height));
        g.Restore(state);
    }
}
