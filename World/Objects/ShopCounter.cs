namespace FarmGame.World.Objects;

/// <summary>
/// Meja toko. Diperiksa (X) saat jam buka = menu toko terbuka.
/// Meja tidak tahu cara kerja menu: ia hanya minta lewat IGameServices.OpenShop().
/// </summary>
public class ShopCounter : WorldObject, IInteractable
{
    private readonly int _widthTiles;

    public Shop Shop { get; }

    /// <summary>MVP 4: penjual di balik meja. Quest-nya diurus lewat meja (penjual tidak bisa didekati langsung).</summary>
    public Npc? Owner { get; }

    public ShopCounter(Point tile, int widthTiles, Shop shop, Npc? owner = null) : base(tile)
    {
        _widthTiles = widthTiles;
        Shop = shop;
        Owner = owner;
    }

    public override Size SizeInTiles => new(_widthTiles, 1);
    public override string SpriteKey => "counter";

    /// <summary>Gambar 8 piksel lebih tinggi dari hitbox (barang di atas meja).</summary>
    public override RectangleF SpriteBounds => new(Position.X, Position.Y - 8, HitboxSize.Width, 24);

    public bool CanInteract(Player player) => true;

    public string InteractLabel(Player player) => "Belanja";

    public ActionResult Interact(ActionContext context)
    {
        // MVP 4: penjual punya quest? Itu dulu.
        ActionResult? questResult = Owner?.TryQuestOrGift(context, allowGift: false);
        if (questResult != null)
            return questResult;

        if (!Shop.IsOpen(context.Services.TimeOfDay))
        {
            string closed = $"Maaf, tokonya tutup. Buka jam {Shop.HoursText}, ya!";
            return Owner != null ? Owner.Talk(context, closed) : ActionResult.Fail(closed);
        }

        context.Services.OpenShop(Shop);
        return ActionResult.None();   // menu langsung tampil, tidak perlu pesan
    }
}
