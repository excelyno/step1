namespace FarmGame.Farming;

/// <summary>
/// Hama yang bisa menyerang tanaman. Dibaca dari crops.json ("pest": "aphid").
/// Hama TIDAK mematikan tanaman: kalau dibiarkan 2 hari, panen berkurang 1 buah.
/// </summary>
public enum HazardKind
{
    None,
    Aphid,   // kutu daun: tomat, terong
    Snail,   // bekicot: kentang, kale, labu
    Crow,    // burung: jagung
    Mold     // jamur: kembang kol
}

public static class HazardInfo
{
    public static string Name(HazardKind kind) => kind switch
    {
        HazardKind.Aphid => "kutu daun",
        HazardKind.Snail => "bekicot",
        HazardKind.Crow => "burung",
        HazardKind.Mold => "jamur",
        _ => "hama"
    };

    /// <summary>Warna tanda di atas tanaman (biar anak mudah melihat).</summary>
    public static Color DotColor(HazardKind kind) => kind switch
    {
        HazardKind.Aphid => System.Drawing.Color.FromArgb(90, 200, 90),
        HazardKind.Snail => System.Drawing.Color.FromArgb(210, 170, 110),
        HazardKind.Crow => System.Drawing.Color.FromArgb(60, 60, 80),
        HazardKind.Mold => System.Drawing.Color.FromArgb(235, 235, 235),
        _ => System.Drawing.Color.Transparent
    };
}
