namespace FarmGame.World.Objects;

/// <summary>
/// Hiasan luar ruangan (MVP 5): peti, tong, orang-orangan sawah, tumpukan kayu.
/// Sama seperti Furniture, tapi kotak tabrakannya boleh lebih kecil dari tile-nya (supaya terasa pas).
/// </summary>
public class Decoration : Furniture
{
    private readonly RectangleF? _collision;   // relatif ke pojok kiri-atas tile; null = penuh

    public Decoration(Point tile, string spriteKey, Size sizeInTiles, int extraHeight = 0, RectangleF? collision = null)
        : base(tile, spriteKey, sizeInTiles, extraHeight)
    {
        _collision = collision;
    }

    public override RectangleF CollisionBox
    {
        get
        {
            if (_collision == null)
                return Bounds;
            RectangleF c = _collision.Value;
            return new RectangleF(Position.X + c.X, Position.Y + c.Y, c.Width, c.Height);
        }
    }
}
