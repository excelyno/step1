namespace FarmGame.Rendering;

/// <summary>
/// Warna langit: lapisan tipis di atas dunia sesuai jam.
/// Pagi sedikit keemasan, siang bening, sore oranye, malam biru gelap.
/// </summary>
public static class DayLight
{
    // (jam dalam menit, warna lapisan). Di antara dua titik, warnanya dicampur pelan-pelan.
    private static readonly (int Minutes, Color Tint)[] Keys =
    {
        (6 * 60,  Color.FromArgb(50, 255, 190, 120)),   // 06:00 fajar
        (8 * 60,  Color.FromArgb(0, 255, 255, 255)),    // 08:00 siang bening
        (16 * 60, Color.FromArgb(0, 255, 255, 255)),    // 16:00
        (18 * 60, Color.FromArgb(60, 255, 140, 60)),    // 18:00 senja oranye
        (20 * 60, Color.FromArgb(120, 40, 40, 110)),    // 20:00 malam
        (26 * 60, Color.FromArgb(160, 15, 15, 60)),     // 02:00 larut
    };

    public static void Draw(Graphics g, int timeOfDay)
    {
        Color tint = TintAt(timeOfDay);
        if (tint.A == 0)
            return;

        using SolidBrush brush = new(tint);
        g.FillRectangle(brush, 0, 0, GameConfig.ScreenWidth, GameConfig.ScreenHeight);
    }

    private static Color TintAt(int minutes)
    {
        if (minutes <= Keys[0].Minutes)
            return Keys[0].Tint;

        for (int i = 1; i < Keys.Length; i++)
        {
            if (minutes <= Keys[i].Minutes)
            {
                var (m0, c0) = Keys[i - 1];
                var (m1, c1) = Keys[i];
                float t = (float)(minutes - m0) / (m1 - m0);
                return Color.FromArgb(Mix(c0.A, c1.A, t), Mix(c0.R, c1.R, t), Mix(c0.G, c1.G, t), Mix(c0.B, c1.B, t));
            }
        }
        return Keys[^1].Tint;
    }

    private static int Mix(int a, int b, float t) => (int)(a + (b - a) * t);
}
