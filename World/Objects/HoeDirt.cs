namespace FarmGame.World.Objects;

/// <summary>
/// Petak tanah yang sudah dicangkul. Bisa disiram, ditanami (punya Crop di dalamnya = komposisi),
/// dan dipanen. Setiap pagi tanamannya tumbuh kalau kemarin disiram.
///
/// Versi baru:
///   - Panen memakai minigame sesuai tanaman (lihat Content/Data/lessons.json).
///   - Hama bisa datang tiap pagi. Kalau dibiarkan 2 hari, panen berikutnya berkurang 1 buah.
///   - Tanaman berbuah ulang (tomat, kale, jagung, terong) tidak hilang setelah dipanen.
/// </summary>
public class HoeDirt : WorldObject, IInteractable, IDayUpdatable, ISaveable
{
    public Crop? Crop { get; private set; }
    public bool IsWatered { get; private set; }

    /// <summary>Hama yang sedang ada di tanaman ini (None = sehat).</summary>
    public HazardKind Hazard { get; private set; }

    /// <summary>Sudah berapa pagi hama ini dibiarkan.</summary>
    public int HazardDays { get; private set; }

    /// <summary>Sudah kena hukuman: panen berikutnya berkurang 1 buah.</summary>
    public bool Penalty { get; private set; }

    public bool HasCrop => Crop != null;
    public bool HasHazard => Hazard != HazardKind.None;

    public HoeDirt(Point tile) : base(tile)
    {
    }

    public override bool IsSolid => false;   // bisa diinjak

    /// <summary>Tanah selalu digambar paling bawah (di bawah pemain), apa pun posisinya.</summary>
    public override int DrawOrder => int.MinValue + Tile.Y;

    /// <summary>
    /// Contoh "hoedirt_dry_5". Angka = tetangga HoeDirt mana saja yang ada (atas=1, kanan=2, bawah=4, kiri=8),
    /// supaya petak yang bersebelahan tergambar menyambung jadi satu bedeng, bukan kotak-kotak terpisah.
    /// MVP 6: string di-cache; dihitung ulang hanya ketika disiram / tetangga berubah (bukan tiap frame).
    /// </summary>
    public override string SpriteKey
    {
        get
        {
            if (_spriteKey == null)
                _spriteKey = $"hoedirt_{(IsWatered ? "wet" : "dry")}_{NeighborMask()}";
            return _spriteKey;
        }
    }

    private string? _spriteKey;

