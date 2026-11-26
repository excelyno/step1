namespace FarmGame.World;

/// <summary>
/// Titik pindah lokasi. Kalau pemain menginjak Tile, ia dipindah ke TargetTile di lokasi TargetLocation.
/// Lokasi tujuan disimpan sebagai NAMA (bukan objek), supaya Farm tidak perlu kenal Town dan sebaliknya.
/// </summary>
public class Warp
{
    public Point Tile { get; }
    public string TargetLocation { get; }
    public Point TargetTile { get; }

    public Warp(Point tile, string targetLocation, Point targetTile)
    {
        Tile = tile;
        TargetLocation = targetLocation;
        TargetTile = targetTile;
    }
}
