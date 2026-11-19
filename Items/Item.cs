namespace FarmGame.Items;

/// <summary>
/// Kelas induk SEMUA barang yang bisa masuk tas.
/// Item = "jenis barang". Jumlahnya disimpan terpisah di ItemStack.
/// </summary>
public abstract class Item
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public int BasePrice { get; }   // harga jual per buah (0 = tidak bisa dijual)

    /// <summary>Kategori barang. WAJIB diisi turunan.</summary>
    public abstract ItemCategory Category { get; }

    /// <summary>Maksimal jumlah dalam satu slot. Alat menimpanya menjadi 1.</summary>
    public virtual int MaxStack => 999;

    public virtual bool IsSellable => BasePrice > 0;

    /// <summary>Nama gambar, contoh "tool_hoe", "seed_parsnip_seeds", "produce_parsnip".</summary>
    public virtual string SpriteKey => Category.ToString().ToLower() + "_" + Id;

    /// <summary>Isi bar kecil di ikon (0..1), misalnya sisa air. null = tidak ada bar.</summary>
    public virtual float? MeterRatio => null;

    protected Item(string id, string name, string description, int basePrice)
    {
        Id = id;
        Name = name;
        Description = description;
        BasePrice = basePrice;
    }

    public virtual int GetSellPrice() => BasePrice;

    /// <summary>Dua item bisa ditumpuk kalau Id-nya sama.</summary>
    public bool IsSameKind(Item other) => other.Id == Id;
}
