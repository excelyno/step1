namespace FarmGame.World.Objects;

/// <summary>Pintu rumah. MVP 1: diperiksa (X) = langsung tidur. MVP 2: masuk ke dalam rumah.</summary>
public class FarmhouseDoor : WorldObject, IInteractable
{
    public FarmhouseDoor(Point tile) : base(tile)
    {
    }

    public override string SpriteKey => "door";

    public bool CanInteract(Player player) => true;

    public ActionResult Interact(ActionContext context)
    {
        context.Services.RequestSleep();
        return ActionResult.None();   // tidak perlu pesan, layar langsung gelap
    }
}
