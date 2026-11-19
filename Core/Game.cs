namespace FarmGame.Core;

/// <summary>
/// Inti game: membuat dan memiliki semua objek utama, lalu menyerahkan Update/Draw ke state aktif.
/// Game juga mengimplementasikan IGameServices, supaya item/objek dunia bisa minta
/// "buat item", "tanggal hari ini", "tidur" tanpa mengenal kelas Game.
/// </summary>
public class Game : IGameServices
{
    private readonly Renderer _renderer;
    private GameState _state;

    // Penghitung FPS
    private int _frameCount;
    private float _fpsTimer;

    // ---------- Objek utama ----------
    public InputManager Input { get; } = new();
    public Camera Camera { get; } = new(GameConfig.ViewWidth, GameConfig.ViewHeight);
    public TimeManager Time { get; } = new();
    public CropDatabase Crops { get; }
    public ItemRegistry Items { get; }
    public Player Player { get; }
    public Farm Farm { get; }
    public GameLocation CurrentLocation { get; private set; }
    public Hud Hud { get; }
    public ToastQueue Toasts { get; } = new();
    public DebugOverlay Debug { get; } = new();
    public int Fps { get; private set; }

    /// <summary>Dari IGameServices: tanggal hari ini.</summary>
    public GameDate Date => Time.Date;

    public Game(IAssetProvider assets)
    {
        // 1. Data dari file
        Crops = new CropDatabase(Path.Combine(AppContext.BaseDirectory, GameConfig.DataFolder, "crops.json"));
        Items = new ItemRegistry(Crops);

        // 2. Tampilan
        _renderer = new Renderer(assets, Camera);
        Hud = new Hud(assets);

        // 3. Dunia dan pemain
        Farm = new Farm();
        CurrentLocation = Farm;
        Player = new Player("Petani");
        GiveStartingItems();
        ChangeLocation(Farm, Farm.WakeUpTile);

        // 4. Mulai dari mode bermain
        _state = new PlayingState(this);
        _state.Enter();
    }

    private void GiveStartingItems()
    {
        Player.Inventory.TryAdd(Items.Create("hoe"));
        Player.Inventory.TryAdd(Items.Create("watering_can"));
        Player.Inventory.TryAdd(Items.Create("parsnip_seeds"), 15);
    }

    /// <summary>Ganti mode game (State pattern).</summary>
    public void ChangeState(GameState next)
    {
        _state.Exit();
        _state = next;
        _state.Enter();
    }

    /// <summary>Memindahkan pemain ke tile tertentu di sebuah lokasi.</summary>
    public void ChangeLocation(GameLocation tujuan, Point tile)
    {
        CurrentLocation.Remove(Player);
        CurrentLocation = tujuan;

        Player.PlaceAt(tujuan.GetPositionOnTile(Player, tile));
        tujuan.Add(Player);

        Camera.SnapTo(Player);
        Camera.ClampTo(tujuan.Map.PixelWidth, tujuan.Map.PixelHeight);
    }

    // ---------- Tidur & ganti hari ----------

    /// <summary>Dari IGameServices: dipanggil pintu rumah.</summary>
    public void RequestSleep() => RequestSleep(SleepReason.Normal);

    public void RequestSleep(SleepReason reason)
    {
        if (_state is DayEndState)
            return;   // sudah sedang tidur, abaikan
        ChangeState(new DayEndState(this, reason));
    }

    /// <summary>
    /// Proses malam hari. URUTAN PENTING:
    /// jual dulu -> ganti tanggal -> tanaman tumbuh -> energi pulih -> pemain bangun di depan pintu.
    /// </summary>
    public DaySummary EndDay(SleepReason reason)
    {
        GameDate endedDate = Time.Date;

        // Energi saat bangun tergantung jam tidur (dihitung sebelum jam direset)
        float energyRatio = 1f;
        if (reason == SleepReason.PassedOut)
            energyRatio = GameConfig.PassedOutEnergyRatio;
        else if (Time.TimeOfDay >= TimeManager.MidnightMinutes)
            energyRatio = GameConfig.LateSleepEnergyRatio;

        // 1. Jual isi kotak kirim
        List<ShipmentLine> sold = Farm.ShippingBin.ProcessShipment(Player.Wallet);

        // 2. Ganti tanggal
        Time.StartNextDay();

        // 3. Tanaman tumbuh (memakai musim BARU)
        Farm.OnNewDay(Time.Date);

        // 4. Energi pulih
        Player.Stamina.RestoreTo(energyRatio);

        // 5. Bangun di depan pintu rumah
        ChangeLocation(Farm, Farm.WakeUpTile);

        return new DaySummary(endedDate, Time.Date, reason, sold, Player.Wallet.Gold, energyRatio);
    }

    // ---------- Loop ----------

    public void Update(float deltaSeconds)
    {
        CountFps(deltaSeconds);

        if (Input.WasPressed(Keys.F3))
            Debug.Toggle();

        _state.Update(deltaSeconds);   // polimorfisme: bermain atau ganti hari

        Input.EndFrame();
    }

    public void Draw(Graphics g)
    {
        _state.Draw(g);
    }

    /// <summary>Kamera mengikuti pemain, lalu dibatasi ke tepi map.</summary>
    public void UpdateCamera(float deltaSeconds)
    {
        Camera.Follow(Player, deltaSeconds);
        Camera.ClampTo(CurrentLocation.Map.PixelWidth, CurrentLocation.Map.PixelHeight);
    }

    /// <summary>Gambar dunia, info debug (kalau F3), lalu HUD. Dipakai oleh semua state.</summary>
    public void DrawWorldAndHud(Graphics g)
    {
        _renderer.Render(g, CurrentLocation);

        if (Debug.IsVisible)
            Debug.Draw(g, this);

        Hud.Draw(g, Player, Time);
    }

    private void CountFps(float deltaSeconds)
    {
        _frameCount++;
        _fpsTimer += deltaSeconds;
        if (_fpsTimer >= 1f)
        {
            Fps = _frameCount;
            _frameCount = 0;
            _fpsTimer -= 1f;
        }
    }
}
