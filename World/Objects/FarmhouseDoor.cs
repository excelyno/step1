namespace FarmGame.World.Objects;

/// <summary>
/// Pintu rumah setinggi pemain (MVP 5: gambar 16x28). Pintu terbuka sendiri kalau pemain berdiri di depannya.
/// Turunan menentukan apa yang terjadi saat diperiksa.
/// </summary>
public abstract class Door : WorldObject, IInteractable
{
    private const float OpenDistance = 22f;   // piksel dari bawah pintu ke kaki pemain

    protected Door(Point tile) : base(tile)
    {
    }

    /// <summary>Pintu rumah orang yang terkunci tidak terbuka sendiri.</summary>
    protected virtual bool OpensWhenNear => true;

    private bool IsPlayerNear =>
        OpensWhenNear && Location != null &&
        Location.Objects.OfType<Player>().Any(p => Vector2.Distance(p.Center, new Vector2(Center.X, Bounds.Bottom + 6)) < OpenDistance);

    public override string SpriteKey => IsPlayerNear ? "door_tall_open" : "door_tall";
    public override RectangleF SpriteBounds => new(Position.X, Position.Y - 12, 16, 28);

    public bool CanInteract(Player player) => true;

    public abstract string InteractLabel(Player player);

    public abstract ActionResult Interact(ActionContext context);
}

/// <summary>Pintu rumah di kebun. MVP 3: diperiksa (X) = masuk ke dalam rumah (tidur sekarang di kasur).</summary>
public class FarmhouseDoor : Door
{
    public FarmhouseDoor(Point tile) : base(tile)
    {
    }

    public override string InteractLabel(Player player) => "Masuk";

    public override ActionResult Interact(ActionContext context)
    {
        context.Services.WarpTo(FarmHouse.LocationName, FarmHouse.EntranceTile);
        return ActionResult.None();   // tidak perlu pesan, layar langsung gelap
    }
}
