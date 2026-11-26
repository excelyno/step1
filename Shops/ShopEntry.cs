namespace FarmGame.Shops;

/// <summary>Satu baris barang di toko: contoh item (untuk nama & gambar), harga, dan info singkat.</summary>
public class ShopEntry
{
    public Item Item { get; }
    public int Price { get; }
    public string Info { get; }   // contoh "Panen 4 hari"

    public ShopEntry(Item item, int price, string info)
    {
        Item = item;
        Price = price;
        Info = info;
    }
}
