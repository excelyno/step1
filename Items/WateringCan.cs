namespace FarmGame.Items;

/// <summary>
/// Penyiram: menyiram HoeDirt (pakai air dan energi),
/// atau diisi ulang di tile air (gratis energi).
/// </summary>
public class WateringCan : Tool
{
    public int Capacity { get; } = GameConfig.WateringCanCapacity;
    public int WaterLeft { get; private set; }

    public WateringCan() : base("watering_can", "Penyiram", "Menyiram tanaman. Isi ulang di sungai, kolam, atau sumur.", GameConfig.CanDurability)
    {
        WaterLeft = Capacity;
    }

    public override int EnergyCost => 2;

    /// <summary>Isi ulang = cipratan; siram = tetesan air.</summary>
    protected override EffectKind GetUseEffect(ActionContext context) =>
        IsFacingWater(context) ? EffectKind.Splash : EffectKind.Water;

    /// <summary>Bar biru di toolbar = sisa air.</summary>
    public override float? MeterRatio => (float)WaterLeft / Capacity;

    /// <summary>Mengisi ulang air tidak memakan daya tahan; hanya menyiram yang memakai.</summary>
    protected override bool ShouldSpendDurability(ActionContext context) => !IsFacingWater(context);

    /// <summary>Mengisi air di kolam tidak memakai energi.</summary>
    protected override int GetEnergyCost(ActionContext context)
    {
        if (IsFacingWater(context))
            return 0;
        return EnergyCost;
    }

    protected override ActionResult ApplyEffect(ActionContext context)
    {
        // Menghadap air: isi ulang
        if (IsFacingWater(context))
        {
            if (WaterLeft == Capacity)
                return ActionResult.Fail("Penyiram sudah penuh.");

            WaterLeft = Capacity;
            return ActionResult.Ok("Penyiram terisi penuh.");
        }

        // Menghadap tanah: siram
        if (context.Location.GetObjectAt(context.TargetTile) is not HoeDirt dirt)
            return ActionResult.Fail("Tidak ada yang perlu disiram di sini.");

        if (WaterLeft <= 0)
            return ActionResult.Fail("Penyiram kosong. Isi ulang di kolam.");

        if (!dirt.Water())
            return ActionResult.Fail("Tanah ini sudah disiram hari ini.");

        WaterLeft--;
        return ActionResult.Ok();
    }

    /// <summary>MVP 5: isi penuh (sumur). False kalau sudah penuh.</summary>
    public bool Refill()
    {
        if (WaterLeft == Capacity)
            return false;
        WaterLeft = Capacity;
        return true;
    }

    /// <summary>MVP 5: dari file simpanan.</summary>
    public void RestoreWater(int amount) => WaterLeft = Math.Clamp(amount, 0, Capacity);

    /// <summary>Air di depan: tile air, atau sumur / air mancur (MVP 5).</summary>
    private bool IsFacingWater(ActionContext context)
    {
        if (context.Location.GetObjectAt(context.TargetTile) is IWaterSource)
            return true;
        Tile? tile = context.Location.Map.GetTile(context.TargetTile.X, context.TargetTile.Y);
        return tile != null && tile.Type == TileType.Water;
    }
}
