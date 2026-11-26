namespace FarmGame.Items;

/// <summary>
/// MVP 9: jasa perbesar tas dari Bu Sari (3 level: 12 -> 18 -> 24 slot).
/// Bukan barang: saat "dibeli", Shop memanggil Apply() — tas pemain langsung bertambah.
/// Sprite tetap pakai ikon tas (backpack_small/backpack_large) supaya ada gambarnya di etalase.
/// </summary>
public class BackpackUpgrade : Item
{
    public int ExtraSlots { get; }
    public string Sprite { get; }

    public BackpackUpgrade(string id, string name, string description, int extraSlots, int price, string sprite)
        : base(id, name, description, price)
    {
        ExtraSlots = extraSlots;
        Sprite = sprite;
    }

    public override ItemCategory Category => ItemCategory.Tool;   // kelompok "perlengkapan"
    public override int MaxStack => 1;
    public override bool IsSellable => false;
    public override string SpriteKey => Sprite;
}
