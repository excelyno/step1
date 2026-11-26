namespace FarmGame.Items;

/// <summary>
/// Tas pemain. Isi & aturan penyimpanan diwarisi dari ItemContainer;
/// kelas ini hanya menambah "slot mana yang sedang dipegang" (toolbar).
/// MVP 1: 12 slot sekaligus jadi toolbar. Slot kosong = null.
/// MVP 9: bisa DIPERBESAR lewat upgrade dari Bu Sari (12 -> 18 -> 24 slot); isi tas tetap aman.
/// </summary>
public class Inventory : ItemContainer
{
    public int SelectedIndex { get; private set; }
    public ItemStack? SelectedStack => GetSlot(SelectedIndex);
    public Item? SelectedItem => SelectedStack?.Item;

    /// <summary>Level tas sekarang: 0 = standar, 1 = besar, 2 = paling besar. Untuk disimpan & cek upgrade.</summary>
    public int BackpackLevel { get; private set; }

    public Inventory(int capacity) : base(capacity)
    {
    }

    /// <summary>MVP 5: kosongkan tas lalu reset pilihan ke slot pertama.</summary>
    public override void Clear()
    {
        base.Clear();
        SelectedIndex = 0;
    }

    /// <summary>
    /// MVP 9: perbesar tas sebanyak 'extraSlots'. Isi tas tidak hilang — slot lama tetap, baru slot kosong.
    /// Ini mengganti array internal, jadi ItemContainer perlu dukungan khusus.
    /// </summary>
    public void Expand(int extraSlots, int level)
    {
        Grow(extraSlots);
        BackpackLevel = Math.Max(BackpackLevel, level);
        if (SelectedIndex >= Capacity)
            SelectedIndex = Capacity - 1;
    }

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
}
