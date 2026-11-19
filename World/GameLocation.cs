namespace FarmGame.World;

/// <summary>
/// Kelas induk semua area (Farm sekarang; Town, rumah, tambang nanti).
/// Memiliki TileMap + daftar GameObject, dan menjadi "wasit" tabrakan.
/// </summary>
public abstract class GameLocation : IUpdatable
{
    private readonly List<GameObject> _objects = new();

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
        _objects.Add(obj);
        obj.Location = this;
    }

    public void Remove(GameObject obj)
    {
        if (_objects.Remove(obj))
            obj.Location = null;
    }

    public virtual void Update(float deltaSeconds)
    {
        // ToList() = salinan daftar, jadi aman kalau ada objek yang ditambah/dihapus saat update
        foreach (GameObject obj in _objects.ToList())
            obj.Update(deltaSeconds);   // polimorfisme: tiap objek punya Update sendiri
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

    /// <summary>Posisi supaya hitbox objek tepat di tengah tile spawn ('S').</summary>
    public Vector2 GetSpawnPosition(GameObject obj)
    {
        float size = GameConfig.TileSize;
        Vector2 tileCorner = TileMap.TileToWorld(Map.SpawnTile.X, Map.SpawnTile.Y);
        return tileCorner + new Vector2((size - obj.HitboxSize.Width) / 2f, (size - obj.HitboxSize.Height) / 2f);
    }
}
