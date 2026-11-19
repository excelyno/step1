namespace FarmGame.World;

/// <summary>
/// Grid 2D berisi Tile. Bisa menjawab "area ini bisa dilewati?" dan
/// menggambar tile yang sedang terlihat kamera saja.
/// </summary>
public class TileMap
{
    private readonly Tile[,] _tiles;

    public int Width { get; }
    public int Height { get; }
    public Point SpawnTile { get; }   // posisi huruf 'S' di file map

    /// <summary>Hiasan dari file map (pohon, batu, bunga, ...). Dibuat jadi objek oleh GameLocation.</summary>
    public IReadOnlyList<MapMarker> Markers { get; }

    public int PixelWidth => Width * GameConfig.TileSize;
    public int PixelHeight => Height * GameConfig.TileSize;

    public TileMap(TileType[,] layout, Point spawnTile, List<MapMarker> markers)
    {
        Markers = markers;
        Width = layout.GetLength(0);
        Height = layout.GetLength(1);
        SpawnTile = spawnTile;

        _tiles = new Tile[Width, Height];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                _tiles[x, y] = new Tile(x, y, layout[x, y]);
    }

    public bool IsInBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    /// <summary>Mengembalikan null kalau di luar map (bukan crash).</summary>
    public Tile? GetTile(int x, int y) => IsInBounds(x, y) ? _tiles[x, y] : null;

    /// <summary>Di luar map dianggap tidak bisa dilewati.</summary>
    public bool IsWalkable(int x, int y)
    {
        Tile? tile = GetTile(x, y);
        return tile != null && tile.IsWalkable;
    }

    /// <summary>True kalau SEMUA tile yang disentuh area (hitbox) bisa dilewati.</summary>
    public bool IsAreaWalkable(RectangleF area)
    {
        int left = WorldToTile(area.Left);
        int right = WorldToTile(area.Right - 0.001f);
        int top = WorldToTile(area.Top);
        int bottom = WorldToTile(area.Bottom - 0.001f);

        for (int y = top; y <= bottom; y++)
            for (int x = left; x <= right; x++)
                if (!IsWalkable(x, y))
                    return false;

        return true;
    }

    /// <summary>Koordinat dunia (piksel logis) ke indeks tile.</summary>
    public static int WorldToTile(float world) => (int)Math.Floor(world / GameConfig.TileSize);

    /// <summary>Indeks tile ke koordinat dunia (pojok kiri-atas tile).</summary>
    public static Vector2 TileToWorld(int x, int y) => new(x * GameConfig.TileSize, y * GameConfig.TileSize);

    /// <summary>Menggambar hanya tile yang masuk area pandang (culling), bukan seluruh map.</summary>
    public void Draw(Graphics g, IAssetProvider assets, RectangleF view)
    {
        int size = GameConfig.TileSize;
        int x0 = Math.Max(0, WorldToTile(view.Left));
        int y0 = Math.Max(0, WorldToTile(view.Top));
        int x1 = Math.Min(Width - 1, WorldToTile(view.Right));
        int y1 = Math.Min(Height - 1, WorldToTile(view.Bottom));

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                assets.DrawTile(g, this, x, y, new RectangleF(x * size, y * size, size, size));
    }
}
