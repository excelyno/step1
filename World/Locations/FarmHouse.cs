namespace FarmGame.World.Locations;

/// <summary>
/// Dalam rumah pemain (MVP 3): kasur untuk tidur, perapian, meja, karpet.
/// Masuk lewat pintu rumah di kebun (X), keluar lewat keset di dinding bawah.
/// Setiap pagi pemain bangun di samping kasur.
/// </summary>
public class FarmHouse : GameLocation
{
    public const string LocationName = "FarmHouse";

    public static readonly Point BedTile = new(10, 2);         // kasur 1x2 tile
    public static readonly Point WakeUpTile = new(9, 3);       // samping kasur
    public static readonly Point EntranceTile = new(5, 7);     // berdiri di sini saat masuk dari kebun
    public static readonly Point ExitTile = new(5, 8);         // keset: injak = keluar

    public FarmHouse() : base(LocationName, "farmhouse.txt")
    {
        Add(new Bed(BedTile));
        Add(new Fireplace(new Point(5, 1)));
        Add(new Furniture(new Point(2, 1), "window", new Size(1, 1), isSolid: false));
        Add(new Furniture(new Point(8, 1), "window", new Size(1, 1), isSolid: false));
        Add(new Furniture(new Point(3, 4), "table", new Size(2, 1), extraHeight: 8));
        Add(new Furniture(new Point(1, 2), "plant", new Size(1, 1), extraHeight: 8));
        Add(new Furniture(new Point(6, 4), "rug", new Size(3, 2), isSolid: false, isFlat: true));
        Add(new Furniture(ExitTile, "doormat", new Size(1, 1), isSolid: false, isFlat: true));

        AddWarp(ExitTile, Farm.LocationName, Farm.FrontOfDoorTile);
    }

    public override bool IsOutdoors => false;
}
