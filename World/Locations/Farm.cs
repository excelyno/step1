namespace FarmGame.World.Locations;

/// <summary>Kebun milik pemain: tempat mencangkul, menanam, kotak kirim, dan pintu rumah.</summary>
public class Farm : GameLocation
{
    public static readonly Point DoorTile = new(12, 3);          // pintu di dinding bawah rumah
    public static readonly Point WakeUpTile = new(12, 4);        // berdiri di depan pintu saat bangun
    public static readonly Point ShippingBinTile = new(15, 4);   // kotak kirim (2 tile lebar)

    public ShippingBin ShippingBin { get; }

    public Farm() : base("Farm", "farm.txt")
    {
        ShippingBin = new ShippingBin(ShippingBinTile);
        Add(ShippingBin);
        Add(new FarmhouseDoor(DoorTile));
    }

    public override bool IsOutdoors => true;
}
