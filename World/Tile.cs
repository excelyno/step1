namespace FarmGame.World;

/// <summary>Satu petak di map. Tahu posisinya, jenisnya, dan apakah bisa dilewati.</summary>
public class Tile
{
    public int X { get; }
    public int Y { get; }
    public TileType Type { get; private set; }   // hanya bisa diubah lewat ChangeType

    public Tile(int x, int y, TileType type)
    {
        X = x;
        Y = y;
        Type = type;
    }

    public bool IsWalkable => Type is TileType.Grass or TileType.Dirt or TileType.Path or TileType.Floor
                                   or TileType.Bridge or TileType.Cobble;

    /// <summary>MVP 1: hanya tanah (Dirt) yang bisa dicangkul.</summary>
    public bool IsTillable => Type == TileType.Dirt;

    /// <summary>Mengubah jenis tile (MVP 1: tanah dicangkul).</summary>
    public void ChangeType(TileType newType) => Type = newType;
}
