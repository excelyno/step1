using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FarmGame.Rendering;

/// <summary>
/// Pencahayaan (MVP 4, menggantikan DayLight):
///   1. Warna "udara" sesuai jam: fajar keemasan, siang bening, senja oranye, malam biru gelap.
///      Di dalam rumah selalu agak redup, malam lebih gelap.
///   2. Saat cukup gelap: dibuat "peta cahaya" kecil seukuran layar logis (320x192).
///      Setiap piksel = gelap, kecuali dekat lampu/jendela/api -> gelapnya berkurang dan diberi warna hangat.
///   3. Peta itu diperbesar 3x dengan filter halus, jadi cahaya tampak lembut (bukan kotak-kotak).
///   4. "Bloom": lingkaran cahaya tipis di atas lampu yang kuat, supaya lampunya terlihat berpendar.
/// </summary>
public class Lighting
{
    private const int MinDarknessForLights = 40;   // di bawah ini cukup diwarnai rata
    private const int GlowAlpha = 45;              // sisa warna hangat di pusat cahaya
    private const float BloomMinIntensity = 0.7f;  // hanya lampu yang kuat yang berpendar
    private const int BloomMaxAlpha = 110;

    private readonly int _width = GameConfig.ViewWidth;
    private readonly int _height = GameConfig.ViewHeight;
    private readonly Bitmap _lightMap;
    private readonly int[] _pixels;
    private readonly float[] _amount, _red, _green, _blue;   // jumlah cahaya + warna per piksel
    private readonly ImageAttributes _smoothEdges = new();

    // (jam dalam menit, warna udara). Di antara dua titik, warnanya dicampur pelan-pelan.
    private static readonly (int Minutes, Color Tint)[] OutdoorKeys =
    {
        (6 * 60,       Color.FromArgb(70, 255, 180, 110)),   // 06:00 fajar
        (8 * 60,       Color.FromArgb(0, 255, 255, 255)),    // 08:00 siang bening
        (16 * 60,      Color.FromArgb(0, 255, 255, 255)),    // 16:00
        (18 * 60,      Color.FromArgb(70, 255, 130, 50)),    // 18:00 senja oranye
        (19 * 60 + 30, Color.FromArgb(125, 70, 40, 110)),    // 19:30 senja ungu
        (21 * 60,      Color.FromArgb(190, 14, 16, 54)),     // 21:00 malam
        (26 * 60,      Color.FromArgb(215, 8, 8, 34)),       // 02:00 larut
    };

    private static readonly (int Minutes, Color Tint)[] IndoorKeys =
    {
        (6 * 60,  Color.FromArgb(70, 30, 20, 40)),
        (9 * 60,  Color.FromArgb(35, 30, 20, 40)),
        (17 * 60, Color.FromArgb(45, 30, 20, 40)),
        (20 * 60, Color.FromArgb(150, 16, 12, 34)),
        (26 * 60, Color.FromArgb(185, 8, 6, 24)),
    };

    public Lighting()
    {
        _lightMap = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
        int count = _width * _height;
        _pixels = new int[count];
        _amount = new float[count];
        _red = new float[count];
        _green = new float[count];
        _blue = new float[count];
        _smoothEdges.SetWrapMode(WrapMode.TileFlipXY);   // cegah garis gelap di tepi saat diperbesar
    }

    public void Draw(Graphics g, Camera camera, int timeOfDay, bool outdoors, IEnumerable<Light> lights)
    {
        Color ambient = TintAt(outdoors ? OutdoorKeys : IndoorKeys, timeOfDay);
        if (ambient.A == 0)
            return;

        if (ambient.A < MinDarknessForLights)
        {
            using SolidBrush flat = new(ambient);
            g.FillRectangle(flat, 0, 0, GameConfig.ScreenWidth, GameConfig.ScreenHeight);
            return;
        }

        Vector2 origin = camera.DrawPosition;
        List<Light> lightList = lights.ToList();
        BuildLightMap(ambient, origin, lightList);

        InterpolationMode oldMode = g.InterpolationMode;
        g.InterpolationMode = InterpolationMode.HighQualityBilinear;
        g.DrawImage(_lightMap, new Rectangle(0, 0, GameConfig.ScreenWidth, GameConfig.ScreenHeight),
                    0, 0, _width, _height, GraphicsUnit.Pixel, _smoothEdges);
        g.InterpolationMode = oldMode;

        DrawBloom(g, origin, lightList, ambient.A / 255f);
    }

