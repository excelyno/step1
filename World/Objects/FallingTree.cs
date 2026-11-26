namespace FarmGame.World.Objects;

/// <summary>
/// Efek pohon tumbang (MVP 5): gambar pohon berputar 90 derajat di pangkal batangnya, makin lama makin cepat
/// (seperti jatuh sungguhan), lalu hilang dengan semburan daun dan getaran layar. Bukan benda solid.
/// </summary>
public class FallingTree : GameObject
{
    private const float FallSeconds = 0.75f;

    private readonly string _spriteKey;
    private readonly RectangleF _sprite;
    private readonly Vector2 _pivot;       // pangkal batang
    private readonly float _direction;     // +1 = jatuh ke kanan, -1 = ke kiri
    private float _time;

    public FallingTree(string spriteKey, RectangleF sprite, Vector2 pivot, bool fallRight)
    {
        _spriteKey = spriteKey;
        _sprite = sprite;
        _pivot = pivot;
        _direction = fallRight ? 1 : -1;
        PlaceAt(new Vector2(pivot.X - 8, pivot.Y - 8));
    }

    public override SizeF HitboxSize => new(16, 8);
    public override RectangleF SpriteBounds => RectangleF.Inflate(_sprite, 48, 16);
    public override int DrawOrder => (int)_pivot.Y + 1;

    public override void Update(float deltaSeconds)
    {
        _time += deltaSeconds;
        if (_time < FallSeconds || Location == null)
            return;

        // Sampai di tanah: daun beterbangan di ujung pohon, layar bergetar, efek selesai
        IGameServices? services = Location.Services;
        float reach = _sprite.Height * 0.7f;
        services?.SpawnEffect(EffectKind.Leaves, new Vector2(_pivot.X + _direction * reach, _pivot.Y - 6));
        services?.SpawnEffect(EffectKind.Leaves, new Vector2(_pivot.X + _direction * reach * 0.5f, _pivot.Y - 4));
        services?.ShakeCamera(2.5f, 0.3f);
        Location.Remove(this);
    }

    public override void Draw(Graphics g, IAssetProvider assets)
    {
        float t = Math.Min(1f, _time / FallSeconds);
        float angle = 90f * t * t * _direction;   // t² = mulai pelan, makin cepat

        GraphicsState state = g.Save();
        g.TranslateTransform(_pivot.X, _pivot.Y);
        g.RotateTransform(angle);
        assets.DrawSprite(g, _spriteKey, new RectangleF(_sprite.X - _pivot.X, _sprite.Y - _pivot.Y, _sprite.Width, _sprite.Height));
        g.Restore(state);
    }
}
