namespace FarmGame.World.Objects;

/// <summary>Tiang lampu (MVP 4): solid, gambar 2 tile tinggi, menyala lembut dan sedikit berkedip.</summary>
public class LampPost : WorldObject, ILightSource
{
    public LampPost(Point tile) : base(tile)
    {
    }

    public override string SpriteKey => "lamppost";

    public override RectangleF SpriteBounds => new(Position.X, Position.Y - 16, 16, 32);
    public override RectangleF CollisionBox => new(Position.X + 5, Position.Y + 9, 6, 6);   // MVP 5: hanya tiangnya

    public IEnumerable<Light> GetLights()
    {
        // Kedip pelan: jari-jari naik-turun sedikit, beda fase tiap lampu
        float flicker = MathF.Sin(Environment.TickCount64 / 180f + Tile.X * 1.7f) * 2f;
        yield return new Light(new Vector2(Position.X + 8, Position.Y - 10), 62 + flicker, Light.Warm, 0.95f);
    }
}
