namespace FarmGame.Rendering;

/// <summary>Satu sumber cahaya bulat di dunia (piksel logis): lampu, jendela, api, kunang-kunang.</summary>
public readonly struct Light
{
    public Vector2 Position { get; }
    public float Radius { get; }
    public Color Color { get; }
    public float Intensity { get; }   // 0..1, seberapa kuat menghapus gelap

    public Light(Vector2 position, float radius, Color color, float intensity = 1f)
    {
        Position = position;
        Radius = radius;
        Color = color;
        Intensity = intensity;
    }

    /// <summary>Warna cahaya lampu & jendela (kuning hangat).</summary>
    public static readonly Color Warm = Color.FromArgb(255, 205, 120);
}
