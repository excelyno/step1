namespace FarmGame.Shops;

/// <summary>
/// Kelas induk semua toko. Aturan beli SAMA untuk semua toko (cek tas -> bayar -> masuk tas),
/// yang beda hanya ISI tokonya: turunan wajib mengisi GetStock().
/// MVP 2: SeedShop. Nanti: toko alat, toko hewan, dll.
/// </summary>
public abstract class Shop
{
    public string Name { get; }
    public string OwnerName { get; }
    public int OpenTime { get; }    // menit sejak 00:00, contoh 9 * 60 = 09:00
    public int CloseTime { get; }

    protected Shop(string name, string ownerName, int openTime, int closeTime)
    {
        Name = name;
        OwnerName = ownerName;
        OpenTime = openTime;
        CloseTime = closeTime;
    }

    /// <summary>Barang yang dijual hari ini (boleh beda tiap musim). WAJIB diisi turunan.</summary>
    public abstract List<ShopEntry> GetStock(GameDate date);

    public bool IsOpen(int timeOfDay) => timeOfDay >= OpenTime && timeOfDay < CloseTime;

    /// <summary>Teks jam buka, contoh "09:00 - 17:00".</summary>
    public string HoursText => $"{OpenTime / 60:00}:{OpenTime % 60:00} - {CloseTime / 60:00}:{CloseTime % 60:00}";

    /// <summary>
    /// Beli barang. Urutan penting: cek tas DULU, baru ambil uang,
    /// supaya uang tidak hilang kalau barangnya tidak muat.
    /// </summary>
    public ActionResult Buy(ShopEntry entry, int quantity, Player buyer, ItemRegistry items)
    {
        Item item = items.Create(entry.Item.Id);   // objek baru, bukan contoh di etalase
        int total = entry.Price * quantity;

        if (!buyer.Inventory.CanFit(item, quantity))
            return ActionResult.Fail("Tas penuh!");

        if (!buyer.Wallet.TrySpend(total))
            return ActionResult.Fail("Uangmu tidak cukup.");

        buyer.Inventory.TryAdd(item, quantity);
        return ActionResult.Ok($"Beli {quantity}x {item.Name} (-{total}g)");
    }
}
