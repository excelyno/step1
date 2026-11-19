namespace FarmGame.World.Objects;

/// <summary>
/// Kelas induk benda yang DIAM dan menempel di grid tile (tanah dicangkul, kotak kirim, pintu,
/// nanti batu dan pohon). Posisinya dihitung dari tile, jadi selalu rapi sejajar grid.
/// </summary>
public abstract class WorldObject : GameObject
{
    public Point Tile { get; }

    /// <summary>Ukuran dalam tile. Default 1x1; ShippingBin 2x1.</summary>
    public virtual Size SizeInTiles => new(1, 1);

    public override SizeF HitboxSize => new(SizeInTiles.Width * GameConfig.TileSize, SizeInTiles.Height * GameConfig.TileSize);

    /// <summary>Default: menghalangi jalan. HoeDirt menimpanya jadi false.</summary>
    public override bool IsSolid => true;

    /// <summary>Nama gambar. WAJIB diisi turunan.</summary>
    public abstract string SpriteKey { get; }

    protected WorldObject(Point tile)
    {
        Tile = tile;
        PlaceAt(TileMap.TileToWorld(tile.X, tile.Y));
    }

    /// <summary>Semua tile yang ditempati benda ini (untuk indeks di GameLocation).</summary>
    public IEnumerable<Point> OccupiedTiles
    {
        get
        {
            for (int x = 0; x < SizeInTiles.Width; x++)
                for (int y = 0; y < SizeInTiles.Height; y++)
                    yield return new Point(Tile.X + x, Tile.Y + y);
        }
    }

    public override void Draw(Graphics g, IAssetProvider assets)
    {
        assets.DrawSprite(g, SpriteKey, SpriteBounds);
    }
}
