namespace FarmGame.Items;

/// <summary>Ikan hasil memancing (MVP 3): bisa dijual dan dimakan. Datanya dari fish.json.</summary>
public class Fish : Food
{
    public FishData Data { get; }

    public Fish(FishData data)
        : base(data.Id, data.Name, $"Ikan segar. Pulihkan {data.EnergyRestore} energi kalau dimakan.", data.SellPrice)
    {
        Data = data;
    }

    public override ItemCategory Category => ItemCategory.Fish;

    public override int EnergyRestore => Data.EnergyRestore;
}
