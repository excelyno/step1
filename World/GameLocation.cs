namespace FarmGame.World;

/// <summary>
/// Kelas induk semua area (Farm sekarang; Town, rumah, tambang nanti).
/// Memiliki TileMap + daftar GameObject, dan menjadi "wasit" tabrakan.
/// MVP 1: juga mencatat objek dunia per tile (tanah dicangkul, kotak kirim, pintu).
/// </summary>
public abstract class GameLocation : IUpdatable
{
    private readonly List<GameObject> _objects = new();
    private int _objectsVersion;

    // Indeks cepat: tile -> objek dunia di tile itu. Satu tile maksimal satu objek.
    private readonly Dictionary<Point, WorldObject> _tileIndex = new();

    // MVP 2: titik pindah ke lokasi lain (tile -> warp)
    private readonly Dictionary<Point, Warp> _warps = new();

    public string Name { get; }
    public TileMap Map { get; }

    /// <summary>Luar ruangan atau bukan (nanti dipakai untuk cuaca). WAJIB diisi turunan.</summary>
    public abstract bool IsOutdoors { get; }

    /// <summary>Daftar objek hanya bisa dibaca dari luar. Menambah/menghapus harus lewat Add/Remove.</summary>
    public IReadOnlyList<GameObject> Objects => _objects;

    /// <summary>
    /// MVP 6: nomor versi daftar objek. Renderer memakai ini untuk tahu urutan gambar perlu dihitung ulang
    /// atau tidak (tanpa mengurutkan ratusan objek setiap frame).
    /// </summary>
    public int ObjectsVersion => _objectsVersion;

    /// <summary>
    /// MVP 5: layanan game (efek, popup, goyang kamera) untuk objek yang bereaksi sendiri tanpa aksi pemain
    /// (rumput diinjak, pohon tumbang). Diisi Game saat dunia dibuat. Null di dalam tes sederhana.
    /// </summary>
    public IGameServices? Services { get; set; }

    protected GameLocation(string name, string mapFileName)
        : this(name, LoadMap(mapFileName))
    {
    }

    /// <summary>MVP 7: lokasi dengan peta dibuat prosedural (tanpa file), misalnya Hutan.</summary>
    protected GameLocation(string name, TileMap map)
    {
        Name = name;
        Map = map;

        // Hiasan dari map -> objek dunia
        foreach (MapMarker marker in Map.Markers)
            Add(CreateScenery(marker.Symbol, marker.Tile));
    }

