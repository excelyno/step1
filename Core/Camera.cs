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

    /// <summary>Mengubah posisi mouse (layar) menjadi posisi dunia. Disiapkan untuk MVP 1.</summary>
    public Vector2 ScreenToWorld(Point screen)
    {
        return new Vector2(screen.X / (float)GameConfig.Scale + Position.X,
                           screen.Y / (float)GameConfig.Scale + Position.Y);
    }
}
