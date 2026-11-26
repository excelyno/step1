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

    /// <summary>Huruf yang berarti hiasan (bukan jenis tile). MVP 3: w = ranting, p = kerikil. MVP 5: Y = cemara.</summary>
    public static bool IsMarker(char c) => "TYbRg*Lwp".Contains(c);

    /// <summary>Jenis tile di bawah hiasan: teratai di air, ranting & kerikil di tanah, sisanya rumput.</summary>
    public static TileType GroundUnder(char symbol)
    {
        if (symbol == 'L') return TileType.Water;
        if (symbol == 'w' || symbol == 'p') return TileType.Dirt;
        return TileType.Grass;
    }
}
