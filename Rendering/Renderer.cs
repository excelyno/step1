namespace FarmGame.Rendering;

/// <summary>
/// Menggambar satu frame: tile yang terlihat, bayangan, objek (diurutkan berdasarkan Y kaki), lalu partikel.
/// Semua dunia digambar dalam piksel logis; skala x3 dan geser kamera dilakukan lewat transform Graphics.
/// MVP 5: atap & pohon yang menutupi pemain jadi tembus pandang (pelan-pelan, tidak berkedip).
/// </summary>
public class Renderer
{
    private const float FadedOpacity = 0.38f;
    private const float FadeSpeed = 7f;   // per detik

    private readonly IAssetProvider _assets;
    private readonly Camera _camera;
    private readonly Func<GameLocation, ChunkCache> _chunkProvider;

    public Renderer(IAssetProvider assets, Camera camera, Func<GameLocation, ChunkCache>? chunkProvider = null)
    {
        _assets = assets;
        _camera = camera;
        _chunkProvider = chunkProvider ?? (_ => throw new InvalidOperationException("Chunk provider belum diatur."));
    }

    public void Render(Graphics g, GameLocation location, ParticleSystem particles)
    {
        g.Clear(Color.Black);
        g.InterpolationMode = InterpolationMode.NearestNeighbor;   // pixel art tetap tajam
        g.PixelOffsetMode = PixelOffsetMode.Half;                  // cegah garis celah antar tile
        g.SmoothingMode = SmoothingMode.None;

        GraphicsState state = BeginWorld(g);
        DrawGround(g, location);                           // lapisan 1: tile (dari cache potongan peta)
        DrawObjects(g, location);                          // lapisan 2: bayangan + objek
        particles.Draw(g);                                 // lapisan 3: partikel (MVP 4)
        g.Restore(state);   // kembali ke koordinat layar biasa
    }

    /// <summary>
    /// MVP 6: lapisan tanah dari ChunkCache (2-6 DrawImage per frame, bukan ratusan).
    /// Air tetap beranimasi: barisan air yang terlihat digambar langsung di atas cache.
    /// </summary>
    private void DrawGround(Graphics g, GameLocation location)
    {
        if (_assets is SpriteAssets sprites)
            game_Chunks(g, location, sprites);
        else
            location.Map.Draw(g, _assets, _camera.ViewArea);   // fallback (placeholder)
    }

    private void game_Chunks(Graphics g, GameLocation location, SpriteAssets sprites)
    {
        ChunkCache chunks = _chunkProvider(location);
        chunks.Draw(g, _camera.ViewArea);

        // Tile yang beranimasi (air) digambar ulang langsung supaya tetap bergerak
        sprites.DrawAnimatedTiles(g, location.Map, _camera.ViewArea);
    }

    /// <summary>Transform dunia: skala x3 lalu geser sesuai kamera (termasuk getaran).</summary>
    private GraphicsState BeginWorld(Graphics g)
    {
        GraphicsState state = g.Save();
        g.ScaleTransform(GameConfig.Scale, GameConfig.Scale);
        Vector2 origin = _camera.DrawPosition;
        g.TranslateTransform(-origin.X, -origin.Y);
        return state;
    }

    /// <summary>
    /// MVP 4: lapisan yang digambar SETELAH cahaya, supaya tetap terang di malam hari (tanda quest, emosi).
    /// MVP 5: juga kursor tile, gelembung petunjuk, dan teks melayang.
    /// </summary>
    public void RenderOverlays(Graphics g, GameLocation location, InteractionPrompt? prompt = null, FloatingPopups? popups = null)
    {
        GraphicsState state = BeginWorld(g);
        prompt?.DrawCursor(g);
        foreach (GameObject obj in location.Objects)
        {
            if (obj is Npc npc && npc.SpriteBounds.IntersectsWith(_camera.ViewArea))
                npc.DrawMarker(g, _assets);
        }
        g.Restore(state);

        // Koordinat layar: huruf tetap tajam
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        popups?.Draw(g, _camera, _assets);
        prompt?.DrawBubble(g, _camera);
    }

    /// <summary>
    /// MVP 5: atur tembus pandang. Benda yang FadesWhenBehind menjadi transparan kalau gambar pemain
    /// tertutup olehnya DAN pemain ada di belakangnya (digambar lebih dulu). Berubah pelan-pelan.
    /// </summary>
    public void UpdateFades(GameLocation location, GameObject focus, float deltaSeconds)
    {
        RectangleF body = focus.SpriteBounds;
        body.Inflate(-2, -2);
        float step = FadeSpeed * deltaSeconds;
        foreach (GameObject obj in location.Objects)
        {
            if (!obj.FadesWhenBehind)
                continue;
            bool covers = obj.DrawOrder > focus.DrawOrder && obj.SpriteBounds.IntersectsWith(body);
            float target = covers ? FadedOpacity : 1f;
            obj.Opacity = obj.Opacity < target ? Math.Min(target, obj.Opacity + step) : Math.Max(target, obj.Opacity - step);
        }
    }

    private void DrawObjects(Graphics g, GameLocation location)
    {
        // Urutan dihitung ulang setiap frame (DrawOrder bergantung posisi pemain/NPC).
        IList<GameObject> ordered = GetSorted(location);

        // MVP 4: semua bayangan dulu, supaya bayangan tidak pernah menimpa objek lain
        foreach (GameObject obj in ordered)
        {
            if (obj.SpriteBounds.IntersectsWith(_camera.ViewArea))
                obj.DrawShadow(g);
        }

        foreach (GameObject obj in ordered)
        {
            if (!obj.SpriteBounds.IntersectsWith(_camera.ViewArea))
                continue;   // culling tanpa alokasi daftar baru

            _assets.Opacity = obj.Opacity;
            obj.Draw(g, _assets);   // polimorfisme: Renderer tidak perlu tahu jenis objeknya
        }
        _assets.Opacity = 1f;
    }

    /// <summary>
    /// Urutan gambar dihitung ulang SETIAP frame: DrawOrder bergantung posisi
    /// (kaki pemain/NPC bergerak), jadi cache berbasis daftar objek saja tidak cukup
    /// dan membuat pemain "ketimpa" bangunan. Buffer dipakai ulang agar tidak alokasi tiap frame.
    /// </summary>
    private readonly List<GameObject> _sortBuffer = new();

    private IList<GameObject> GetSorted(GameLocation location)
    {
        _sortBuffer.Clear();
        _sortBuffer.AddRange(location.Objects);
        _sortBuffer.Sort((a, b) => a.DrawOrder.CompareTo(b.DrawOrder));
        return _sortBuffer;
    }
}
