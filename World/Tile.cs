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

    public bool IsWalkable => Type == TileType.Grass || Type == TileType.Dirt || Type == TileType.Path;

    /// <summary>Mengubah jenis tile (MVP 1: tanah dicangkul).</summary>
    public void ChangeType(TileType newType) => Type = newType;
}
