namespace FarmGame.Items;

/// <summary>
/// Tas pemain. MVP 1: 12 slot sekaligus jadi toolbar. Slot kosong = null.
/// Isi tas hanya bisa diubah lewat method di kelas ini.
/// </summary>
public class Inventory
{
    private readonly ItemStack?[] _slots;

    public int Capacity => _slots.Length;
    public int SelectedIndex { get; private set; }
    public ItemStack? SelectedStack => _slots[SelectedIndex];
    public Item? SelectedItem => SelectedStack?.Item;

    public Inventory(int capacity)
    {
        _slots = new ItemStack?[capacity];
    }

    public ItemStack? GetSlot(int index) => _slots[index];

    public void Select(int index)
    {
        if (index >= 0 && index < Capacity)
            SelectedIndex = index;
    }

    /// <summary>Geser pilihan slot (+1 ke kanan, -1 ke kiri). Di ujung berputar ke sisi lain.</summary>
    public void SelectNext(int direction)
    {
        SelectedIndex = (SelectedIndex + direction + Capacity) % Capacity;
    }

    /// <summary>
    /// Masukkan barang. Isi dulu tumpukan sejenis, baru slot kosong.
    /// Mengembalikan SISA yang tidak muat (0 = semua masuk).
    /// </summary>
    public int TryAdd(Item item, int quantity = 1)
    {
        int remaining = quantity;

        // 1. Gabung ke tumpukan yang sejenis
        foreach (ItemStack? stack in _slots)
        {
            if (remaining == 0) break;
            if (stack != null && stack.CanMerge(item))
                remaining = stack.Add(remaining);
        }

        // 2. Sisanya ke slot kosong dari kiri
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

    /// <summary>Kurangi isi slot. Kalau habis, slot jadi kosong.</summary>
    public void RemoveFromSlot(int index, int quantity)
    {
        ItemStack? stack = _slots[index];
        if (stack == null)
            return;

        stack.Remove(quantity);
        if (stack.IsEmpty)
            _slots[index] = null;
    }

    /// <summary>Ambil seluruh isi slot (misalnya untuk dimasukkan ke kotak pengiriman).</summary>
    public ItemStack? TakeSlot(int index)
    {
        ItemStack? stack = _slots[index];
        _slots[index] = null;
        return stack;
    }
}
