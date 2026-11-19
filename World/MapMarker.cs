namespace FarmGame.World;

/// <summary>Satu hiasan di file map: hurufnya apa dan di tile mana.</summary>
public class MapMarker
{
    public char Symbol { get; }
    public Point Tile { get; }

    public MapMarker(char symbol, Point tile)
    {
        Symbol = symbol;
        Tile = tile;
    }

    /// <summary>Huruf yang berarti hiasan (bukan jenis tile).</summary>
    public static bool IsMarker(char c) => "TbRg*L".Contains(c);
}
