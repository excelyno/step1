namespace FarmGame.Rendering;

/// <summary>
/// Menggambar satu frame: tile yang terlihat, lalu objek (diurutkan berdasarkan Y kaki).
/// Semua dunia digambar dalam piksel logis; skala x3 dan geser kamera dilakukan lewat transform Graphics.
/// </summary>
public class Renderer
{
    private readonly IAssetProvider _assets;
    private readonly Camera _camera;

    public Renderer(IAssetProvider assets, Camera camera)
    {
        _assets = assets;
        _camera = camera;
    }

    public void Render(Graphics g, GameLocation location)
    {
        g.Clear(Color.Black);
        g.InterpolationMode = InterpolationMode.NearestNeighbor;   // pixel art tetap tajam
        g.PixelOffsetMode = PixelOffsetMode.Half;                  // cegah garis celah antar tile
        g.SmoothingMode = SmoothingMode.None;

        GraphicsState state = g.Save();
        g.ScaleTransform(GameConfig.Scale, GameConfig.Scale);
        g.TranslateTransform(-MathF.Round(_camera.Position.X), -MathF.Round(_camera.Position.Y));

        location.Map.Draw(g, _assets, _camera.ViewArea);   // lapisan 1: tile
        DrawObjects(g, location);                          // lapisan 2: objek

        g.Restore(state);   // kembali ke koordinat layar biasa
    }

    private void DrawObjects(Graphics g, GameLocation location)
    {
        // Ambil objek yang terlihat saja (culling), lalu urutkan dari yang paling atas (Y-sorting)
        List<GameObject> visible = new();
        foreach (GameObject obj in location.Objects)
        {
            if (obj.SpriteBounds.IntersectsWith(_camera.ViewArea))
                visible.Add(obj);
        }
        visible.Sort((a, b) => a.DrawOrder.CompareTo(b.DrawOrder));

        foreach (GameObject obj in visible)
            obj.Draw(g, _assets);   // polimorfisme: Renderer tidak perlu tahu jenis objeknya
    }
}