    private static TileMap LoadMap(string mapFileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, GameConfig.MapFolder, mapFileName);
        return MapLoader.LoadFromFile(path);
    }

    /// <summary>Huruf hiasan di file map -> objek. Lokasi lain boleh menimpa untuk hiasan khasnya.</summary>
    protected virtual Scenery CreateScenery(char symbol, Point tile)
    {
        switch (symbol)
        {
            case 'T': return new Tree(tile);
            case 'Y': return new Pine(tile);
            case 'b': return new Bush(tile);
            case 'R': return new Rock(tile);
            case 'g': return new GrassTuft(tile);
            case '*': return new Flowers(tile);
            case 'w': return new Twig(tile);
            case 'p': return new Pebble(tile);
            default: return new LilyPad(tile);   // 'L'
        }
    }

    public void Add(GameObject obj)
    {
        // Objek dunia dicatat juga di indeks tile
        if (obj is WorldObject worldObject)
        {
            foreach (Point tile in worldObject.OccupiedTiles)
            {
                if (_tileIndex.ContainsKey(tile))
                    throw new InvalidOperationException($"Tile {tile} sudah ditempati.");
                _tileIndex[tile] = worldObject;
            }
        }

        _objects.Add(obj);
        _objectsVersion++;
        obj.Location = this;
    }

    public void Remove(GameObject obj)
    {
        if (!_objects.Remove(obj))
            return;

        _objectsVersion++;
        if (obj is WorldObject worldObject)
        {
            foreach (Point tile in worldObject.OccupiedTiles)
                _tileIndex.Remove(tile);
        }
        obj.Location = null;
    }

    /// <summary>Objek dunia di tile ini, atau null kalau kosong.</summary>
    public WorldObject? GetObjectAt(Point tile)
    {
        _tileIndex.TryGetValue(tile, out WorldObject? obj);
        return obj;
    }

    public bool IsTileFree(Point tile) => GetObjectAt(tile) == null;

    /// <summary>
    /// Sesuatu yang bisa diperiksa di tile ini: objek dunia (meja, pintu, tanaman),
    /// atau makhluk yang sedang berdiri di sana (NPC).
    /// </summary>
    public IInteractable? GetInteractableAt(Point tile)
    {
        if (GetObjectAt(tile) is IInteractable worldObject)
            return worldObject;

        foreach (GameObject obj in _objects)
        {
            if (obj is not WorldObject && obj is IInteractable creature && obj.TilePosition == tile)
                return creature;
        }
        return null;
    }

    // ---------- MVP 2: pindah lokasi ----------

    public void AddWarp(Point tile, string targetLocation, Point targetTile)
    {
        _warps[tile] = new Warp(tile, targetLocation, targetTile);
    }

    /// <summary>Warp di tile ini, atau null.</summary>
    public Warp? GetWarpAt(Point tile)
    {
        _warps.TryGetValue(tile, out Warp? warp);
        return warp;
    }

    /// <summary>
    /// Tambah bangunan sambil mengecek bahwa tile di bawahnya memang huruf B di map.
    /// Kalau tidak cocok (salah ketik map / salah koordinat), langsung error yang jelas.
    /// </summary>
    protected void AddBuilding(Building building)
    {
        foreach (Point tile in building.CoveredTiles)
        {
            if (Map.GetTile(tile.X, tile.Y)?.Type != TileType.Building)
                throw new MapFormatException($"{Name}: bangunan di tile {tile.X},{tile.Y} tidak cocok dengan huruf 'B' di map.");
        }
        Add(building);
    }

    /// <summary>MVP 5: burung pipit yang mematuk tanah lalu terbang kalau didekati.</summary>
    protected void AddBirds(int count, RectangleF area, int seed)
    {
        Random rng = new(seed);
        for (int i = 0; i < count; i++)
        {
            Vector2 start = new(area.X + (float)rng.NextDouble() * area.Width, area.Y + (float)rng.NextDouble() * area.Height);
            Add(new Bird(start, area));
        }
    }

    /// <summary>MVP 5: tile rumput kosong acak di dalam area (untuk barang liar). Null kalau tidak ketemu.</summary>
    public Point? FindFreeGrassTile(Rectangle areaInTiles, Random rng)
    {
        for (int attempt = 0; attempt < 60; attempt++)
        {
            Point tile = new(rng.Next(areaInTiles.Left, areaInTiles.Right), rng.Next(areaInTiles.Top, areaInTiles.Bottom));
            if (Map.GetTile(tile.X, tile.Y)?.Type == TileType.Grass && IsTileFree(tile) && GetWarpAt(tile) == null)
                return tile;
        }
        return null;
    }

    /// <summary>Kupu-kupu beterbangan di dalam lokasi (tidak sampai ke hutan di pinggir).</summary>
    protected void AddButterflies(int count, int seed)
    {
        int margin = 3 * GameConfig.TileSize;
        RectangleF area = new(margin, margin, Map.PixelWidth - margin * 2, Map.PixelHeight - margin * 2);
        Random rng = new(seed);
        for (int i = 0; i < count; i++)
        {
            Vector2 start = new(area.X + (float)rng.NextDouble() * area.Width, area.Y + (float)rng.NextDouble() * area.Height);
            Add(new Butterfly(start, area, i % 2));
        }
    }

    public virtual void Update(float deltaSeconds)
    {
        // ToList() = salinan daftar, jadi aman kalau ada objek yang ditambah/dihapus saat update
        foreach (GameObject obj in _objects.ToList())
            obj.Update(deltaSeconds);   // polimorfisme: tiap objek punya Update sendiri

        // MVP 5: makhluk yang sedang berjalan "menyentuh" benda di tile tempatnya berdiri
        foreach (GameObject obj in _objects.ToList())
        {
            if (obj is Character { IsMoving: true } walker && GetObjectAt(walker.TilePosition) is ITouchable touched)
                touched.OnTouched(walker);
        }
    }

    /// <summary>Ganti hari: semua objek yang peduli hari baru (IDayUpdatable) diberi tahu.</summary>
    public virtual void OnNewDay(GameDate newDate)
    {
        foreach (GameObject obj in _objects.ToList())
        {
            if (obj is IDayUpdatable dayUpdatable)
                dayUpdatable.OnNewDay(newDate);   // polimorfisme: HoeDirt menumbuhkan tanaman
        }
    }

    /// <summary>Boleh berdiri di area ini? Tile harus walkable dan tidak menabrak objek solid lain.</summary>
    public bool CanOccupy(RectangleF area, GameObject mover)
    {
        return Map.IsAreaWalkable(area) && FindBlocker(area, mover) == null;
    }

    /// <summary>
    /// Objek solid yang menghalangi area ini (null = bebas).
    /// MVP 5: memakai CollisionBox. Objek yang SUDAH bertumpuk dengan mover diabaikan,
    /// supaya karakter yang terlanjur menempel (misalnya baru muncul) tidak terkunci selamanya.
    /// </summary>
    public GameObject? FindBlocker(RectangleF area, GameObject mover)
    {
        RectangleF current = mover.CollisionBox;
        foreach (GameObject other in _objects)
        {
            if (other == mover || !other.IsSolid)
                continue;
            RectangleF box = other.CollisionBox;
            if (box.IntersectsWith(area) && !box.IntersectsWith(current))
                return other;
        }
        return null;
    }

    /// <summary>Posisi supaya hitbox objek tepat di tengah sebuah tile.</summary>
    public Vector2 GetPositionOnTile(GameObject obj, Point tile)
    {
        float size = GameConfig.TileSize;
        Vector2 tileCorner = TileMap.TileToWorld(tile.X, tile.Y);
        return tileCorner + new Vector2((size - obj.HitboxSize.Width) / 2f, (size - obj.HitboxSize.Height) / 2f);
    }

    /// <summary>Posisi di tile spawn ('S' di file map).</summary>
    public Vector2 GetSpawnPosition(GameObject obj) => GetPositionOnTile(obj, Map.SpawnTile);
}
