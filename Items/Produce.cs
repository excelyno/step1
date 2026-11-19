namespace FarmGame.Items;

/// <summary>Hasil panen: bisa dijual (karena Item) DAN bisa dimakan (karena IEdible).</summary>
public class Produce : Item, IEdible
{
    public CropData Data { get; }

    public Produce(CropData data)
        : base(data.Id, data.Name, $"Hasil panen. Pulihkan {data.EnergyRestore} energi kalau dimakan.", data.SellPrice)
    {
        Data = data;
    }

    public override ItemCategory Category => ItemCategory.Produce;

    public int EnergyRestore => Data.EnergyRestore;

    public ActionResult Eat(Player eater)
    {
        if (eater.Stamina.Current >= eater.Stamina.Max)
            return ActionResult.Fail("Energimu sudah penuh.");

        eater.Stamina.Restore(EnergyRestore);
        return ActionResult.Ok($"Makan {Name}. +{EnergyRestore} energi");
    }
}
