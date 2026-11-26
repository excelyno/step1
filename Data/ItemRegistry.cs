namespace FarmGame.Data;

/// <summary>
/// "Pabrik" item: membuat objek Item dari Id teks.
/// Kode lain cukup panggil Create("parsnip_seeds") tanpa perlu tahu kelas konkretnya.
/// </summary>
public class ItemRegistry
{
    private readonly CropDatabase _crops;
    private readonly FishDatabase _fish;

    public ItemRegistry(CropDatabase crops, FishDatabase fish)
    {
        _crops = crops;
        _fish = fish;
    }

    /// <summary>Selalu membuat objek BARU (penting untuk alat yang punya isi, misalnya air penyiram).</summary>
    public Item Create(string id)
    {
        // Alat
        if (id == "hoe") return new Hoe();
        if (id == "watering_can") return new WateringCan();
        if (id == "axe") return new Axe();
        if (id == "pickaxe") return new Pickaxe();
        if (id == "fishing_rod") return new FishingRod();

        // Bahan
        if (id == "wood") return new Resource("wood", "Kayu", "Bahan bangunan dari pohon.", 2);
        if (id == "stone") return new Resource("stone", "Batu", "Bahan bangunan dari bebatuan.", 2);

        // MVP 5: barang liar dari padang
        if (id == "mushroom") return new Forageable("mushroom", "Jamur Kuping", "Tumbuh di tanah lembap dekat pohon.", 35, 25);
        if (id == "berries") return new Forageable("berries", "Beri Hutan", "Manis dan sedikit asam. Burung juga suka.", 25, 15);
        if (id == "wildflower") return new Forageable("wildflower", "Bunga Liar", "Bunga kuning cerah dari padang. Cocok jadi hadiah.", 30, 5);

        // Bibit dan hasil panen: dibuat otomatis dari crops.json
        foreach (CropData crop in _crops.All)
        {
            if (id == crop.SeedId) return new Seed(crop);   // "parsnip_seeds"
            if (id == crop.Id) return new Produce(crop);    // "parsnip"
        }

        // Ikan: dari fish.json
        foreach (FishData fish in _fish.All)
        {
            if (id == fish.Id) return new Fish(fish);
        }

        throw new GameDataException($"Item '{id}' tidak terdaftar.");
    }
}
