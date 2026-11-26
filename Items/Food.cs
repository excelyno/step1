namespace FarmGame.Items;

/// <summary>
/// Induk semua barang yang bisa dimakan (hasil panen, ikan).
/// Cara makan ditulis SEKALI di sini; turunan cukup bilang berapa energinya.
/// </summary>
public abstract class Food : Item, IEdible
{
    protected Food(string id, string name, string description, int basePrice)
        : base(id, name, description, basePrice)
    {
    }

    /// <summary>Energi yang pulih kalau dimakan. WAJIB diisi turunan.</summary>
    public abstract int EnergyRestore { get; }

    public ActionResult Eat(Player eater)
    {
        if (eater.Stamina.Current >= eater.Stamina.Max)
            return ActionResult.Fail("Energimu sudah penuh.");

        eater.Stamina.Restore(EnergyRestore);
        return ActionResult.Ok($"Makan {Name}. +{EnergyRestore} energi");
    }
}
