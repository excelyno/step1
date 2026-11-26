namespace FarmGame.UI;

/// <summary>
/// Panel UI berbingkai kayu dengan sudut membulat (MVP 4). Dipakai semua menu supaya tampilannya seragam:
/// bayangan di belakang -> bingkai kayu gelap -> kayu terang -> isi (kertas krem atau kayu gelap).
/// </summary>
public static class UiPanel
{
    public static readonly Color Ink = Color.FromArgb(62, 38, 22);
    public static readonly Color Paper = Color.FromArgb(250, 236, 200);

    private static readonly Color Shadow = Color.FromArgb(90, 0, 0, 0);
    private static readonly Color FrameDark = Color.FromArgb(74, 44, 24);
    private static readonly Color FrameWood = Color.FromArgb(168, 108, 58);
    private static readonly Color FrameLight = Color.FromArgb(214, 152, 92);
    private static readonly Color DarkFill = Color.FromArgb(236, 58, 38, 26);

    /// <summary>paper = true: isi krem (dialog, jurnal). false: isi kayu gelap (jam, toolbar).</summary>
    public static void Draw(Graphics g, Rectangle r, bool paper = true)
    {
        SmoothingMode oldMode = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle shadow = r;
        shadow.Offset(4, 5);
        Fill(g, shadow, 10, Shadow);
        Fill(g, r, 10, FrameDark);
        Fill(g, Inset(r, 3), 8, FrameWood);
        using (Pen highlight = new(FrameLight, 1.5f))
        using (GraphicsPath top = RoundedRect(Inset(r, 4), 7))
            g.DrawPath(highlight, top);
        Fill(g, Inset(r, 7), 5, paper ? Paper : DarkFill);

        g.SmoothingMode = oldMode;
    }

    /// <summary>Label kecil berbingkai (nama pembicara di atas kotak dialog).</summary>
    public static void DrawTag(Graphics g, Rectangle r, string text, Font font)
    {
        Draw(g, r, paper: false);
        SizeF size = g.MeasureString(text, font);
        g.DrawString(text, font, Brushes.White, r.X + (r.Width - size.Width) / 2, r.Y + (r.Height - size.Height) / 2);
    }

    private static void Fill(Graphics g, Rectangle r, int radius, Color color)
    {
        using GraphicsPath path = RoundedRect(r, radius);
        using SolidBrush brush = new(color);
        g.FillPath(brush, path);
    }

    private static Rectangle Inset(Rectangle r, int amount)
    {
        r.Inflate(-amount, -amount);
        return r;
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        GraphicsPath path = new();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
