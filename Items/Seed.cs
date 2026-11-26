namespace FarmGame.Items;

/// <summary>
/// Bibit. SATU kelas untuk semua jenis bibit; bedanya hanya di CropData.
/// Ditanam ke HoeDirt yang kosong, kalau musimnya cocok.
/// </summary>
public class Seed : Item, IUsable
{
    public CropData Data { get; }

    public Seed(CropData data)
        : base(data.SeedId, data.SeedName,
               $"Tanam di tanah yang dicangkul. Panen dalam {data.DaysToMature} hari.",
               data.SeedPrice / 2)   // bibit dijual setengah harga beli
    {
        Data = data;
    }

    public override ItemCategory Category => ItemCategory.Seed;

    public bool ConsumesOnUse => true;   // bibit berkurang 1 setiap ditanam

    public ActionResult Use(ActionContext context)
    {
        if (context.Location.GetObjectAt(context.TargetTile) is not HoeDirt dirt)
            return ActionResult.Fail("Bibit harus ditanam di tanah yang sudah dicangkul.");

        if (dirt.HasCrop)
            return ActionResult.Fail("Sudah ada tanaman di sini.");

        if (!Data.CanGrowIn(context.Services.Date.Season))
            return ActionResult.Fail($"{Data.Name} tidak bisa tumbuh di musim ini.");

        dirt.Plant(new Crop(Data));
        context.Services.SpawnEffect(EffectKind.Dirt, Tool.TileCenter(context.TargetTile));
        context.Services.PlaySound("plant");
        return ActionResult.Ok();
    }
}
