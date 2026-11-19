namespace FarmGame.World.Objects;

/// <summary>
/// Petak tanah yang sudah dicangkul. Bisa disiram, ditanami (punya Crop di dalamnya = komposisi),
/// dan dipanen. Setiap pagi tanamannya tumbuh kalau kemarin disiram.
/// </summary>
public class HoeDirt : WorldObject, IInteractable, IDayUpdatable
{
    public Crop? Crop { get; private set; }
    public bool IsWatered { get; private set; }

    public bool HasCrop => Crop != null;

    public HoeDirt(Point tile) : base(tile)
    {
    }

    public override bool IsSolid => false;   // bisa diinjak

    /// <summary>Tanah selalu digambar paling bawah (di bawah pemain), apa pun posisinya.</summary>
    public override int DrawOrder => int.MinValue + Tile.Y;

    /// <summary>
    /// Contoh "hoedirt_dry_5". Angka = tetangga HoeDirt mana saja yang ada (atas=1, kanan=2, bawah=4, kiri=8),
    /// supaya petak yang bersebelahan tergambar menyambung jadi satu bedeng, bukan kotak-kotak terpisah.
    /// </summary>
    public override string SpriteKey => $"hoedirt_{(IsWatered ? "wet" : "dry")}_{NeighborMask()}";

    private int NeighborMask()
    {
        int mask = 0;
        if (HasHoeDirtAt(0, -1)) mask += 1;
        if (HasHoeDirtAt(1, 0)) mask += 2;
        if (HasHoeDirtAt(0, 1)) mask += 4;
        if (HasHoeDirtAt(-1, 0)) mask += 8;
        return mask;
    }

    private bool HasHoeDirtAt(int dx, int dy) =>
        Location?.GetObjectAt(new Point(Tile.X + dx, Tile.Y + dy)) is HoeDirt;

    public void Plant(Crop crop)
    {
        if (HasCrop)
            throw new InvalidOperationException("Sudah ada tanaman.");   // Seed sudah cek duluan
        Crop = crop;
    }

    /// <summary>Siram. False kalau sudah disiram hari ini.</summary>
    public bool Water()
    {
        if (IsWatered)
            return false;
        IsWatered = true;
        return true;
    }

    public override void Draw(Graphics g, IAssetProvider assets)
    {
        base.Draw(g, assets);                                 // gambar tanah
        if (Crop != null)
            assets.DrawSprite(g, Crop.SpriteKey, SpriteBounds); // gambar tanaman di atasnya
    }

    // ---------- IInteractable: panen ----------

    public bool CanInteract(Player player) => Crop != null && (Crop.IsReady || Crop.IsDead);

    public ActionResult Interact(ActionContext context)
    {
        if (Crop == null)
            return ActionResult.None();

        if (Crop.IsDead)
        {
            Crop = null;
            return ActionResult.Ok("Tanaman mati dibersihkan.");
        }

        if (!Crop.IsReady)
            return ActionResult.Fail("Belum siap dipanen.");

        Item produce = context.Services.Items.Create(Crop.Data.Id);
        if (context.User.Inventory.TryAdd(produce) > 0)
            return ActionResult.Fail("Tas penuh!");   // tanaman tetap di tanah

        Crop = null;
        return ActionResult.Ok($"+1 {produce.Name}");
    }

    // ---------- IDayUpdatable: tumbuh tiap pagi ----------

    public void OnNewDay(GameDate newDate)
    {
        Crop?.AdvanceDay(IsWatered, newDate.Season);   // tumbuh berdasarkan siraman KEMARIN
        IsWatered = false;                              // lalu tanah kering lagi
    }
}
