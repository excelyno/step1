namespace FarmGame.World.Objects;

/// <summary>Pintu rumah warga. Belum bisa masuk, jadi diperiksa (X) hanya memberi pesan.</summary>
public class LockedDoor : Door
{
    private readonly string _message;

    public LockedDoor(Point tile, string message) : base(tile)
    {
        _message = message;
    }

    protected override bool OpensWhenNear => false;

    public override string InteractLabel(Player player) => "Ketuk";

    public override ActionResult Interact(ActionContext context) => ActionResult.Fail(_message);
}
