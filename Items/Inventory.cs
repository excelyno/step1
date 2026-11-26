namespace FarmGame.Items;

/// <summary>
/// Tas pemain. Isi & aturan penyimpanan diwarisi dari ItemContainer;
/// kelas ini hanya menambah "slot mana yang sedang dipegang" (toolbar).
/// MVP 1: 12 slot sekaligus jadi toolbar. Slot kosong = null.
/// </summary>
public class Inventory : ItemContainer
{
    public int SelectedIndex { get; private set; }
    public ItemStack? SelectedStack => GetSlot(SelectedIndex);
    public Item? SelectedItem => SelectedStack?.Item;

    public Inventory(int capacity) : base(capacity)
    {
    }

    /// <summary>MVP 5: kosongkan tas lalu reset pilihan ke slot pertama.</summary>
    public override void Clear()
    {
        base.Clear();
        SelectedIndex = 0;
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
