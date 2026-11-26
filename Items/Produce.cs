namespace FarmGame.Items;

/// <summary>Hasil panen: bisa dijual (karena Item) DAN bisa dimakan (karena Food).</summary>
public class Produce : Food
{
    public CropData Data { get; }

    public Produce(CropData data)
        : base(data.Id, data.Name, $"Hasil panen. Pulihkan {data.EnergyRestore} energi kalau dimakan.", data.SellPrice)
    {
        Data = data;
    }

    public override ItemCategory Category => ItemCategory.Produce;

    public override int EnergyRestore => Data.EnergyRestore;
}
