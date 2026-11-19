namespace FarmGame.Items;

/// <summary>Satu tumpukan barang: jenis item + jumlah. Jumlah selalu 1..MaxStack.</summary>
public class ItemStack
{
    public Item Item { get; }
    public int Quantity { get; private set; }   // hanya berubah lewat Add / Remove

    public int SpaceLeft => Item.MaxStack - Quantity;
    public bool IsEmpty => Quantity <= 0;
    public int TotalSellValue => Item.GetSellPrice() * Quantity;

    public ItemStack(Item item, int quantity)
    {
        Item = item;
        Quantity = quantity;
    }

    public bool CanMerge(Item other) => Item.IsSameKind(other) && SpaceLeft > 0;

    /// <summary>Tambah sebanyak mungkin. Mengembalikan SISA yang tidak muat.</summary>
    public int Add(int amount)
    {
        int added = Math.Min(amount, SpaceLeft);
        Quantity += added;
        return amount - added;
    }

    /// <summary>Kurangi. Mengembalikan jumlah yang benar-benar diambil.</summary>
    public int Remove(int amount)
    {
        int removed = Math.Min(amount, Quantity);
        Quantity -= removed;
        return removed;
    }
}
