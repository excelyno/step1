namespace FarmGame.World.Locations;

/// <summary>
/// MVP 7: Hutan prosedural di timur kebun (dicapai lewat jembatan + jalan ke kanan).
///
/// Peta TIDAK berasal dari file, tapi dibuat programatik dari sebuah seed acak:
///   - Padat di tepi (dinding hutan), jarang di jalur, ada tanah lapang/kolam kecil.
///   - Pohon/cemara/batu/semak ditebar dengan jarak minimum supaya selalu ada koridor.
///
/// Alasan desain: sumber kayu & batu yang tak habis ("unlimited").
///   - Pohon ditebang -> tunggul -> tumbuh jadi pohon lagi dalam 5 hari (sistem yang sudah ada).
///   - Setiap pagi, selama jumlah pohon di bawah batas, beberapa pohon/batu baru muncul
///     di tile rumput kosong. Jadi walau ditebang habis, hutan pulih perlahan.
///
/// Realistis: semua pohon/tunggul/batu adalah objek ISaveable, jadi kondisi hutan
/// (mana yang sudah ditebang, umur tunggul) ikut tersimpan otomatis di save.json.
/// Seed disimpan juga, jadi bentuk hutan yang sama muncul lagi saat dimuat ulang.
/// </summary>
public class Forest : GameLocation
{
    public const string LocationName = "Forest";

    // Warp
    public static readonly Point ToFarmTile = new(1, GameConfig.ForestHeight / 2);    // di ujung barat, kembali ke kebun
    public static readonly Point FromFarmTile = new(3, GameConfig.ForestHeight / 2);  // berdiri saat datang dari kebun

    // Daerah tumbuh baru (menjauh dari pintu masuk supaya jalur tidak tertutup)
    private static readonly Rectangle GrowthArea = new(8, 3, GameConfig.ForestWidth - 16, GameConfig.ForestHeight - 6);

    private readonly Random _growthRng = new();

    public Forest(int seed) : base(LocationName, GenerateMap(seed))
    {
        AddWarp(ToFarmTile, Farm.LocationName, Farm.FromForestTile);
    }

    public override bool IsOutdoors => true;

    /// <summary>Setiap pagi: hutan pulih — pohon & batu baru tumbuh sampai batas.</summary>
    public override void OnNewDay(GameDate newDate)
    {
        base.OnNewDay(newDate);

        for (int i = 0; i < GameConfig.ForestRegrowPerDay; i++)
            SpawnTree();
        for (int i = 0; i < GameConfig.ForestRockPerDay; i++)
            SpawnRock();
    }

    private void SpawnTree()
    {
        if (Objects.OfType<Tree>().Count() >= GameConfig.ForestMaxTrees)
            return;
        Point? tile = FindFreeGrassTile(GrowthArea, _growthRng);
        if (tile != null)
            Add(_growthRng.Next(3) == 0 ? new Pine(tile.Value) : new Tree(tile.Value));
    }

    private void SpawnRock()
    {
        if (Objects.OfType<Rock>().Count() >= GameConfig.ForestMaxRocks)
            return;
        Point? tile = FindFreeGrassTile(GrowthArea, _growthRng);
        if (tile != null)
            Add(new Rock(tile.Value));
    }

    // ------------------------------------------------------------------ generator

