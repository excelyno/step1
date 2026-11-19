namespace FarmGame.Entities;

/// <summary>
/// Karakter yang dikendalikan pemain.
/// Player TIDAK membaca keyboard sendiri. PlayingState memberinya "niat gerak" dan perintah aksi.
/// MVP 1: punya tas, dompet, dan energi (KOMPOSISI: dirakit dari objek lain, bukan diwariskan).
/// </summary>
public class Player : Character
{
    private Vector2 _moveIntent;   // arah gerak yang diinginkan (panjang maksimal 1)
    private bool _wantsToRun;
    private float _actionCooldown; // sisa jeda setelah pakai alat (detik)

    public Inventory Inventory { get; } = new(GameConfig.InventorySize);
    public Wallet Wallet { get; } = new(GameConfig.StartingGold);
    public Stamina Stamina { get; } = new(GameConfig.MaxStamina);

    protected override string SpritePrefix => "player";

    public float CurrentSpeed => _wantsToRun ? GameConfig.PlayerRunSpeed : GameConfig.PlayerWalkSpeed;

    /// <summary>Sedang jeda setelah aksi: tidak bisa jalan atau beraksi.</summary>
    public bool IsBusy => _actionCooldown > 0;

    /// <summary>Tile tepat di depan pemain (sasaran alat dan tombol periksa).</summary>
    public Point FacingTile
    {
        get
        {
            Point tile = TilePosition;
            if (Facing == Direction.Up) return new Point(tile.X, tile.Y - 1);
            if (Facing == Direction.Down) return new Point(tile.X, tile.Y + 1);
            if (Facing == Direction.Left) return new Point(tile.X - 1, tile.Y);
            return new Point(tile.X + 1, tile.Y);
        }
    }

    public Player(string name) : base(name)
    {
    }

    public void SetMovementIntent(Vector2 direction, bool run)
    {
        // Jaga panjang vektor maksimal 1, supaya jalan diagonal tidak lebih cepat
        if (direction.Length() > 1f)
            direction = Vector2.Normalize(direction);

        _moveIntent = direction;
        _wantsToRun = run;
    }

    public override void Update(float deltaSeconds)
    {
        if (IsBusy)
        {
            _actionCooldown -= deltaSeconds;
            Move(Vector2.Zero, 0, deltaSeconds);   // diam selama jeda
            return;
        }

        Move(_moveIntent, CurrentSpeed, deltaSeconds);
    }

    /// <summary>Tombol C / klik kiri: pakai item terpilih ke tile depan.</summary>
    public ActionResult UseSelectedItem(IGameServices services)
    {
        if (IsBusy || Location == null)
            return ActionResult.None();

        // Polimorfisme: tidak peduli Hoe, WateringCan, atau Seed. Cukup tahu "bisa dipakai".
        if (Inventory.SelectedItem is not IUsable usable)
            return ActionResult.None();

        ActionResult result = usable.Use(new ActionContext(this, Location, FacingTile, services));

        if (result.Success)
        {
            if (usable.ConsumesOnUse)
                Inventory.RemoveFromSlot(Inventory.SelectedIndex, 1);   // bibit berkurang
            _actionCooldown = GameConfig.ToolCooldownSeconds;
        }
        return result;
    }

    /// <summary>Tombol X / klik kanan: periksa benda di depan. Kalau tidak ada, coba makan item terpilih.</summary>
    public ActionResult Interact(IGameServices services)
    {
        if (IsBusy || Location == null)
            return ActionResult.None();

        // 1. Ada benda yang bisa diperiksa di depan? (panen, kotak kirim, pintu)
        if (Location.GetObjectAt(FacingTile) is IInteractable target && target.CanInteract(this))
            return target.Interact(new ActionContext(this, Location, FacingTile, services));

        // 2. Kalau tidak, dan pegang makanan -> makan
        if (Inventory.SelectedItem is IEdible food)
        {
            ActionResult result = food.Eat(this);
            if (result.Success)
                Inventory.RemoveFromSlot(Inventory.SelectedIndex, 1);
            return result;
        }

        return ActionResult.None();
    }
}
