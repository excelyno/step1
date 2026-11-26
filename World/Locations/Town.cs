namespace FarmGame.World.Locations;

/// <summary>
/// Desa Tandur (MVP 5: digambar ulang, 52x40 tile).
/// Jalan dari kebun turun ke alun-alun batu dengan air mancur dan bangku. Di utara alun-alun ada toko bibit
/// (Bu Sari berjaga di lapak depan toko), rumah Pak Darto, dan rumah bata kecil. Di selatan, sungai lebar
/// dengan jembatan dan dermaga tempat Raka memancing. Di seberang sungai: padang tempat barang liar tumbuh.
/// Koordinat bangunan di sini HARUS sama dengan huruf B di town.txt (dicek oleh AddBuilding).
/// </summary>
public class Town : GameLocation
{
    public const string LocationName = "Town";

    public static readonly Point ToFarmTile = new(26, 0);     // ujung jalan di tepi atas
    public static readonly Point FromFarmTile = new(26, 1);   // berdiri di sini saat datang dari kebun

    private static readonly Point ShopTile = new(15, 7);          // toko 8x4 tile
    private static readonly Point CounterTile = new(17, 12);      // lapak 4 tile di depan toko
    private static readonly Point ShopkeeperTile = new(18, 11);   // Bu Sari berdiri di antara tile 18 dan 19
    private static readonly Point RakaTile = new(38, 29);         // ujung dermaga

    private static readonly Point[] LampTiles =
    {
        new(25, 12), new(27, 12),   // gerbang alun-alun dari utara
        new(12, 13), new(40, 13),   // pojok atas alun-alun
        new(10, 23), new(42, 23),   // pojok bawah alun-alun
        new(25, 24), new(27, 24),   // jalan ke jembatan
    };

    private static readonly Point[] BenchTiles = { new(21, 15), new(30, 15), new(21, 21), new(30, 21) };

    private static readonly Rectangle ForageArea = new(3, 32, 46, 4);   // padang di seberang sungai
    private const int MaxForage = 5;
    private readonly Random _forageRng = new();

    public Town(Shop seedShop, NpcDatabase npcs) : base(LocationName, "town.txt")
    {
        // Bangunan dan pintunya
        AddBuilding(Building.Shop(ShopTile));
        AddBuilding(Building.BlueHouse(new Point(30, 6)));
        AddBuilding(Building.RedHouse(new Point(41, 7)));
        AddBuilding(Building.RedHouse(new Point(4, 15)));
        Add(new LockedDoor(new Point(33, 9), "Rumah Pak Darto. Ia sedang jalan-jalan di alun-alun."));
        Add(new LockedDoor(new Point(43, 10), "Rumah Raka. Pasti ia sedang memancing di dermaga."));
        Add(new LockedDoor(new Point(6, 18), "Rumahnya kosong. Ada tulisan: \"Dijual. Tanya Pak Darto.\""));

        // Lapak toko: penjual berdiri di belakang meja; quest-nya lewat meja
        Npc shopkeeper = CreateShopkeeper(npcs.Get("sari"));
        Add(shopkeeper);
        Add(new ShopCounter(CounterTile, 4, seedShop, shopkeeper));
        Add(new Decoration(new Point(23, 9), "barrel", new Size(1, 1), extraHeight: 4, collision: new RectangleF(2, 4, 12, 11)));
        Add(new Decoration(new Point(23, 10), "crate", new Size(1, 1), extraHeight: 2, collision: new RectangleF(1, 3, 14, 12)));
        Add(new Decoration(new Point(14, 10), "crate", new Size(1, 1), extraHeight: 2, collision: new RectangleF(1, 3, 14, 12)));
        Add(new Decoration(new Point(14, 9), "barrel", new Size(1, 1), extraHeight: 4, collision: new RectangleF(2, 4, 12, 11)));

        // Alun-alun
        Add(new Fountain(new Point(25, 17)));
        foreach (Point tile in BenchTiles)
            Add(new Bench(tile));
        Add(new NoticeBoard(new Point(23, 12)));

        // Warga lain
        Add(CreateVillager(npcs.Get("darto")));
        Npc raka = new(npcs.Get("raka"), facing: Direction.Down);   // menghadap sungai
        raka.PlaceAt(GetPositionOnTile(raka, RakaTile));
        Add(raka);

        // Papan petunjuk
        Add(new Sign(new Point(27, 5), "Utara: Kebunmu. Selatan: Alun-alun Desa Tandur."));
        Add(new Sign(new Point(14, 12), $"{seedShop.Name} {seedShop.OwnerName}. Buka {seedShop.HoursText}."));
        Add(new Sign(new Point(37, 27), "Dermaga Sungai Tandur. Katanya ada ikan besar di sini..."));

        foreach (Point tile in LampTiles)
            Add(new LampPost(tile));

        AddWarp(ToFarmTile, Farm.LocationName, Farm.FromTownTile);
        AddButterflies(3, seed: 11);
        int size = GameConfig.TileSize;
        AddBirds(5, new RectangleF(13 * size, 14 * size, 26 * size, 8 * size), seed: 9);

        for (int i = 0; i < 3; i++)
            SpawnForage();
    }

    public override bool IsOutdoors => true;

    public override void OnNewDay(GameDate newDate)
    {
        base.OnNewDay(newDate);
        SpawnForage();
    }

    private void SpawnForage()
    {
        if (Objects.OfType<Forage>().Count() >= MaxForage)
            return;
        Point? tile = FindFreeGrassTile(ForageArea, _forageRng);
        if (tile != null)
            Add(new Forage(tile.Value, Forage.ItemIds[_forageRng.Next(Forage.ItemIds.Length)]));
    }

    /// <summary>Penjual berdiri di belakang meja, tepat di tengah (antara 2 tile).</summary>
    private Npc CreateShopkeeper(NpcData data)
    {
        Npc shopkeeper = new(data);
        Vector2 onTile = GetPositionOnTile(shopkeeper, ShopkeeperTile);
        shopkeeper.PlaceAt(onTile + new Vector2(GameConfig.TileSize / 2f, 0));
        return shopkeeper;
    }

    /// <summary>Warga yang jalan-jalan di alun-alun.</summary>
    private Npc CreateVillager(NpcData data)
    {
        int size = GameConfig.TileSize;
        RectangleF plaza = new(13 * size, 14 * size, 27 * size, 9 * size);   // alun-alun (tile 13..39, 14..22)

        Npc villager = new(data, plaza);
        villager.PlaceAt(GetPositionOnTile(villager, new Point(17, 19)));
        return villager;
    }
}
