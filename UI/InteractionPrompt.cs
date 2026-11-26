namespace FarmGame.UI;

/// <summary>
/// Gelembung petunjuk di atas benda yang bisa diperiksa (MVP 5): tombol [X] + kata kerjanya ("Bicara", "Panen").
/// Muncul dengan efek "pop" dan naik-turun pelan. Juga menggambar kotak sasaran (kursor tile) saat memegang alat/bibit.
/// </summary>
public class InteractionPrompt
{
    private readonly Font _font = new("Segoe UI", 10f, FontStyle.Bold);
    private readonly Font _keyFont = new("Segoe UI", 9f, FontStyle.Bold);

    private object? _target;
    private float _age;
    private string _label = "";
    private RectangleF _anchor;   // gambar benda sasaran (koordinat dunia)

    private Point? _cursorTile;
    private bool _cursorUseful;   // ada yang bisa dikerjakan di tile itu (kursor lebih terang)
    private float _clock;

    /// <summary>Dipanggil tiap frame oleh Game dengan sasaran saat ini (null = tidak ada).</summary>
    public void Update(float deltaSeconds, IInteractable? target, GameObject? targetObject, Player player)
    {
        _clock += deltaSeconds;
        if (!ReferenceEquals(target, _target))
        {
            _target = target;
            _age = 0;
        }
        _age += deltaSeconds;
        if (target != null && targetObject != null)
        {
            _label = target.InteractLabel(player);
            _anchor = targetObject.SpriteBounds;
        }
    }

    /// <summary>Kursor tile: tile depan pemain kalau sedang memegang alat atau bibit.</summary>
    public void SetCursor(Point? tile, bool useful)
    {
        _cursorTile = tile;
        _cursorUseful = useful;
    }

    public void Clear()
    {
        _target = null;
        _cursorTile = null;
    }

    /// <summary>Kursor digambar di koordinat dunia (dipanggil di dalam transform kamera).</summary>
    public void DrawCursor(Graphics g)
    {
        if (_cursorTile == null)
            return;
        int size = GameConfig.TileSize;
        float pulse = 0.5f + 0.5f * MathF.Sin(_clock * 6f);
        int alpha = _cursorUseful ? 150 + (int)(pulse * 90) : 70;
        RectangleF r = new(_cursorTile.Value.X * size + 0.5f, _cursorTile.Value.Y * size + 0.5f, size - 1, size - 1);

        using Pen pen = new(Color.FromArgb(alpha, 255, 250, 220), 1f);
        // hanya pojok-pojoknya, seperti bingkai bidik
        float c = 4;
        g.DrawLines(pen, new[] { new PointF(r.Left, r.Top + c), new PointF(r.Left, r.Top), new PointF(r.Left + c, r.Top) });
        g.DrawLines(pen, new[] { new PointF(r.Right - c, r.Top), new PointF(r.Right, r.Top), new PointF(r.Right, r.Top + c) });
        g.DrawLines(pen, new[] { new PointF(r.Left, r.Bottom - c), new PointF(r.Left, r.Bottom), new PointF(r.Left + c, r.Bottom) });
        g.DrawLines(pen, new[] { new PointF(r.Right - c, r.Bottom), new PointF(r.Right, r.Bottom), new PointF(r.Right, r.Bottom - c) });
        if (_cursorUseful)
        {
            using SolidBrush fill = new(Color.FromArgb(28, 255, 250, 220));
            g.FillRectangle(fill, r);
        }
    }

    /// <summary>Gelembung digambar di koordinat layar.</summary>
    public void DrawBubble(Graphics g, Camera camera)
    {
        if (_target == null)
            return;

        float pop = _age < 0.14f ? 0.5f + _age / 0.14f * 0.65f : Math.Max(1f, 1.15f - (_age - 0.14f) * 1.5f);
        float bob = MathF.Sin(_clock * 3.5f) * 2f;
        PointF top = camera.WorldToScreen(new Vector2(_anchor.X + _anchor.Width / 2, _anchor.Top));

        SizeF text = g.MeasureString(_label, _font);
        float keyW = 20, pad = 6;
        float width = keyW + pad * 3 + text.Width;
        float height = 26;

        GraphicsState state = g.Save();
        g.TranslateTransform(top.X, Math.Max(height + 4, top.Y - 8 + bob));
        g.ScaleTransform(pop, pop);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        RectangleF box = new(-width / 2, -height, width, height);
        using SolidBrush back = new(Color.FromArgb(235, 250, 242, 220));
        using Pen border = new(Color.FromArgb(120, 80, 45), 2f);
        using GraphicsPath shape = RoundedBox(box, 7);
        g.FillPath(back, shape);
        g.DrawPath(border, shape);
        // ekor kecil yang menunjuk ke benda
        PointF[] tail = { new(-5, -1), new(5, -1), new(0, 6) };
        g.FillPolygon(back, tail);
        g.DrawLine(border, -5, 0, 0, 6);
        g.DrawLine(border, 0, 6, 5, 0);

        // tombol X
        RectangleF key = new(box.X + pad, box.Y + 4, keyW, height - 8);
        using SolidBrush keyBack = new(Color.FromArgb(80, 55, 35));
        using GraphicsPath keyShape = RoundedBox(key, 4);
        g.FillPath(keyBack, keyShape);
        SizeF x = g.MeasureString("X", _keyFont);
        g.DrawString("X", _keyFont, Brushes.White, key.X + (key.Width - x.Width) / 2, key.Y + (key.Height - x.Height) / 2);

        using SolidBrush ink = new(Color.FromArgb(70, 45, 25));
        g.DrawString(_label, _font, ink, key.Right + pad, box.Y + (height - text.Height) / 2);
        g.Restore(state);
    }

    private static GraphicsPath RoundedBox(RectangleF r, float radius)
    {
        float d = radius * 2;
        GraphicsPath path = new();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
