namespace FarmGame.Items;

/// <summary>Bahan mentah (MVP 3): kayu dari pohon, batu dari bebatuan. Bisa dijual murah.</summary>
public class Resource : Item
{
    public Resource(string id, string name, string description, int basePrice)
        : base(id, name, description, basePrice)
    {
    }

    public override ItemCategory Category => ItemCategory.Resource;
}
