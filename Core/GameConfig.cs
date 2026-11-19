namespace FarmGame.Core;

/// <summary>Semua angka pengaturan game dikumpulkan di sini (tidak ada "angka ajaib" di kode lain).</summary>
public static class GameConfig
{
    // Ukuran tile dan skala layar
    public const int TileSize = 16;     // 1 tile = 16 piksel logis
    public const int Scale = 3;         // 1 piksel logis = 3 piksel layar

    // Ukuran pandangan kamera
    public const int ViewTilesX = 20;
    public const int ViewTilesY = 12;
    public const int ViewWidth = ViewTilesX * TileSize;    // 320 piksel logis
    public const int ViewHeight = ViewTilesY * TileSize;   // 192 piksel logis
    public const int ScreenWidth = ViewWidth * Scale;      // 960 piksel layar
    public const int ScreenHeight = ViewHeight * Scale;    // 576 piksel layar

    // Game loop
    public const int TimerInterval = 10;        // ms. Windows membulatkannya ke ±15,6 ms (±60 FPS)
    public const float MaxDeltaSeconds = 0.1f;  // batas dt supaya pemain tidak "teleport" saat jendela di-drag

    // Gameplay
    public const float PlayerWalkSpeed = 64f;   // piksel logis per detik (4 tile/detik)
    public const float PlayerRunSpeed = 96f;    // 6 tile/detik
    public const float CameraFollowSpeed = 8f;  // makin besar, makin cepat kamera mengejar

    // Lokasi file map
    public static readonly string MapFolder = Path.Combine("Content", "Maps");
}
