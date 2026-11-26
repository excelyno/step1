namespace FarmGame.Items;

/// <summary>Kategori barang. Dipakai juga sebagai awalan nama sprite ("tool_hoe").</summary>
public enum ItemCategory
{
    Tool,       // alat: cangkul, penyiram, kapak, beliung, pancing
    Seed,       // bibit
    Produce,    // hasil panen
    Resource,   // MVP 3: bahan (kayu, batu)
    Fish,       // MVP 3: ikan
    Forage      // MVP 5: barang liar (jamur, beri, bunga liar)
}
