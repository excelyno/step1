namespace FarmGame.World;

/// <summary>
/// Kelas induk semua area (Farm sekarang; Town, rumah, tambang nanti).
/// Memiliki TileMap + daftar GameObject, dan menjadi "wasit" tabrakan.
/// MVP 1: juga mencatat objek dunia per tile (tanah dicangkul, kotak kirim, pintu).
/// </summary>
public abstract class GameLocation : IUpdatable
{
    private readonly List<GameObject> _objects = new();

    // Indeks cepat: tile -> objek dunia di tile itu. Satu tile maksimal satu objek.
    private readonly Dictionary<Point, WorldObject> _tileIndex = new();

    public string Name { get; }
    public TileMap Map { get; }

    /// <summary>Luar ruangan atau bukan (nanti dipakai untuk cuaca). WAJIB diisi turunan.</summary>
    public abstract bool IsOutdoors { get; }

    /// <summary>Daftar objek hanya bisa dibaca dari luar. Menambah/menghapus harus lewat Add/Remove.</summary>
    public IReadOnlyList<GameObject> Objects => _objects;

    protected GameLocation(string name, string mapFileName)
    {
        Name = name;
        string path = Path.Combine(AppContext.BaseDirectory, GameConfig.MapFolder, mapFileName);
        Map = MapLoader.LoadFromFile(path);
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
        obj.Location = this;
    }

    public void Remove(GameObject obj)
    {
        if (!_objects.Remove(obj))
            return;

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

    public virtual void Update(float deltaSeconds)
    {
        // ToList() = salinan daftar, jadi aman kalau ada objek yang ditambah/dihapus saat update
        foreach (GameObject obj in _objects.ToList())
            obj.Update(deltaSeconds);   // polimorfisme: tiap objek punya Update sendiri
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
        if (!Map.IsAreaWalkable(area))
            return false;

        foreach (GameObject other in _objects)
        {
            if (other != mover && other.IsSolid && other.Bounds.IntersectsWith(area))
                return false;
        }
        return true;
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
