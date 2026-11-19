namespace FarmGame.Items;

/// <summary>
/// Penyiram: menyiram HoeDirt (pakai air dan energi),
/// atau diisi ulang di tile air (gratis energi).
/// </summary>
public class WateringCan : Tool
{
    public int Capacity { get; } = GameConfig.WateringCanCapacity;
    public int WaterLeft { get; private set; }

    public WateringCan() : base("watering_can", "Penyiram", "Menyiram tanaman. Isi ulang di kolam.")
    {
        WaterLeft = Capacity;
    }

    public override int EnergyCost => 2;

    /// <summary>Bar biru di toolbar = sisa air.</summary>
    public override float? MeterRatio => (float)WaterLeft / Capacity;

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

    private bool IsFacingWater(ActionContext context)
    {
        Tile? tile = context.Location.Map.GetTile(context.TargetTile.X, context.TargetTile.Y);
        return tile != null && tile.Type == TileType.Water;
    }
}
