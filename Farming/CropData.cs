namespace FarmGame.Farming;

/// <summary>
/// Data satu jenis tanaman, dibaca dari Content/Data/crops.json.
/// Menambah tanaman baru = menambah baris JSON, BUKAN membuat kelas baru.
/// </summary>
public class CropData
{
    public string Id { get; set; } = "";            // "parsnip" (juga Id item hasil panen)
    public string Name { get; set; } = "";          // "Parsnip"
    public string SeedName { get; set; } = "";      // "Bibit Parsnip"
    public int SeedPrice { get; set; }              // harga beli bibit (toko di MVP 2)
    public int SellPrice { get; set; }              // harga jual hasil panen
    public int DaysToMature { get; set; }           // berapa hari disiram sampai siap panen
    public int EnergyRestore { get; set; }          // energi kalau dimakan
    public List<Season> Seasons { get; set; } = new();

    /// <summary>Id item bibit, contoh "parsnip_seeds".</summary>
    public string SeedId => Id + "_seeds";

    public bool CanGrowIn(Season season) => Seasons.Contains(season);
}
