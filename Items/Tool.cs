namespace FarmGame.Items;

/// <summary>
/// Kelas induk semua alat. Memakai pola TEMPLATE METHOD:
/// Use() menetapkan urutan yang selalu sama (cek daya tahan -> cek energi -> efek -> bayar energi -> pakai daya tahan),
/// kelas turunan cukup mengisi ApplyEffect() dan biaya energinya.
///
/// MVP 8: alat punya DAYA TAHAN (durability). Setiap pemakaian yang berhasil memakai 1 poin;
/// habis = alat hancur dan hilang dari tas (realistis; harus beli lagi di toko Bu Sari).
/// </summary>
public abstract class Tool : Item, IUsable
{
    protected Tool(string id, string name, string description, int maxDurability)
        : base(id, name, description, 0)
    {
        MaxDurability = maxDurability;
        Durability = maxDurability;
    }

    public override ItemCategory Category => ItemCategory.Tool;
    public override int MaxStack => 1;            // alat tidak bisa ditumpuk
    public override bool IsSellable => false;     // alat tidak bisa dijual

    /// <summary>Biaya energi sekali pakai. WAJIB diisi turunan.</summary>
    public abstract int EnergyCost { get; }

    // ---------- MVP 8: daya tahan ----------

    /// <summary>Banyak pemakaian sejak baru. WAJIB diisi turunan (dari toko).</summary>
    public int MaxDurability { get; }

    /// <summary>Sisa pemakaian. 0 = hancur.</summary>
    public int Durability { get; private set; }

    public bool IsBroken => Durability <= 0;

    /// <summary>Untuk bar daya tahan di toolbar (0..1).</summary>
    public float DurabilityRatio => MaxDurability <= 0 ? 0f : (float)Durability / MaxDurability;

    /// <summary>Kurangi daya tahan 1 pemakaian.</summary>
    private void SpendDurability() => Durability = Math.Max(0, Durability - 1);

    /// <summary>Dari file simpanan.</summary>
    public void RestoreDurability(int amount) => Durability = Math.Clamp(amount, 0, MaxDurability);

    /// <summary>Turunan boleh menolak memakai daya tahan (mis. penyiram saat mengisi air).</summary>
    protected virtual bool ShouldSpendDurability(ActionContext context) => true;

    public bool ConsumesOnUse => false;           // alat tidak habis dipakai (daya tahannya yang berkurang)

    /// <summary>Bar daya tahan di toolbar (penyiram menimpanya dengan bar air).</summary>
    public override float? MeterRatio => DurabilityRatio;

    /// <summary>Bar daya tahan di toolbar: hijau -> kuning -> merah sesuai sisa pemakaian.</summary>
    public override Color MeterColor
    {
        get
        {
            float r = DurabilityRatio;
            if (r > 0.5f) return Color.ForestGreen;
            if (r > 0.2f) return Color.Goldenrod;
            return Color.OrangeRed;
        }
    }

    /// <summary>Urutan tetap untuk SEMUA alat. Turunan tidak bisa mengubah urutannya.</summary>
    public ActionResult Use(ActionContext context)
    {
        // 1. Alat hancur tidak bisa dipakai lagi (harus beli baru di toko)
        if (IsBroken)
            return ActionResult.Fail($"{Name} sudah hancur. Beli yang baru di toko Bu Sari.");

        int cost = GetEnergyCost(context);

        // 2. Cek energi cukup
        if (!context.User.Stamina.CanSpend(cost))
            return ActionResult.Fail("Kamu terlalu lelah. Istirahatlah.");

        // 3. Jalankan efek khas alat
        ActionResult result = ApplyEffect(context);

        // 4. Energi hanya berkurang kalau berhasil
        if (result.Success && cost > 0)
            context.User.Stamina.Spend(cost);

        // 5. MVP 8: daya tahan berkurang 1 kalau aksi benar-benar terjadi.
        //    Habis = alat HANCUR dan langsung hilang dari tas (harus beli lagi di toko).
        if (result.Success && !result.Deferred && ShouldSpendDurability(context))
        {
            SpendDurability();
            if (IsBroken)
                context.Services.BreakTool(this);
            else if (Durability <= Math.Max(1, MaxDurability / 5))
                context.Services.Notify($"{Name} hampir hancur ({Durability} pemakaian lagi).", false);
        }

        // 6. MVP 4: efek partikel + suara di tile sasaran
        if (result.Success)
        {
            EffectKind effect = GetUseEffect(context);
            context.Services.SpawnEffect(effect, TileCenter(context.TargetTile));
            PlaySoundFor(context, effect);
        }

        return result;
    }

    /// <summary>Suara khas alat. Diputar hanya kalau aksi benar-benar terjadi (bukan saat minigame tertunda).</summary>
    private void PlaySoundFor(ActionContext context, EffectKind effect)
    {
        // Pancing punya layar sendiri; suaranya di FishingState
        if (this is FishingRod)
            return;

        switch (effect)
        {
            case EffectKind.Dirt: context.Services.PlaySound("hoe"); break;
            case EffectKind.Water: context.Services.PlaySound("water"); break;
            case EffectKind.Splash: context.Services.PlaySound("water"); break;
            case EffectKind.WoodChips: context.Services.PlaySound("wood"); break;
            case EffectKind.StoneChips: context.Services.PlaySound("stone"); break;
            default: break;
        }
    }

    /// <summary>MVP 4: partikel saat berhasil dipakai. Default: tidak ada.</summary>
    protected virtual EffectKind GetUseEffect(ActionContext context) => EffectKind.None;

    public static Vector2 TileCenter(Point tile) =>
        new(tile.X * GameConfig.TileSize + GameConfig.TileSize / 2f, tile.Y * GameConfig.TileSize + GameConfig.TileSize / 2f);

    /// <summary>Default = EnergyCost. Boleh ditimpa kalau biayanya tergantung situasi.</summary>
    protected virtual int GetEnergyCost(ActionContext context) => EnergyCost;

    /// <summary>Efek khas alat. WAJIB ditulis turunan.</summary>
    protected abstract ActionResult ApplyEffect(ActionContext context);
}
