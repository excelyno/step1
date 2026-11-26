namespace FarmGame.UI;

/// <summary>
/// Gambar kotak dialog di bagian bawah layar: label nama, teks yang dipecah per baris,
/// dan segitiga berkedip kalau halaman sudah selesai diketik. Hanya MENGGAMBAR.
/// </summary>
public class DialogueBox
{
    private const int BoxHeight = 150;
    private const int Margin = 60;

    private const int PortraitSize = 96;

    private readonly IAssetProvider _assets;
    private readonly Font _text = new("Segoe UI", 14f, FontStyle.Bold);
    private readonly Font _name = new("Segoe UI", 12f, FontStyle.Bold);
    private readonly Font _hint = new("Segoe UI", 9f);

    public DialogueBox(IAssetProvider assets)
    {
        _assets = assets;
    }

    /// <param name="visibleText">bagian halaman yang sudah "diketik"</param>
    /// <param name="pageDone">true = tampilkan tanda lanjut</param>
    /// <param name="portrait">sprite karakter untuk wajah di kanan; null = tanpa wajah</param>
    public void Draw(Graphics g, string speaker, string visibleText, bool pageDone, int page, int pageCount, string? portrait = null)
    {
        var box = new Rectangle(Margin, GameConfig.ScreenHeight - BoxHeight - 16, GameConfig.ScreenWidth - Margin * 2, BoxHeight);
        UiPanel.Draw(g, box);

        // Wajah pembicara di kanan: sprite 16x32 diperbesar 6x, hanya bagian kepala-dada yang terlihat
        int textRight = box.Right;
        if (portrait != null)
        {
            var frame = new Rectangle(box.Right - PortraitSize - 22, box.Y + (box.Height - PortraitSize) / 2, PortraitSize, PortraitSize);
            using (SolidBrush back = new(Color.FromArgb(226, 200, 150)))
                g.FillRectangle(back, frame);
            GraphicsState state = g.Save();
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.SetClip(frame);
            _assets.DrawSprite(g, portrait, new RectangleF(frame.X, frame.Y + 4, PortraitSize, PortraitSize * 2));
            g.Restore(state);
            using Pen border = new(Color.FromArgb(150, 100, 60), 3);
            g.DrawRectangle(border, frame);
            textRight = frame.X - 10;
        }

        // Label nama di pojok kiri atas
        int nameWidth = (int)g.MeasureString(speaker, _name).Width + 30;
        UiPanel.DrawTag(g, new Rectangle(box.X + 18, box.Y - 22, nameWidth, 36), speaker, _name);

        // Teks dipecah supaya muat
        var textArea = new RectangleF(box.X + 28, box.Y + 24, textRight - box.X - 56, box.Height - 44);
        using SolidBrush ink = new(UiPanel.Ink);
        g.DrawString(visibleText, _text, ink, textArea);

        // Nomor halaman + tanda lanjut
        if (pageCount > 1)
            g.DrawString($"{page + 1}/{pageCount}", _hint, Brushes.SaddleBrown, textRight - 40, box.Y + 12);

        if (pageDone && Environment.TickCount64 / 400 % 2 == 0)
        {
            float x = textRight - 30, y = box.Bottom - 30;
            PointF[] arrow = { new(x, y), new(x + 14, y), new(x + 7, y + 9) };
            g.FillPolygon(ink, arrow);
        }
    }
}
