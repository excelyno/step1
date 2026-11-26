namespace FarmGame.Shops;

/// <summary>
/// Toko bibit. Isinya dibuat otomatis dari crops.json: hanya bibit yang bisa tumbuh di musim ini.
/// Tambah tanaman di JSON = otomatis ikut dijual, tanpa ubah kode.
/// </summary>
public class SeedShop : Shop
{
    private readonly CropDatabase _crops;
    private readonly ItemRegistry _items;

    public SeedShop(CropDatabase crops, ItemRegistry items)
        : base("Toko Bibit", "Bu Sari", 9 * 60, 17 * 60)
    {
        _crops = crops;
        _items = items;
    }

    public override List<ShopEntry> GetStock(GameDate date)
    {
        List<ShopEntry> stock = new();
        foreach (CropData crop in _crops.All)
        {
            if (crop.CanGrowIn(date.Season))
                stock.Add(new ShopEntry(_items.Create(crop.SeedId), crop.SeedPrice, $"Panen {crop.DaysToMature} hari"));
        }
        return stock;
    }
}
