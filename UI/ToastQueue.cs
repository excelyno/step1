namespace FarmGame.UI;

/// <summary>
/// Pesan singkat di kiri bawah yang hilang sendiri setelah 2,5 detik.
/// Hijau = berhasil, oranye = gagal. Maksimal 4 pesan sekaligus.
/// </summary>
public class ToastQueue
{
    private const int MaxToasts = 4;
    private const float LifeSeconds = 2.5f;
    private const float FadeSeconds = 0.5f;

    /// <summary>Satu pesan.</summary>
    private class Toast
    {
        public string Text = "";
        public bool Success;
        public float TimeLeft;
    }

    private readonly List<Toast> _toasts = new();
    private readonly Font _font = new("Segoe UI", 11f, FontStyle.Bold);

    public void Push(string text, bool success)
    {
        // Pesan yang sama tidak ditumpuk, cukup diperpanjang
        foreach (Toast existing in _toasts)
        {
            if (existing.Text == text)
            {
                existing.TimeLeft = LifeSeconds;
                return;
            }
        }

        _toasts.Add(new Toast { Text = text, Success = success, TimeLeft = LifeSeconds });
        if (_toasts.Count > MaxToasts)
            _toasts.RemoveAt(0);   // buang yang paling lama
    }

    public void Clear() => _toasts.Clear();

    public void Update(float deltaSeconds)
    {
        foreach (Toast toast in _toasts)
            toast.TimeLeft -= deltaSeconds;
        _toasts.RemoveAll(toast => toast.TimeLeft <= 0);
    }

    public void Draw(Graphics g)
    {
        const int height = 30, gap = 4, left = 8;
        int y = GameConfig.ScreenHeight - 70;   // di atas toolbar

        // Yang terbaru paling bawah
        for (int i = _toasts.Count - 1; i >= 0; i--)
        {
            Toast toast = _toasts[i];
            int alpha = (int)(255 * Math.Clamp(toast.TimeLeft / FadeSeconds, 0f, 1f));   // memudar di akhir
            int width = (int)g.MeasureString(toast.Text, _font).Width + 24;
            y -= height;

            using SolidBrush background = new(Color.FromArgb(alpha * 200 / 255, 30, 30, 30));
            using SolidBrush accent = new(toast.Success
                ? Color.FromArgb(alpha, 90, 200, 90)
                : Color.FromArgb(alpha, 240, 150, 50));
            using SolidBrush text = new(Color.FromArgb(alpha, 255, 255, 255));

            g.FillRectangle(background, left, y, width, height);
            g.FillRectangle(accent, left, y, 6, height);
            g.DrawString(toast.Text, _font, text, left + 14, y + 6);

            y -= gap;
        }
    }
}