    /// <summary>Panggil ini ketika tanah berubah keadaan (disiram, tetangga dicangkul) supaya nama gambar dihitung ulang.</summary>
    public void InvalidateSprite() => _spriteKey = null;

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
        InvalidateSprite();
        _wiggleTimer = WiggleSeconds;   // MVP 5: tanaman bergoyang senang
        return true;
    }

    // MVP 5: tanaman bergoyang sebentar setelah disiram
    private const float WiggleSeconds = 0.5f;
    private float _wiggleTimer;

    public override void Update(float deltaSeconds)
    {
        _wiggleTimer = Math.Max(0, _wiggleTimer - deltaSeconds);
    }

    public override void Draw(Graphics g, IAssetProvider assets)
    {
        base.Draw(g, assets);                                 // gambar tanah
        if (Crop == null)
            return;

        RectangleF bounds = SpriteBounds;
        if (_wiggleTimer > 0)
        {
            // condong kiri-kanan dari pangkal batang
            float angle = MathF.Sin(_wiggleTimer * 30f) * 8f * (_wiggleTimer / WiggleSeconds);
            GraphicsState state = g.Save();
            g.TranslateTransform(bounds.X + 8, bounds.Bottom - 2);
            g.RotateTransform(angle);
            assets.DrawSprite(g, Crop.SpriteKey, new RectangleF(-8, -bounds.Height + 2, bounds.Width, bounds.Height));
            g.Restore(state);
        }
        else
        {
            assets.DrawSprite(g, Crop.SpriteKey, bounds);     // gambar tanaman di atasnya
        }

        DrawHazard(g, assets);
    }

    /// <summary>Tanda hama: sprite hama kecil di atas tanaman + titik merah kalau hasil sudah berkurang.</summary>
    private void DrawHazard(Graphics g, IAssetProvider assets)
    {
        if (!HasHazard)
            return;

        RectangleF bounds = SpriteBounds;
        float bob = MathF.Sin(Environment.TickCount64 / 260f + Tile.X) * 1.2f;

        // Sprite hama (dibuat Tools/generate_assets.py): 12x10 piksel, x3 skala
        var pestRect = new RectangleF(bounds.X + 2, bounds.Y - 5 + bob, 12, 10);
        assets.DrawSprite(g, "pest_" + Hazard.ToString().ToLowerInvariant(), pestRect);

        if (Penalty)
        {
            using SolidBrush red = new(Color.FromArgb(235, 210, 60, 50));
            g.FillEllipse(red, bounds.Right - 7, bounds.Y - 3 + bob, 9, 9);
            using Pen ring = new(Color.FromArgb(230, 255, 240, 235), 1);
            g.DrawEllipse(ring, bounds.Right - 7, bounds.Y - 3 + bob, 9, 9);
        }
    }

    // ---------- MVP 5: simpan & muat ----------

    public ObjectSave ToSave()
    {
        ObjectSave save = new("hoedirt", Tile, Crop?.Data.Id ?? "");
        save.Data["watered"] = IsWatered ? 1 : 0;
        save.Data["hazard"] = (int)Hazard;
        save.Data["hazardDays"] = HazardDays;
        save.Data["penalty"] = Penalty ? 1 : 0;
        if (Crop != null)
        {
            save.Data["days"] = Crop.DaysGrown;
            save.Data["dead"] = Crop.IsDead ? 1 : 0;
        }
        return save;
    }

    /// <summary>Kembalikan keadaan dari file simpanan.</summary>
    public void Restore(bool watered, Crop? crop, HazardKind hazard = HazardKind.None,
                        int hazardDays = 0, bool penalty = false)
    {
        IsWatered = watered;
        Crop = crop;
        Hazard = hazard;
        HazardDays = hazardDays;
        Penalty = penalty;
    }

    // ---------- IInteractable: tangkap hama / panen ----------

    public bool CanInteract(Player player) =>
        Crop != null && (HasHazard || Crop.IsReady || Crop.IsDead);

    public string InteractLabel(Player player)
    {
        if (Crop is { IsDead: true }) return "Bersihkan";
        if (HasHazard) return $"Tangkap {HazardInfo.Name(Hazard)}";
        return Penalty ? "Panen (hasil -1)" : "Panen";
    }

    public ActionResult Interact(ActionContext context)
    {
        if (Crop == null)
            return ActionResult.None();

        if (Crop.IsDead)
        {
            Crop = null;
            return ActionResult.Ok("Tanaman mati dibersihkan.");
        }

        IGameServices services = context.Services;

        // 1. Hama harus ditangani dulu (event, selalu muncul)
        if (HasHazard)
        {
            if (services.RunPestLesson(() => ClearHazard(context)))
                return ActionResult.Defer();
            ClearHazard(context);
            return ActionResult.None();
        }

        if (!Crop.IsReady)
            return ActionResult.Fail("Belum siap dipanen.");

        // 2. Panen: minigame sesuai tanaman (sekali saja), lalu hasil masuk tas
        Item produce = services.Items.Create(Crop.Data.Id);
        int amount = HarvestAmount(services);
        if (amount > 0 && !context.User.Inventory.CanFit(produce, amount))
            return ActionResult.Fail("Tas penuh!");

        if (services.RunHarvestLesson(Crop.Data, () => DoHarvest(context, produce, amount)))
            return ActionResult.Defer();

        DoHarvest(context, produce, amount);
        return ActionResult.None();
    }

    /// <summary>1 buah + bonus tingkat (Muda ke atas) - 1 kalau hama dibiarkan.</summary>
    private int HarvestAmount(IGameServices services)
    {
        int bonus = services.Lessons.Grade >= 2 ? 1 : 0;
        int penalty = Penalty ? 1 : 0;
        return Math.Max(0, 1 + bonus - penalty);
    }

    private void ClearHazard(ActionContext context)
    {
        HazardKind handled = Hazard;
        Hazard = HazardKind.None;
        HazardDays = 0;
        context.Services.SpawnEffect(EffectKind.Leaves, Center);
        context.Services.PlaySound("good");
        context.Services.Notify($"{HazardInfo.Name(handled)} sudah ditangani.", true);
    }

    /// <summary>Bagian yang benar-benar mengubah dunia. Dipanggil setelah minigame selesai.</summary>
    private void DoHarvest(ActionContext context, Item produce, int amount)
    {
        if (Crop == null)
            return;

        bool penalized = Penalty;
        Penalty = false;

        if (amount > 0)
        {
            int left = context.User.Inventory.TryAdd(produce, amount);
            int got = amount - left;
            if (got > 0)
            {
                context.Services.ShowPickup(produce, got, new Vector2(Center.X, Position.Y));
                context.Services.PlaySound("harvest");
            }
            if (left > 0)
                context.Services.Notify("Tas penuh, sebagian hasil tertinggal.", false);
            else if (got > 1)
            {
                context.Services.Notify($"Panen {got} buah sekaligus!", true);
                context.Services.PlaySound("coin");
            }
        }

        if (penalized)
            context.Services.Notify(amount > 0
                ? "Hasil berkurang 1 karena hama dibiarkan."
                : "Semua hasil habis dimakan hama. Rawat lagi, ya.", false);

        context.Services.SpawnEffect(EffectKind.Leaves, Center);

        if (Crop == null)
            return;

        // Tanaman berbuah ulang tetap hidup; yang sekali panen langsung hilang
        if (Crop.Data.Regrows)
            Crop.AfterHarvest();
        else
            Crop = null;
    }

    // ---------- IDayUpdatable: tumbuh tiap pagi ----------

    public void OnNewDay(GameDate newDate)
    {
        if (Crop != null && !Crop.IsDead)
        {
            Crop.AdvanceDay(IsWatered, newDate.Season);   // tumbuh berdasarkan siraman KEMARIN

            if (HasHazard)
            {
                HazardDays++;
                if (HazardDays >= GameConfig.PestPenaltyDays && !Penalty)
                {
                    Penalty = true;
                    Location?.Services?.Notify(
                        $"{HazardInfo.Name(Hazard)} dibiarkan {HazardDays} hari: panen berikutnya berkurang 1 buah.", false);
                }
            }
            else if (Crop.DaysGrown >= 1 && Crop.Data.Hazard != HazardKind.None
                     && Random.Shared.Next(100) < GameConfig.PestChancePercent)
            {
                Hazard = Crop.Data.Hazard;
                HazardDays = 0;
                Location?.Services?.Notify(
                    $"Ada {HazardInfo.Name(Hazard)} di ladang! Tekan X di tanaman untuk menanganinya.", false);
            }
        }

        IsWatered = false;                              // lalu tanah kering lagi
        InvalidateSprite();
    }
}
