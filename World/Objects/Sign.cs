namespace FarmGame.World.Objects;

/// <summary>Papan petunjuk. Diperiksa (X) = tulisannya muncul. MVP 5: di kotak dialog, bukan pesan kecil.</summary>
public class Sign : WorldObject, IInteractable
{
    public string Text { get; }

    public Sign(Point tile, string text) : base(tile)
    {
        Text = text;
    }

    public override string SpriteKey => "sign";

    /// <summary>Papan lebih tinggi dari 1 tile (tiangnya menjulang 8 piksel).</summary>
    public override RectangleF SpriteBounds => new(Position.X, Position.Y - 8, 16, 24);
    public override RectangleF CollisionBox => new(Position.X + 3, Position.Y + 8, 10, 7);

    public bool CanInteract(Player player) => true;
    public string InteractLabel(Player player) => "Baca";

    public ActionResult Interact(ActionContext context)
    {
        context.Services.ShowDialogue("Papan", new[] { Text });
        return ActionResult.None();
    }
}
