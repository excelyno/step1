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
    public const int ScreenWidth = ViewWidth * Scale;      // 960 piksel (ruang gambar game)
    public const int ScreenHeight = ViewHeight * Scale;    // 576 piksel (ruang gambar game)

    // Render scale dinamis (best practice pixel-art): back buffer = ViewWidth*RenderScale.
    // 1 = kasar; makin besar makin tajam. Diatur GameForm sesuai ukuran jendela/layar.
    // Semua kode game tetap menggambar di ruang 960x576 (Scale=3); faktor RenderScale/Scale
    // dipasang di paling atas Game.Draw, jadi sprite 16px tampil 16*RenderScale piksel = rapi.
    public const int MaxRenderScale = 8;
    private static int _renderScale = Scale;
    public static int RenderScale
    {
        get => _renderScale;
        set => _renderScale = Math.Clamp(value, 1, MaxRenderScale);
    }

    /// <summary>Ukuran back buffer sebenarnya (piksel perangkat).</summary>
    public static int SurfaceWidth => ViewWidth * RenderScale;
    public static int SurfaceHeight => ViewHeight * RenderScale;

    /// <summary>Faktor dari ruang game 960x576 ke back buffer.</summary>
    public static float UiFactor => RenderScale / (float)Scale;

    // Game loop
    public const int TimerInterval = 10;        // ms. Windows membulatkannya ke ±15,6 ms (±60 FPS)
    public const float MaxDeltaSeconds = 0.1f;  // batas dt supaya pemain tidak "teleport" saat jendela di-drag

    // Gerak
    public const float PlayerWalkSpeed = 64f;   // piksel logis per detik (4 tile/detik)
    public const float PlayerRunSpeed = 96f;    // 6 tile/detik
    public const float CameraFollowSpeed = 8f;  // makin besar, makin cepat kamera mengejar

    // MVP 1: bertani
    public const int InventorySize = 12;            // jumlah slot tas = toolbar
    public const int ShippingBinSize = 36;          // jumlah slot kotak kirim (6 x 6)
    public const int StartingGold = 500;
    public const int MaxStamina = 270;
    public const float ToolCooldownSeconds = 0.35f; // jeda setelah pakai alat (tidak bisa jalan)
    public const int WateringCanCapacity = 40;
    public const float SecondsPerTenMinutes = 7f;   // 10 menit di game = 7 detik nyata
    public const float PassedOutEnergyRatio = 0.5f; // pingsan: bangun dengan 50% energi
    public const float LateSleepEnergyRatio = 0.75f;// tidur lewat jam 24:00: 75% energi

    // Minigame bercocok tanam & hama
    public const int PestChancePercent = 12;   // peluang hama baru tiap pagi, per tanaman
    public const int PestPenaltyDays = 2;      // hama dibiarkan selama ini -> panen -1 buah

    // MVP 8: daya tahan alat (banyak pemakaian sampai hancur) dan harga di toko Bu Sari
    public const int HoeDurability = 40;    // cangkul tua warisan kakek
    public const int CanDurability = 90;
    public const int AxeDurability = 90;
    public const int PickaxeDurability = 90;
    public const int RodDurability = 120;
    public const int CanPrice = 80;
    public const int AxePrice = 100;
    public const int PickaxePrice = 100;
    public const int RodPrice = 120;

    // MVP 9: tas (backpack) 3 level. Level awal = 12 slot; tiap upgrade menambah slot.
    public const int BackpackBaseSlots = 12;
    public const int BackpackL1Slots = 18;
    public const int BackpackL2Slots = 24;
    public const int BackpackL1Price = 300;
    public const int BackpackL2Price = 800;

    // Lokasi file
    public static readonly string MapFolder = Path.Combine("Content", "Maps");
    public static readonly string DataFolder = Path.Combine("Content", "Data");

    // MVP 7: hutan prosedural di timur kebun
    public const int ForestWidth = 80;            // tile
    public const int ForestHeight = 56;           // tile
    public const int ForestInitialTrees = 900;    // pohon+cemara saat dunia dibuat
    public const int ForestMaxTrees = 1100;       // batas total pohon (anti membesar tanpa henti)
    public const int ForestRegrowPerDay = 8;      // pohon baru yang tumbuh tiap pagi (dibawah batas)
    public const int ForestMaxRocks = 220;        // batas batu di hutan
    public const int ForestRockPerDay = 2;        // batu baru tiap pagi
    public const int DefaultWorldSeed = 20250101; // dipakai simpanan lama yang belum menyimpan seed
}
