namespace FarmGame.World;

/// <summary>Jenis petak (tile) di map.</summary>
public enum TileType
{
    Void,       // di luar dunia (hitam), tidak bisa dilewati
    Grass,      // rumput
    Dirt,       // tanah (MVP 1: bisa dicangkul)
    Path,       // jalan setapak
    Water,      // air, tidak bisa dilewati (MVP 3: tempat memancing)
    Fence,      // pagar, tidak bisa dilewati
    Building,   // dinding bangunan, tidak bisa dilewati
    Floor,      // MVP 3: lantai kayu di dalam rumah
    Wall,       // MVP 3: dinding dalam rumah, tidak bisa dilewati
    Bridge,     // MVP 5: jembatan / dermaga kayu di atas air (bisa dilewati, bisa memancing dari sini)
    Cobble      // MVP 5: batu alun-alun
}
