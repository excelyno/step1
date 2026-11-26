namespace FarmGame.Rendering;

/// <summary>
/// Tepi layar sedikit lebih gelap (MVP 4) supaya mata fokus ke tengah.
/// Gambarnya dibuat SEKALI lalu dipakai ulang setiap frame (murah).
/// </summary>
public class Vignette
{
    private readonly Bitmap _image;

    public Vignette()
    {
        int w = GameConfig.ScreenWidth, h = GameConfig.ScreenHeight;
        _image = new Bitmap(w, h);
        using Graphics g = Graphics.FromImage(_image);
        using GraphicsPath ellipse = new();
        ellipse.AddEllipse(-w * 0.25f, -h * 0.3f, w * 1.5f, h * 1.6f);   // lebih besar dari layar: hanya sudut yang gelap

        g.Clear(Color.FromArgb(70, 10, 6, 12));
        using PathGradientBrush brush = new(ellipse)
        {
            CenterColor = Color.FromArgb(0, 10, 6, 12),
            SurroundColors = new[] { Color.FromArgb(70, 10, 6, 12) },
            FocusScales = new PointF(0.55f, 0.5f),   // tengah yang terang lebih luas
        };
        g.CompositingMode = CompositingMode.SourceCopy;
        g.FillPath(brush, ellipse);
    }

    public void Draw(Graphics g)
    {
        g.DrawImageUnscaled(_image, 0, 0);
    }
}
