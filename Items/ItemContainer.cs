namespace FarmGame.Items;

/// <summary>
/// Wadah barang berbasis slot (array). Kelas induk tas, kotak kirim, dan peti penyimpanan nanti.
/// Aturan penyimpanan ditulis SEKALI di sini: gabung ke tumpukan sejenis dulu, baru pakai slot kosong.
/// Slot kosong = null. Isi hanya bisa diubah lewat method kelas ini (enkapsulasi).
/// </summary>
public class ItemContainer
{
    private readonly ItemStack?[] _slots;

    public int Capacity => _slots.Length;

    /// <summary>Jumlah slot yang terisi (bukan jumlah barang).</summary>
    public int UsedSlots => _slots.Count(stack => stack != null);

    public bool IsFull => UsedSlots >= Capacity;

    /// <summary>Semua tumpukan yang ada, dari slot kiri ke kanan. Slot kosong dilewati.</summary>
    public IEnumerable<ItemStack> Stacks => _slots.OfType<ItemStack>();

    /// <summary>Total nilai jual semua isi (dipakai kotak kirim untuk perkiraan pendapatan).</summary>
    public int TotalSellValue => Stacks.Sum(stack => stack.TotalSellValue);

    public ItemContainer(int capacity)
    {
        _slots = new ItemStack?[capacity];
    }

    public ItemStack? GetSlot(int index) =>
        index >= 0 && index < _slots.Length ? _slots[index] : null;

    /// <summary>Taruh tumpukan langsung di slot tertentu (dari file simpanan atau saat pindah drag).</summary>
    public void SetSlot(int index, ItemStack? stack)
    {
        if (index >= 0 && index < _slots.Length)
            _slots[index] = stack;
    }

    /// <summary>Kosongkan semua slot. Inventory menimpa method ini untuk mereset pilihan slot.</summary>
    public virtual void Clear() => Array.Clear(_slots);

    /// <summary>
    /// Masukkan barang. Isi dulu tumpukan sejenis, baru slot kosong.
    /// Mengembalikan SISA yang tidak muat (0 = semua masuk).
    /// </summary>
    public int TryAdd(Item item, int quantity = 1)
    {
        int remaining = quantity;

        foreach (ItemStack? stack in _slots)
        {
            if (remaining == 0) break;
            if (stack != null && stack.CanMerge(item))
                remaining = stack.Add(remaining);
        }

        for (int i = 0; i < Capacity && remaining > 0; i++)
        {
            if (_slots[i] == null)
            {
                int put = Math.Min(remaining, item.MaxStack);
                _slots[i] = new ItemStack(item, put);
                remaining -= put;
            }
        }

        return remaining;
    }

    /// <summary>Apakah 'quantity' buah item ini muat? Wadah tidak diubah.</summary>
    public bool CanFit(Item item, int quantity)
    {
        int space = 0;
        foreach (ItemStack? stack in _slots)
        {
            if (stack == null)
                space += item.MaxStack;
            else if (stack.CanMerge(item))
                space += stack.SpaceLeft;
        }
        return space >= quantity;
    }

    /// <summary>Jumlah semua barang yang cocok dengan pola (id atau "category:...").</summary>
    public int CountMatching(string pattern)
    {
        int total = 0;
        foreach (ItemStack? stack in _slots)
        {
            if (stack != null && stack.Item.Matches(pattern))
                total += stack.Quantity;
        }
        return total;
    }

    /// <summary>Ambil 'amount' barang yang cocok, dari slot kiri ke kanan. Cek CountMatching dulu.</summary>
    public void RemoveMatching(string pattern, int amount)
    {
        for (int i = 0; i < Capacity && amount > 0; i++)
        {
            ItemStack? stack = _slots[i];
            if (stack == null || !stack.Item.Matches(pattern))
                continue;

            int take = Math.Min(amount, stack.Quantity);
            RemoveFromSlot(i, take);
            amount -= take;
        }
    }

    /// <summary>Kurangi isi slot. Kalau habis, slot jadi kosong.</summary>
    public void RemoveFromSlot(int index, int quantity)
    {
        ItemStack? stack = GetSlot(index);
        if (stack == null)
            return;

        stack.Remove(quantity);
        if (stack.IsEmpty)
            _slots[index] = null;
    }

    /// <summary>Ambil seluruh isi slot (misalnya untuk drag & drop atau memasukkan ke kotak).</summary>
    public ItemStack? TakeSlot(int index)
    {
        if (index < 0 || index >= _slots.Length)
            return null;

        ItemStack? stack = _slots[index];
        _slots[index] = null;
        return stack;
    }
}
