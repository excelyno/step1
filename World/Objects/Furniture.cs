namespace FarmGame.World.Objects;

/// <summary>
/// Perabot rumah (MVP 3): meja, tanaman pot, karpet, jendela, keset.
/// Satu kelas untuk semua perabot biasa; bedanya hanya gambar, ukuran, tinggi gambar, solid, dan datar.
/// Perabot yang bisa DIPAKAI (kasur) dibuat turunan sendiri.
/// </summary>
public class Furniture : WorldObject
{
    private readonly string _spriteKey;
    private readonly Size _sizeInTiles;
    private readonly int _extraHeight;   // gambar menjulang ke atas berapa piksel
    private readonly bool _isSolid;
    private readonly bool _isFlat;       // datar di lantai (karpet, keset) = selalu digambar di bawah

    public Furniture(Point tile, string spriteKey, Size sizeInTiles, int extraHeight = 0, bool isSolid = true, bool isFlat = false)
        : base(tile)
    {
        _spriteKey = spriteKey;
        _sizeInTiles = sizeInTiles;
        _extraHeight = extraHeight;
        _isSolid = isSolid;
        _isFlat = isFlat;
    }

    public override Size SizeInTiles => _sizeInTiles;
    public override string SpriteKey => _spriteKey;
    public override bool IsSolid => _isSolid;

    public override RectangleF SpriteBounds =>
        new(Position.X, Position.Y - _extraHeight, HitboxSize.Width, HitboxSize.Height + _extraHeight);

    public override int DrawOrder => _isFlat ? int.MinValue + Tile.Y : base.DrawOrder;
}

/// <summary>Kasur. Diperiksa (X) = tidur, hari berakhir. (MVP 3: tidur pindah dari pintu ke kasur.)</summary>
public class Bed : Furniture, IInteractable
{
    public Bed(Point tile) : base(tile, "bed", new Size(1, 2))
    {
    }

    public bool CanInteract(Player player) => true;

    public string InteractLabel(Player player) => "Tidur";

    public ActionResult Interact(ActionContext context)
    {
        context.Services.RequestSleep();
        return ActionResult.None();
    }
}

/// <summary>Perapian di dinding: apinya berkedip (2 gambar bergantian).</summary>
public class Fireplace : Furniture, ILightSource
{
    public Fireplace(Point tile) : base(tile, "fireplace", new Size(2, 1), extraHeight: 16)
    {
    }

    public override string SpriteKey => "fireplace_" + (Environment.TickCount64 / 250 % 2);

    /// <summary>MVP 4: api menerangi ruangan, jari-jarinya bergoyang seperti api sungguhan.</summary>
    public IEnumerable<Light> GetLights()
    {
        float t = Environment.TickCount64 / 1000f;
        float flicker = MathF.Sin(t * 9f) * 3f + MathF.Sin(t * 23f) * 2f;
        yield return new Light(new Vector2(Position.X + 16, Position.Y + 6), 78 + flicker, Color.FromArgb(255, 170, 80), 1f);
    }
}
