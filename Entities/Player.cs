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

    // MVP 5: animasi ayunan alat (gambar alat berputar di tangan)
    private const float SwingSeconds = 0.3f;
    private float _swingTimer;
    private string? _swingSprite;
    private bool _swingIsPour;     // penyiram: dimiringkan, bukan diayun

    public Inventory Inventory { get; } = new(GameConfig.InventorySize);
    public Wallet Wallet { get; } = new(GameConfig.StartingGold);
    public Stamina Stamina { get; } = new(GameConfig.MaxStamina);
    public Relationships Relationships { get; } = new();   // MVP 4: pertemanan dengan warga

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

    /// <summary>MVP 5: hadap ke arah tertentu (dipakai saat memuat simpanan).</summary>
    public void FaceTo(Direction direction) => Facing = direction;

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
        _swingTimer = Math.Max(0, _swingTimer - deltaSeconds);
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
            StartSwing(usable as Item);
        }
        return result;
    }

    /// <summary>Alat (selain pancing, yang punya layar sendiri) diayun; bibit tidak.</summary>
    private void StartSwing(Item? item)
    {
        if (item is not Tool || item is FishingRod)
            return;
        _swingSprite = item.SpriteKey;
        _swingIsPour = item is WateringCan;
        _swingTimer = SwingSeconds;
    }

    /// <summary>MVP 5: menghadap ke atas = alat di belakang badan, arah lain = di depan.</summary>
    public override void Draw(Graphics g, IAssetProvider assets)
    {
        bool behind = Facing == Direction.Up;
        if (behind)
            DrawSwing(g, assets);
        base.Draw(g, assets);
        if (!behind)
            DrawSwing(g, assets);
    }

    /// <summary>
    /// Gambar alat berputar di sekitar tangan. Sudut 0 = alat tegak (gagang di bawah), 90 = mengarah ke depan.
    /// Ayunan: diangkat ke belakang lalu dihantamkan ke depan dengan cepat (ease-out).
    /// </summary>
    private void DrawSwing(Graphics g, IAssetProvider assets)
    {
        if (_swingTimer <= 0 || _swingSprite == null)
            return;

        float t = 1f - _swingTimer / SwingSeconds;
        float eased = 1f - (1f - t) * (1f - t);
        float angle = _swingIsPour ? 20f + 70f * eased : -50f + 160f * eased;

        Vector2 hand;
        float mirror = 1f;
        switch (Facing)
        {
            case Direction.Left: hand = new Vector2(Position.X + 2, Position.Y - 7); mirror = -1f; break;
            case Direction.Right: hand = new Vector2(Position.X + 10, Position.Y - 7); break;
            case Direction.Up: hand = new Vector2(Position.X + 9, Position.Y - 12); angle = _swingIsPour ? 30 * eased : -20f + 40f * eased; break;
            default: hand = new Vector2(Position.X + 9, Position.Y - 4); angle = _swingIsPour ? 150f + 40f * eased : 100f + 90f * eased; break;
        }

        GraphicsState state = g.Save();
        g.TranslateTransform(hand.X, hand.Y);
        g.ScaleTransform(mirror, 1f);
        g.RotateTransform(angle);
        assets.DrawSprite(g, _swingSprite, new RectangleF(-4, -13, 13, 13));
        g.Restore(state);
    }

    /// <summary>Tombol X / klik kanan: periksa benda di depan. Kalau tidak ada, coba makan item terpilih.</summary>
    public ActionResult Interact(IGameServices services)
    {
        if (IsBusy || Location == null)
            return ActionResult.None();

        // 1. Ada sesuatu yang bisa diperiksa di depan? (panen, kotak kirim, pintu, meja toko, papan, warga)
        IInteractable? target = Location.GetInteractableAt(FacingTile);
        if (target != null && target.CanInteract(this))
            return target.Interact(new ActionContext(this, Location, FacingTile, services));

        // 2. Kalau tidak, dan pegang makanan -> makan
        if (Inventory.SelectedItem is IEdible food)
        {
            ActionResult result = food.Eat(this);
            if (result.Success)
            {
                Inventory.RemoveFromSlot(Inventory.SelectedIndex, 1);
                if (Location is IGameServices game)
                    game.PlaySound("eat");
            }
            return result;
        }

        return ActionResult.None();
    }
}