    /// <summary>
    /// Buat TileMap 80x56 dari seed. Baris dasarnya rumput; tepi diberi dinding pohon;
    /// jalan berkelok dari pintu masuk barat; sedikit tanah terbuka & kolam kecil;
    /// lalu pohon/cemara/batu/semak ditebar dengan jarak minimum.
    /// </summary>
    private static TileMap GenerateMap(int seed)
    {
        Random rng = new(seed);
        int width = GameConfig.ForestWidth, height = GameConfig.ForestHeight;

        TileType[,] layout = new TileType[width, height];
        List<MapMarker> markers = new();

        // 1. Dasar: rumput di dalam, void di tepi peta (batas dunia)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool border = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                layout[x, y] = border ? TileType.Void : TileType.Grass;
            }
        }

        // 2. Kolam kecil (air) di beberapa tempat — dibuat SEBELUM jalan supaya jalan selalu terbuka
        int pondCount = 2 + rng.Next(2);
        for (int p = 0; p < pondCount; p++)
        {
            int cx = rng.Next(10, width - 12), cy = rng.Next(6, height - 8);
            int radius = 2 + rng.Next(2);
            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                    if (x > 0 && y > 0 && x < width - 1 && y < height - 1 &&
                        (x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius)
                        layout[x, y] = TileType.Water;
        }

        // 3. Jalan berkelok dari barat (pintu masuk) ke timur — ditulis setelah kolam, jadi pasti terbuka
        int yPath = height / 2;
        for (int x = 1; x < width - 1; x++)
        {
            layout[x, yPath] = TileType.Path;
            if (x % 7 == 0)   // berkelok pelan supaya terasa alami
                yPath = Math.Clamp(yPath + rng.Next(-2, 3), 3, height - 4);
            layout[x, yPath] = TileType.Path;
        }

        // 4. Dinding hutan di tepi (2 lapis pohon supaya terasa terkurung)
        for (int y = 1; y < height - 1; y++)
        {
            for (int layer = 0; layer < 2; layer++)
            {
                int x = layer == 0 ? 1 : width - 2;
                if (layout[x, y] == TileType.Grass && rng.Next(4) != 0)   // 75% pohon, ada celah
                    markers.Add(new MapMarker(rng.Next(2) == 0 ? 'T' : 'Y', new Point(x, y)));
            }
        }
        for (int x = 2; x < width - 2; x++)
        {
            for (int layer = 0; layer < 2; layer++)
            {
                int y = layer == 0 ? 1 : height - 2;
                if (layout[x, y] == TileType.Grass && rng.Next(4) != 0)
                    markers.Add(new MapMarker(rng.Next(2) == 0 ? 'T' : 'Y', new Point(x, y)));
            }
        }

        // 5. Taburkan pohon/cemara/batu/semak/ranting/kerikil/bunga.
        //    Dipakai grid berjeda (jittered grid): setiap sel 2x2 dipilih satu titik acak di dalamnya.
        //    Jadi pohon tidak menumpuk, koridor tetap ada, dan jumlah percobaan pasti selesai
        //    (rejection sampling murni bisa berputar tanpa henti kalau peta hampir penuh).
        int targetTrees = GameConfig.ForestInitialTrees;
        int placedTrees = 0, placedOther = 0;
        int maxOther = 240;

        for (int cellY = 2; cellY < height - 2 && placedTrees < targetTrees; cellY += 2)
        {
            for (int cellX = 2; cellX < width - 2 && placedTrees < targetTrees; cellX += 2)
            {
                int x = Math.Clamp(cellX + rng.Next(-1, 2), 2, width - 3);
                int y = Math.Clamp(cellY + rng.Next(-1, 2), 2, height - 3);

                if (layout[x, y] != TileType.Grass || !IsSpotFree(markers, x, y))
                    continue;

                // Jauh dari jalan utama -> lebih banyak cemara (teras padat di pedalaman)
                int distToPath = Math.Abs(y - yPath);
                bool pine = rng.Next(100) < (distToPath > 6 ? 45 : 25);
                markers.Add(new MapMarker(pine ? 'Y' : 'T', new Point(x, y)));
                placedTrees++;

                // Sesekali sel yang sama diberi hiasan tambahan di sela pohon
                if (placedOther < maxOther && rng.Next(3) == 0)
                {
                    int dx = Math.Clamp(x + rng.Next(-2, 3), 2, width - 3);
                    int dy = Math.Clamp(y + rng.Next(-2, 3), 2, height - 3);
                    if (layout[dx, dy] == TileType.Grass && IsSpotFree(markers, dx, dy))
                    {
                        markers.Add(new MapMarker(RandomDecor(rng), new Point(dx, dy)));
                        placedOther++;
                    }
                }
            }
        }

        // 6. Pecahan batu & kerikil di jalur (mudah ditemukan saat pertama masuk)
        for (int i = 0; i < 26; i++)
        {
            int x = rng.Next(4, width - 4);
            int y = Math.Clamp(yPath + rng.Next(-1, 2), 2, height - 3);
            if (layout[x, y] == TileType.Path && IsSpotFree(markers, x, y))
                markers.Add(new MapMarker(rng.Next(3) == 0 ? 'R' : 'p', new Point(x, y)));
        }

        // 7. Pintu masuk: buka area kecil di barat supaya tidak langsung ketutup pohon
        int midY = height / 2;
        for (int x = 1; x <= 5; x++)
        {
            for (int y = midY - 2; y <= midY + 2; y++)
            {
                layout[x, y] = x == 1 || y == midY ? TileType.Path : TileType.Dirt;
                markers.RemoveAll(m => m.Tile.X == x && m.Tile.Y == y);
            }
        }

        // 8. Pastikan jalan pintu masuk terbuka penuh (hapus hiasan di jalurnya)
        markers.RemoveAll(m => m.Tile.Y == midY && m.Tile.X >= 1 && m.Tile.X <= 6);

        return new TileMap(layout, FromFarmTile, markers);
    }

    /// <summary>Tidak ada hiasan lain di tile ini dan 1 tile di sekitarnya (pohon tidak menumpuk).</summary>
    private static bool IsSpotFree(List<MapMarker> markers, int x, int y)
    {
        foreach (MapMarker m in markers)
        {
            if (Math.Abs(m.Tile.X - x) <= 1 && Math.Abs(m.Tile.Y - y) <= 1)
                return false;
        }
        return true;
    }

    private static char RandomDecor(Random rng)
    {
        int roll = rng.Next(100);
        if (roll < 22) return 'b';    // semak
        if (roll < 40) return 'R';    // batu
        if (roll < 55) return 'w';    // ranting
        if (roll < 70) return 'g';    // rumput liar
        if (roll < 85) return '*';    // bunga
        return 'Y';                   // cemara ekstra
    }
}
