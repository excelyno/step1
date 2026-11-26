namespace FarmGame.Items;

/// <summary>
/// Barang liar yang dipungut dari padang (MVP 5): jamur, beri hutan, bunga liar.
/// Bisa dimakan (energi kecil), dijual, atau dijadikan hadiah untuk warga.
/// </summary>
public class Forageable : Food
{
    private readonly int _energy;

    public Forageable(string id, string name, string description, int basePrice, int energy)
        : base(id, name, description, basePrice)
    {
        _energy = energy;
    }

    public override ItemCategory Category => ItemCategory.Forage;
    public override int EnergyRestore => _energy;
}
