namespace FarmGame.Shops;

/// <summary>
/// Toko Bu Sari: bibit (dibuat otomatis dari crops.json) + alat pertanian (MVP 8).
/// Alat punya daya tahan: habis dipakai = hancur dan harus dibeli lagi di sini.
/// Tambah tanaman di JSON = otomatis ikut dijual, tanpa ubah kode.
/// </summary>
public class SeedShop : Shop
{
    private readonly CropDatabase _crops;
    private readonly ItemRegistry _items;
    private Player? _player;   // MVP 9: untuk tahu level tas di etalase (diisi setelah pemain dibuat)

    public SeedShop(CropDatabase crops, ItemRegistry items, Player? player = null)
        : base("Toko Bibit & Alat", "Bu Sari", 9 * 60, 17 * 60)
    {
        _crops = crops;
        _items = items;
        _player = player;
    }

    /// <summary>MVP 9: pemain baru dibuat setelah dunia; toko ikut tahu level tasnya.</summary>
    public void BindPlayer(Player player) => _player = player;

    public override List<ShopEntry> GetStock(GameDate date)
    {
        List<ShopEntry> stock = new();

        // MVP 9: upgrade tas (hilang dari etalase kalau level itu sudah dibeli)
        if (_player?.Inventory.BackpackLevel is < 1 or 0)
            stock.Add(new ShopEntry(_items.Create("backpack_l1"), GameConfig.BackpackL1Price,
                        "12 -> 18 slot. Isi tas tetap aman"));
        if (_player?.Inventory.BackpackLevel is 1)
            stock.Add(new ShopEntry(_items.Create("backpack_l2"), GameConfig.BackpackL2Price,
                        "18 -> 24 slot. Untuk petani sejati"));

        // MVP 8: alat pertanian. Daya tahan ditulis di Info supaya pembeli tahu betapa awetnya.
        stock.Add(new ShopEntry(_items.Create("hoe"), GameConfig.HoeDurability * 2,
                    $"Cangkul baru, daya tahan {GameConfig.HoeDurability} pemakaian"));
        stock.Add(new ShopEntry(_items.Create("watering_can"), GameConfig.CanPrice,
                    $"Daya tahan {GameConfig.CanDurability} pemakaian"));
        stock.Add(new ShopEntry(_items.Create("axe"), GameConfig.AxePrice,
                    $"Daya tahan {GameConfig.AxeDurability} pemakaian"));
        stock.Add(new ShopEntry(_items.Create("pickaxe"), GameConfig.PickaxePrice,
                    $"Daya tahan {GameConfig.PickaxeDurability} pemakaian"));
        stock.Add(new ShopEntry(_items.Create("fishing_rod"), GameConfig.RodPrice,
                    $"Daya tahan {GameConfig.RodDurability} pemakaian"));

        foreach (CropData crop in _crops.All)
        {
            if (crop.CanGrowIn(date.Season))
                stock.Add(new ShopEntry(_items.Create(crop.SeedId), crop.SeedPrice, $"Panen {crop.DaysToMature} hari"));
        }
        return stock;
    }
}
