namespace FarmGame.Social;

/// <summary>Seberapa suka warga pada sebuah hadiah.</summary>
public enum GiftTaste
{
    Love,
    Like,
    Neutral,
    Hate
}

/// <summary>Data satu warga, dibaca dari Content/Data/npcs.json.</summary>
public class NpcData
{
    public string Id { get; set; } = "";        // "darto" (juga nama sprite "npc_darto_...")
    public string Name { get; set; } = "";      // "Pak Darto"
    public string Role { get; set; } = "";      // "Kepala Desa Tandur"
    public List<string> DailyLines { get; set; } = new();
    public List<string> HeartLines { get; set; } = new();   // kalimat khusus kalau sudah akrab
    public List<string> Loves { get; set; } = new();
    public List<string> Likes { get; set; } = new();
    public List<string> Hates { get; set; } = new();

    public GiftTaste TasteOf(Item item)
    {
        if (Loves.Any(item.Matches)) return GiftTaste.Love;
        if (Hates.Any(item.Matches)) return GiftTaste.Hate;
        if (Likes.Any(item.Matches)) return GiftTaste.Like;
        return GiftTaste.Neutral;
    }
}
