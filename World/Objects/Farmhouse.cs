namespace FarmGame.World.Objects;

/// <summary>
/// Gambar rumah (dekorasi saja). Tabrakan sudah diurus tile Building di map,
/// jadi rumah tidak solid dan TIDAK masuk indeks tile (pintunya objek terpisah: FarmhouseDoor).
/// </summary>
public class Farmhouse : GameObject
{
    private const int WidthTiles = 5, HeightTiles = 3;
    private const int RoofExtraHeight = 12;   // atap + cerobong menjulang di atas tile bangunan

    public Farmhouse(Point topLeftTile)
    {
        PlaceAt(TileMap.TileToWorld(topLeftTile.X, topLeftTile.Y));
    }

    public override SizeF HitboxSize => new(WidthTiles * GameConfig.TileSize, HeightTiles * GameConfig.TileSize);

    public override RectangleF SpriteBounds =>
        new(Position.X, Position.Y - RoofExtraHeight, HitboxSize.Width, HitboxSize.Height + RoofExtraHeight);

    /// <summary>Satu piksel lebih awal dari pintu, supaya pintu selalu tergambar di atas rumah.</summary>
    public override int DrawOrder => (int)Bounds.Bottom - 1;

    public override void Draw(Graphics g, IAssetProvider assets)
    {
        assets.DrawSprite(g, "house", SpriteBounds);
    }
}