    /// <summary>Pendar lembut di sekitar sumber cahaya. Makin gelap malamnya, makin terang pendarnya.</summary>
    private static void DrawBloom(Graphics g, Vector2 origin, List<Light> lights, float darkness)
    {
        int alpha = (int)(BloomMaxAlpha * darkness);
        float scale = GameConfig.Scale;

        foreach (Light light in lights)
        {
            if (light.Intensity < BloomMinIntensity)
                continue;

            float radius = light.Radius * 0.3f * scale;
            float x = (light.Position.X - origin.X) * scale, y = (light.Position.Y - origin.Y) * scale;
            if (x < -radius || y < -radius || x > GameConfig.ScreenWidth + radius || y > GameConfig.ScreenHeight + radius)
                continue;

            using GraphicsPath circle = new();
            circle.AddEllipse(x - radius, y - radius, radius * 2, radius * 2);
            using PathGradientBrush glow = new(circle)
            {
                CenterColor = Color.FromArgb(alpha, light.Color),
                SurroundColors = new[] { Color.FromArgb(0, light.Color) },
            };
            g.FillPath(glow, circle);
        }
    }

    private void BuildLightMap(Color ambient, Vector2 origin, List<Light> lights)
    {
        Array.Clear(_amount);
        Array.Clear(_red);
        Array.Clear(_green);
        Array.Clear(_blue);

        // 1. Tambahkan setiap cahaya, hanya di kotak sekitar cahaya itu (lebih cepat)
        foreach (Light light in lights)
            AddLight(light, origin);

        // 2. Gabungkan: gelap berkurang sebanyak cahaya, warna bergeser ke warna cahaya
        for (int i = 0; i < _pixels.Length; i++)
        {
            float amount = Math.Min(1f, _amount[i]);
            int alpha, r, g, b;
            if (amount <= 0.001f)
            {
                alpha = ambient.A; r = ambient.R; g = ambient.G; b = ambient.B;
            }
            else
            {
                float total = _amount[i];
                alpha = (int)(ambient.A * (1 - amount) + GlowAlpha * amount);
                r = (int)(ambient.R + (_red[i] / total - ambient.R) * amount);
                g = (int)(ambient.G + (_green[i] / total - ambient.G) * amount);
                b = (int)(ambient.B + (_blue[i] / total - ambient.B) * amount);
            }
            _pixels[i] = (alpha << 24) | (r << 16) | (g << 8) | b;
        }

        BitmapData data = _lightMap.LockBits(new Rectangle(0, 0, _width, _height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(_pixels, 0, data.Scan0, _pixels.Length);
        _lightMap.UnlockBits(data);
    }

    private void AddLight(Light light, Vector2 origin)
    {
        float cx = light.Position.X - origin.X;
        float cy = light.Position.Y - origin.Y;
        float radius = light.Radius;

        int x0 = Math.Max(0, (int)(cx - radius)), x1 = Math.Min(_width - 1, (int)(cx + radius));
        int y0 = Math.Max(0, (int)(cy - radius)), y1 = Math.Min(_height - 1, (int)(cy + radius));
        if (x0 > x1 || y0 > y1)
            return;   // di luar layar

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float distance = MathF.Sqrt(dx * dx + dy * dy);
                if (distance >= radius)
                    continue;

                float falloff = 1f - distance / radius;
                float strength = falloff * falloff * light.Intensity;   // lembut di pinggir
                int i = y * _width + x;
                _amount[i] += strength;
                _red[i] += light.Color.R * strength;
                _green[i] += light.Color.G * strength;
                _blue[i] += light.Color.B * strength;
            }
        }
    }

    private static Color TintAt((int Minutes, Color Tint)[] keys, int minutes)
    {
        if (minutes <= keys[0].Minutes)
            return keys[0].Tint;

        for (int i = 1; i < keys.Length; i++)
        {
            if (minutes <= keys[i].Minutes)
            {
                var (m0, c0) = keys[i - 1];
                var (m1, c1) = keys[i];
                float t = (float)(minutes - m0) / (m1 - m0);
                return Color.FromArgb(Mix(c0.A, c1.A, t), Mix(c0.R, c1.R, t), Mix(c0.G, c1.G, t), Mix(c0.B, c1.B, t));
            }
        }
        return keys[^1].Tint;
    }

    private static int Mix(int a, int b, float t) => (int)(a + (b - a) * t);
}
