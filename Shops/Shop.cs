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
        // MVP 9: upgrade tas bukan barang — dibeli = tas langsung diperbesar
        if (entry.Item is BackpackUpgrade pack)
            return BuyBackpack(pack, buyer);

        Item item = items.Create(entry.Item.Id);   // objek baru, bukan contoh di etalase

        // Alat (MaxStack 1) tidak bisa dibeli bertumpuk: beli 5 via Shift = tetap 1 buah
        quantity = Math.Clamp(quantity, 1, item.MaxStack);
        int total = entry.Price * quantity;

        if (!buyer.Inventory.CanFit(item, quantity))
            return ActionResult.Fail("Tas penuh!");

        if (!buyer.Wallet.TrySpend(total))
            return ActionResult.Fail("Uangmu tidak cukup.");

        buyer.Inventory.TryAdd(item, quantity);
        return ActionResult.Ok($"Beli {quantity}x {item.Name} (-{total}g)");
    }

    /// <summary>MVP 9: beli upgrade tas. Cek level (tidak bisa loncat / beli dua kali), bayar, lalu perbesar.</summary>
    private ActionResult BuyBackpack(BackpackUpgrade pack, Player buyer)
    {
        Inventory inv = buyer.Inventory;
        int targetLevel = pack.Id == "backpack_l2" ? 2 : 1;

        if (inv.BackpackLevel >= targetLevel)
            return ActionResult.Fail("Tasmu sudah setingkat ini atau lebih besar.");

        // Lompatan langsung ke level 2 tanpa level 1 juga ditolak (slot harus konsisten)
        if (targetLevel == 2 && inv.Capacity < GameConfig.BackpackL1Slots)
            return ActionResult.Fail("Beli Tas Besar dulu sebelum Tas Raksasa.");

        if (!buyer.Wallet.TrySpend(pack.BasePrice))
            return ActionResult.Fail("Uangmu tidak cukup.");

        inv.Expand(pack.ExtraSlots, targetLevel);
        return ActionResult.Ok($"{pack.Name}! Tas sekarang {inv.Capacity} slot (-{pack.BasePrice}g)");
    }
}
