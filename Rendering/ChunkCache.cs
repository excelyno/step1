namespace FarmGame.Rendering;

/// <summary>
/// Cache gambar tile-map per potongan (chunk, MVP 6).
/// Map hampir tidak pernah berubah (tanah dicangkul/siram hanya mengganti OBJEK, bukan tile),
/// jadi setiap potongan 8x8 tile digambar SEKALI ke bitmap, lalu tiap frame cukup ditempel.
///
/// Hasilnya: dari ratusan DrawImage per frame (tile + tepi rumput/air otomatis)
/// menjadi sekitar 2-6 DrawImageUnscaled per frame. Tampilan tidak berubah sama sekali.
///
/// Kalau suatu saat tile diubah saat bermain (misalnya jalan rusak), panggil InvalidateAll()
/// atau Invalidate(tile) supaya potongan itu digambar ulang.
/// </summary>
public class ChunkCache : IDisposable
{
    public const int ChunkTiles = 8;   // 8x8 tile = 128 piksel logis = 384 piksel layar

    private readonly TileMap _map;
    private readonly SpriteAssets _assets;
    private readonly int _chunksX, _chunksY;
    private readonly Bitmap?[] _chunks;
    private bool _dirtyAll = true;

    public ChunkCache(TileMap map, SpriteAssets assets)
    {
        _map = map;
        _assets = assets;
        _chunksX = (map.Width + ChunkTiles - 1) / ChunkTiles;
        _chunksY = (map.Height + ChunkTiles - 1) / ChunkTiles;
        _chunks = new Bitmap[_chunksX * _chunksY];
    }

    /// <summary>Tandai semua potongan perlu digambar ulang (dipanggil kalau peta berubah).</summary>
    public void InvalidateAll()
    {
        _dirtyAll = true;
    }

    /// <summary>Tandai satu tile berubah (potongannya saja yang digambar ulang).</summary>
    public void Invalidate(Point tile)
    {
        int cx = tile.X / ChunkTiles, cy = tile.Y / ChunkTiles;
        if (cx < 0 || cy < 0 || cx >= _chunksX || cy >= _chunksY)
            return;
        _chunks[cy * _chunksX + cx]?.Dispose();
        _chunks[cy * _chunksX + cx] = null;
    }

    /// <summary>Gambar bagian peta yang terlihat. Digambar dalam koordinat DUNIA (pemanggil sudah pasang transform).</summary>
    public void Draw(Graphics g, RectangleF view)
    {
        int size = GameConfig.TileSize;
        int cx0 = Math.Max(0, TileMap.WorldToTile(view.Left) / ChunkTiles);
        int cy0 = Math.Max(0, TileMap.WorldToTile(view.Top) / ChunkTiles);
        int cx1 = Math.Min(_chunksX - 1, TileMap.WorldToTile(view.Right) / ChunkTiles);
        int cy1 = Math.Min(_chunksY - 1, TileMap.WorldToTile(view.Bottom) / ChunkTiles);

        for (int cy = cy0; cy <= cy1; cy++)
        {
            for (int cx = cx0; cx <= cx1; cx++)
            {
                Bitmap chunk = GetChunk(cx, cy);
                g.DrawImage(chunk, cx * ChunkTiles * size, cy * ChunkTiles * size);
            }
        }
    }

    private Bitmap GetChunk(int cx, int cy)
    {
        int index = cy * _chunksX + cx;
        if (_dirtyAll)
        {
            for (int i = 0; i < _chunks.Length; i++)
            {
                _chunks[i]?.Dispose();
                _chunks[i] = null;
            }
            _dirtyAll = false;
        }

        Bitmap? chunk = _chunks[index];
        if (chunk != null)
            return chunk;

        // Gambar potongan peta ke bitmap kecil (piksel logis, tanpa skala)
        int width = Math.Min(ChunkTiles, _map.Width - cx * ChunkTiles);
        int height = Math.Min(ChunkTiles, _map.Height - cy * ChunkTiles);
        chunk = new Bitmap(width * GameConfig.TileSize, height * GameConfig.TileSize);

        using (Graphics g = Graphics.FromImage(chunk))
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int tileX = cx * ChunkTiles + x;
                    int tileY = cy * ChunkTiles + y;
                    _assets.DrawTile(g, _map, tileX, tileY,
                                     new RectangleF(x * GameConfig.TileSize, y * GameConfig.TileSize,
                                                    GameConfig.TileSize, GameConfig.TileSize));
                }
            }
        }

        _chunks[index] = chunk;
        return chunk;
    }

    public void Dispose()
    {
        foreach (Bitmap? chunk in _chunks)
            chunk?.Dispose();
        _chunks[_chunks.Length - 1] = null;
        Array.Clear(_chunks);
    }
}
