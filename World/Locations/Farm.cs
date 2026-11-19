namespace FarmGame.World.Locations;

/// <summary>Kebun milik pemain: tempat mencangkul, menanam, kotak kirim, dan pintu rumah.</summary>
public class Farm : GameLocation
{
    public static readonly Point HouseTile = new(8, 4);          // pojok kiri-atas bangunan (5x3 tile)
    public static readonly Point DoorTile = new(10, 6);          // pintu di dinding bawah rumah
    public static readonly Point WakeUpTile = new(10, 7);        // berdiri di depan pintu saat bangun
    public static readonly Point ShippingBinTile = new(13, 7);   // kotak kirim (2 tile lebar)

    public ShippingBin ShippingBin { get; }

    public Farm() : base("Farm", "farm.txt")
    {
        Add(new Farmhouse(HouseTile));
        ShippingBin = new ShippingBin(ShippingBinTile);
        Add(ShippingBin);
        Add(new FarmhouseDoor(DoorTile));
        AddButterflies(4);
    }

    /// <summary>Kupu-kupu beterbangan di dalam kebun (tidak sampai ke hutan di pinggir).</summary>
    private void AddButterflies(int count)
    {
        int margin = 3 * GameConfig.TileSize;
        RectangleF area = new(margin, margin, Map.PixelWidth - margin * 2, Map.PixelHeight - margin * 2);
        Random rng = new(7);
        for (int i = 0; i < count; i++)
        {
            Vector2 start = new(area.X + (float)rng.NextDouble() * area.Width, area.Y + (float)rng.NextDouble() * area.Height);
            Add(new Butterfly(start, area, i % 2));
        }
    }

    public override bool IsOutdoors => true;
}
