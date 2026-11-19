namespace FarmGame.World;

/// <summary>Jenis petak (tile) di map.</summary>
public enum TileType
{
    Void,       // di luar dunia (hitam), tidak bisa dilewati
    Grass,      // rumput
    Dirt,       // tanah (MVP 1: bisa dicangkul)
    Path,       // jalan setapak
    Water,      // air, tidak bisa dilewati
    Fence,      // pagar, tidak bisa dilewati
    Building    // dinding bangunan, tidak bisa dilewati
}
