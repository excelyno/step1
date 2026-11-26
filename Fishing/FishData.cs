namespace FarmGame.Fishing;

/// <summary>Data satu jenis ikan, dibaca dari Content/Data/fish.json. Ikan baru = baris JSON baru.</summary>
public class FishData
{
    public string Id { get; set; } = "";            // "carp" (juga Id item, sprite "fish_carp")
    public string Name { get; set; } = "";          // "Ikan Mas"
    public int SellPrice { get; set; }
    public int EnergyRestore { get; set; }
    public int Difficulty { get; set; }             // 1 (gampang) .. 5 (susah)
    public List<Season> Seasons { get; set; } = new();

    public bool IsAvailableIn(Season season) => Seasons.Contains(season);
}
