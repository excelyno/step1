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

    // Gerak
    public const float PlayerWalkSpeed = 64f;   // piksel logis per detik (4 tile/detik)
    public const float PlayerRunSpeed = 96f;    // 6 tile/detik
    public const float CameraFollowSpeed = 8f;  // makin besar, makin cepat kamera mengejar

    // MVP 1: bertani
    public const int InventorySize = 12;            // jumlah slot tas = toolbar
    public const int StartingGold = 500;
    public const int MaxStamina = 270;
    public const float ToolCooldownSeconds = 0.35f; // jeda setelah pakai alat (tidak bisa jalan)
    public const int WateringCanCapacity = 40;
    public const float SecondsPerTenMinutes = 7f;   // 10 menit di game = 7 detik nyata
    public const float PassedOutEnergyRatio = 0.5f; // pingsan: bangun dengan 50% energi
    public const float LateSleepEnergyRatio = 0.75f;// tidur lewat jam 24:00: 75% energi

    // Lokasi file
    public static readonly string MapFolder = Path.Combine("Content", "Maps");
    public static readonly string DataFolder = Path.Combine("Content", "Data");
}
