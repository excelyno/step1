namespace FarmGame.Data;

/// <summary>
/// "Pabrik" item: membuat objek Item dari Id teks.
/// Kode lain cukup panggil Create("parsnip_seeds") tanpa perlu tahu kelas konkretnya.
/// </summary>
public class ItemRegistry
{
    private readonly CropDatabase _crops;

    public ItemRegistry(CropDatabase crops)
    {
        _crops = crops;
    }

    /// <summary>Selalu membuat objek BARU (penting untuk alat yang punya isi, misalnya air penyiram).</summary>
    public Item Create(string id)
    {
        // Alat
        if (id == "hoe") return new Hoe();
        if (id == "watering_can") return new WateringCan();

        // Bibit dan hasil panen: dibuat otomatis dari crops.json
        foreach (CropData crop in _crops.All)
        {
            if (id == crop.SeedId) return new Seed(crop);   // "parsnip_seeds"
            if (id == crop.Id) return new Produce(crop);    // "parsnip"
        }

        throw new GameDataException($"Item '{id}' tidak terdaftar.");
    }
}
