namespace FarmGame.UI;

/// <summary>
/// Teks melayang di atas dunia (MVP 5): "+6 Kayu" dengan ikon barang, "+15 energi", "-1g".
/// Muncul dengan efek "pop", naik pelan, lalu memudar. Posisi disimpan dalam koordinat dunia,
/// tapi digambar di koordinat layar (huruf tetap tajam, tidak ikut diperbesar x3).
/// </summary>
public class FloatingPopups
{
    private const float LifeSeconds = 1.3f;
    private const float RiseSpeed = 14f;   // piksel logis per detik

    private class Popup
    {
        public string Text = "";
        public string? Icon;
        public Color Color;
        public Vector2 WorldPosition;
        public float Age;
    }

    private readonly List<Popup> _popups = new();
    private readonly Font _font = new("Segoe UI", 11f, FontStyle.Bold);

    public void Add(string text, Vector2 worldPosition, Color color, string? icon = null)
    {
        // Popup yang muncul hampir bersamaan di tempat sama ditumpuk ke atas supaya tidak saling menimpa
        int stacked = _popups.Count(p => p.Age < 0.3f && Vector2.Distance(p.WorldPosition, worldPosition) < 12);
        _popups.Add(new Popup { Text = text, Icon = icon, Color = color, WorldPosition = worldPosition - new Vector2(0, stacked * 9) });
    }

    public void Clear() => _popups.Clear();

    public void Update(float deltaSeconds)
    {
        foreach (Popup popup in _popups)
        {
            popup.Age += deltaSeconds;
            popup.WorldPosition.Y -= RiseSpeed * deltaSeconds;
        }
        _popups.RemoveAll(p => p.Age >= LifeSeconds);
    }

    public void Draw(Graphics g, Camera camera, IAssetProvider assets)
    {
        foreach (Popup popup in _popups)
        {
            float alpha = Math.Clamp((LifeSeconds - popup.Age) / 0.4f, 0f, 1f);
            float pop = popup.Age < 0.12f ? 0.6f + popup.Age / 0.12f * 0.6f : Math.Max(1f, 1.2f - (popup.Age - 0.12f) * 2f);

            PointF screen = camera.WorldToScreen(popup.WorldPosition);
            SizeF size = g.MeasureString(popup.Text, _font);
            float iconSize = popup.Icon == null ? 0 : 24;
            float total = iconSize + size.Width;

            GraphicsState state = g.Save();
            g.TranslateTransform(screen.X, screen.Y);
            g.ScaleTransform(pop, pop);
            float x = -total / 2;

            if (popup.Icon != null)
            {
                float oldOpacity = assets.Opacity;
                assets.Opacity = alpha;
                assets.DrawSprite(g, popup.Icon, new RectangleF(x, -size.Height / 2 - 2, iconSize, iconSize));
                assets.Opacity = oldOpacity;
                x += iconSize;
            }

            using SolidBrush shadow = new(Color.FromArgb((int)(alpha * 200), 30, 20, 10));
            using SolidBrush text = new(Color.FromArgb((int)(alpha * 255), popup.Color));
            g.DrawString(popup.Text, _font, shadow, x + 1, -size.Height / 2 + 2);
            g.DrawString(popup.Text, _font, text, x, -size.Height / 2);
            g.Restore(state);
        }
    }
}
