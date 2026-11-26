namespace FarmGame.World.Locations;

/// <summary>
/// Kebun milik pemain (MVP 5: digambar ulang, 50x38 tile).
/// Rumah besar di utara, halaman depan dengan kotak surat, sumur dan kotak kirim, ladang berpagar di tengah,
/// sungai di timur yang melebar jadi kolam dengan dermaga, dan hutan di sekelilingnya.
/// Jalan setapak di selatan menuju Desa Tandur.
/// </summary>
public class Farm : GameLocation
{
    public const string LocationName = "Farm";

    public static readonly Point HouseTile = new(14, 5);         // pojok kiri-atas rumah (7x4 tile)
    public static readonly Point DoorTile = new(17, 8);          // pintu di dinding bawah rumah
    public static readonly Point FrontOfDoorTile = new(17, 9);   // berdiri di depan pintu saat keluar rumah
    public static readonly Point ShippingBinTile = new(21, 9);   // kotak kirim (2 tile lebar)
    public static readonly Point MailboxTile = new(16, 9);
    public static readonly Point WellTile = new(10, 8);          // sumur (2 tile lebar)

    // Jalan setapak di tepi bawah menuju kota
    public static readonly Point ToTownTile = new(32, 37);
    public static readonly Point FromTownTile = new(32, 36);

    // MVP 7: jalan ke timur (seberang jembatan) menuju hutan prosedural
    public static readonly Point ToForestTile = new(49, 12);
    public static readonly Point FromForestTile = new(48, 12);

    // Padang liar tempat barang liar tumbuh sendiri (dalam tile)
    private static readonly Rectangle[] ForageAreas =
    {
        new(4, 31, 27, 4),    // padang selatan, di bawah ladang
        new(41, 2, 6, 20),    // padang cemara di seberang sungai
        new(3, 2, 10, 5),     // kebun buah tua di barat laut
    };
    private const int MaxForage = 7;

    private readonly Random _forageRng = new();

    public ShippingBin ShippingBin { get; }

    public Farm() : base(LocationName, "farm.txt")
    {
        AddBuilding(Building.House(HouseTile));
        Add(new FarmhouseDoor(DoorTile));
        AddWarp(ToForestTile, Forest.LocationName, Forest.FromFarmTile);

        // Halaman depan
        ShippingBin = new ShippingBin(ShippingBinTile);
        Add(ShippingBin);
        Add(new Mailbox(MailboxTile));
        Add(new Well(WellTile));
        Add(new Decoration(new Point(21, 7), "woodpile", new Size(2, 1), extraHeight: 6, collision: new RectangleF(1, 4, 30, 11)));
        Add(new Decoration(new Point(13, 7), "barrel", new Size(1, 1), extraHeight: 4, collision: new RectangleF(2, 4, 12, 11)));
        Add(new Decoration(new Point(23, 7), "crate", new Size(1, 1), extraHeight: 2, collision: new RectangleF(1, 3, 14, 12)));
        Add(new Decoration(new Point(23, 8), "barrel", new Size(1, 1), extraHeight: 4, collision: new RectangleF(2, 4, 12, 11)));

        // Orang-orangan sawah menjaga ladang
        Add(new Decoration(new Point(12, 20), "scarecrow", new Size(1, 1), extraHeight: 16, collision: new RectangleF(5, 9, 6, 6)));
        Add(new Decoration(new Point(24, 25), "scarecrow", new Size(1, 1), extraHeight: 16, collision: new RectangleF(5, 9, 6, 6)));

        Add(new Sign(new Point(33, 13), "Selatan: Desa Tandur (toko bibit, alun-alun, dermaga sungai).\nTimur: jembatan ke padang cemara."));
        Add(new Sign(new Point(31, 27), "Kolam Kakek. Ikan suka berkumpul di ujung dermaga."));
        AddWarp(ToTownTile, Town.LocationName, Town.FromFarmTile);

        // Lampu di depan rumah, di persimpangan, dan di jalan ke kota
        Add(new LampPost(new Point(19, 9)));
        Add(new LampPost(new Point(31, 11)));
        Add(new LampPost(new Point(31, 25)));
        Add(new LampPost(new Point(33, 35)));

        AddButterflies(5, seed: 7);
        int size = GameConfig.TileSize;
        AddBirds(4, new RectangleF(6 * size, 9 * size, 24 * size, 3 * size), seed: 3);
        AddBirds(3, new RectangleF(5 * size, 31 * size, 24 * size, 3 * size), seed: 5);

        // Beberapa barang liar sudah ada di hari pertama
        for (int i = 0; i < 4; i++)
            SpawnForage();
    }

    public override bool IsOutdoors => true;

    /// <summary>Setiap pagi: barang liar baru tumbuh (sampai batas tertentu).</summary>
    public override void OnNewDay(GameDate newDate)
    {
        base.OnNewDay(newDate);
        for (int i = 0; i < 2; i++)
            SpawnForage();
    }

    private void SpawnForage()
    {
        if (Objects.OfType<Forage>().Count() >= MaxForage)
            return;
        Rectangle area = ForageAreas[_forageRng.Next(ForageAreas.Length)];
        Point? tile = FindFreeGrassTile(area, _forageRng);
        if (tile != null)
            Add(new Forage(tile.Value, Forage.ItemIds[_forageRng.Next(Forage.ItemIds.Length)]));
    }
}
