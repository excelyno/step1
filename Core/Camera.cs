namespace FarmGame.Core;

/// <summary>
/// Menentukan bagian dunia yang terlihat di layar.
/// Position = pojok kiri-atas pandangan (piksel logis).
/// </summary>
public class Camera
{
    public Vector2 Position { get; private set; }
    public int ViewWidth { get; }
    public int ViewHeight { get; }

    public RectangleF ViewArea => new(Position.X, Position.Y, ViewWidth, ViewHeight);

    // MVP 5: getaran layar (pohon tumbang). Hanya mengubah posisi GAMBAR, bukan posisi kamera sebenarnya.
    private static readonly Random ShakeRng = new();
    private float _shakeStrength;
    private float _shakeTimer;
    private float _shakeDuration;
    private Vector2 _shakeOffset;

    /// <summary>Pojok kiri-atas yang dipakai untuk menggambar (posisi + getaran), dibulatkan ke piksel.</summary>
    public Vector2 DrawPosition => new(MathF.Round(Position.X + _shakeOffset.X), MathF.Round(Position.Y + _shakeOffset.Y));

    /// <summary>Mulai bergetar. Getaran yang lebih kuat menimpa yang lebih lemah.</summary>
    public void Shake(float strength, float seconds)
    {
        if (strength < _shakeStrength && _shakeTimer > 0)
            return;
        _shakeStrength = strength;
        _shakeTimer = _shakeDuration = seconds;
    }

    /// <summary>Getaran mereda pelan-pelan sampai berhenti.</summary>
    public void UpdateShake(float deltaSeconds)
    {
        if (_shakeTimer <= 0)
        {
            _shakeOffset = Vector2.Zero;
            _shakeStrength = 0;
            return;
        }
        _shakeTimer -= deltaSeconds;
        float power = _shakeStrength * Math.Max(0, _shakeTimer / _shakeDuration);
        _shakeOffset = new Vector2((float)(ShakeRng.NextDouble() * 2 - 1) * power, (float)(ShakeRng.NextDouble() * 2 - 1) * power);
    }

    public Camera(int viewWidth, int viewHeight)
    {
        ViewWidth = viewWidth;
        ViewHeight = viewHeight;
    }

    /// <summary>Mengikuti target pelan-pelan (lerp) supaya gerakan kamera halus.</summary>
    public void Follow(GameObject target, float deltaSeconds)
    {
        Vector2 tujuan = target.Center - new Vector2(ViewWidth / 2f, ViewHeight / 2f);
        float t = Math.Min(1f, GameConfig.CameraFollowSpeed * deltaSeconds);
        Position = Vector2.Lerp(Position, tujuan, t);
    }

    /// <summary>MVP 5: atur posisi langsung (kamera layar judul).</summary>
    public void SetPosition(Vector2 position) => Position = position;

    /// <summary>Langsung pindah ke target tanpa lerp (dipakai saat spawn / ganti lokasi).</summary>
    public void SnapTo(GameObject target)
    {
        Position = target.Center - new Vector2(ViewWidth / 2f, ViewHeight / 2f);
    }

    /// <summary>Menjaga kamera supaya tidak menampilkan area di luar map.</summary>
    public void ClampTo(int mapWidth, int mapHeight)
    {
        float x, y;

        if (mapWidth <= ViewWidth)
            x = (mapWidth - ViewWidth) / 2f;   // map lebih kecil dari layar: taruh di tengah
        else
            x = Math.Clamp(Position.X, 0, mapWidth - ViewWidth);

        if (mapHeight <= ViewHeight)
            y = (mapHeight - ViewHeight) / 2f;
        else
            y = Math.Clamp(Position.Y, 0, mapHeight - ViewHeight);

        Position = new Vector2(x, y);
    }

    /// <summary>MVP 3: posisi dunia (piksel logis) -> posisi layar. Dipakai untuk menggambar kail pancing di atas dunia.</summary>
    public PointF WorldToScreen(Vector2 world)
    {
        return new PointF((world.X - DrawPosition.X) * GameConfig.Scale,
                          (world.Y - DrawPosition.Y) * GameConfig.Scale);
    }

    /// <summary>Mengubah posisi mouse (layar) menjadi posisi dunia. Disiapkan untuk MVP 1.</summary>
    public Vector2 ScreenToWorld(Point screen)
    {
        return new Vector2(screen.X / (float)GameConfig.Scale + Position.X,
                           screen.Y / (float)GameConfig.Scale + Position.Y);
    }
}
