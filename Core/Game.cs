namespace FarmGame.Core;

/// <summary>
/// Inti game: membuat dan memiliki semua objek utama, lalu menyerahkan Update/Draw ke state aktif.
/// Game juga mengimplementasikan IGameServices, supaya item/objek dunia bisa minta
/// "buat item", "tanggal hari ini", "tidur" tanpa mengenal kelas Game.
/// MVP 5: dunia bisa dibuat ulang (Main Baru / Lanjutkan dari simpanan), layar judul, widget umpan balik.
/// </summary>
public class Game : IGameServices
{
    private readonly Renderer _renderer;
    private readonly Lighting _lighting = new();     // MVP 4
    private readonly Vignette _vignette = new();     // MVP 4
    private readonly Random _rng = new();
    private GameState _state;

    // MVP 2: semua lokasi, dicari lewat nama (dipakai Warp)
    private readonly Dictionary<string, GameLocation> _locations = new();

    // MVP 5: nama lokasi untuk spanduk saat datang
    private static readonly Dictionary<string, (string Title, string Subtitle)> LocationBanners = new()
    {
        { Farm.LocationName, ("Kebun Kakek", "Ladang, sumur, dan kolam di tepi hutan") },
        { Town.LocationName, ("Desa Tandur", "Alun-alun, toko bibit, dan dermaga sungai") },
        { Forest.LocationName, ("Hutan Tandur", "Kayu, batu, dan barang liar — pulih setiap hari") },
    };

    private bool _dayBannerPending;
    private float _rippleTimer;

    // Penghitung FPS
    private int _frameCount;
    private float _fpsTimer;

    // ---------- Objek utama ----------
    public InputManager Input { get; } = new();
    public Camera Camera { get; } = new(GameConfig.ViewWidth, GameConfig.ViewHeight);
    public TimeManager Time { get; private set; } = new();
    public CropDatabase Crops { get; }
    public FishDatabase Fish { get; }
    public LessonDatabase LessonData { get; }                    // minigame bercocok tanam
    public LessonBook Lessons { get; private set; } = null!;     // bintang pemain (dibuat ulang tiap dunia baru)
    public ItemRegistry Items { get; }
    public NpcDatabase Npcs { get; }                             // MVP 4
    public QuestLog Quests { get; private set; } = null!;        // MVP 4 (dibuat ulang tiap dunia baru)
    public MailSystem Mail { get; private set; } = null!;        // MVP 5
    public Player Player { get; private set; } = null!;
    public Farm Farm { get; private set; } = null!;
    public Town Town { get; private set; } = null!;
    public FarmHouse FarmHouse { get; private set; } = null!;
    public Forest Forest { get; private set; } = null!;
    /// <summary>Seed dunia: bentuk hutan prosedural. Disimpan di save.json agar hutan sama saat dimuat.</summary>
    public int WorldSeed { get; private set; }
    public SeedShop SeedShop { get; }
    public GameLocation CurrentLocation { get; private set; } = null!;
    public IEnumerable<GameLocation> Locations => _locations.Values;
    public IAssetProvider Assets { get; }
    public SoundPlayer Sounds { get; } = new();

    // MVP 6: gambar tile dirender sekali per potongan peta (ChunkCache), bukan per frame
    private readonly Dictionary<string, ChunkCache> _chunkCaches = new();

    /// <summary>Cache potongan peta untuk lokasi ini. Menggambar tile jadi sangat murah.</summary>
    public ChunkCache ChunksFor(GameLocation location)
    {
        if (!_chunkCaches.TryGetValue(location.Name, out ChunkCache? cache))
        {
            cache = new ChunkCache(location.Map, (SpriteAssets)Assets);
            _chunkCaches[location.Name] = cache;
        }
        return cache;
    }
    public Hud Hud { get; }
    public ShopMenu ShopMenu { get; }
    public ShippingBinMenu ShippingBinMenu { get; }
    public CropLessonView CropLessonView { get; }
    public FishingView FishingView { get; } = new();
    public DialogueBox DialogueBox { get; }                      // MVP 4
    public JournalMenu Journal { get; }                          // MVP 4
    public ParticleSystem Particles { get; } = new();            // MVP 4
    public FloatingPopups Popups { get; } = new();               // MVP 5
    public InteractionPrompt Prompt { get; } = new();            // MVP 5
    public ToastQueue Toasts { get; } = new();
    public DebugOverlay Debug { get; } = new();
    public int Fps { get; private set; }

    /// <summary>MVP 5: menu judul minta game ditutup.</summary>
    public bool ExitRequested { get; private set; }

    /// <summary>Mode yang sedang aktif (bermain, ganti hari, pindah lokasi, toko).</summary>
    public GameState CurrentState => _state;

    /// <summary>Dari IGameServices: tanggal hari ini.</summary>
    public GameDate Date => Time.Date;

    /// <summary>Dari IGameServices: jam sekarang (menit sejak 00:00).</summary>
    public int TimeOfDay => Time.TimeOfDay;

    public Game(IAssetProvider assets)
    {
        Assets = assets;

        // 1. Data dari file
        Crops = new CropDatabase(DataPath("crops.json"));
        Fish = new FishDatabase(DataPath("fish.json"));
        LessonData = new LessonDatabase(DataPath("lessons.json"), Crops);
        Items = new ItemRegistry(Crops, Fish);
        SeedShop = new SeedShop(Crops, Items);
        Npcs = new NpcDatabase(DataPath("npcs.json"));

        // 2. Tampilan
        _renderer = new Renderer(assets, Camera, ChunksFor);
        Hud = new Hud(assets);
        ShopMenu = new ShopMenu(assets);
        ShippingBinMenu = new ShippingBinMenu(assets);
        CropLessonView = new CropLessonView(assets);
        Journal = new JournalMenu(assets);
        DialogueBox = new DialogueBox(assets);

        // 3. Dunia untuk latar layar judul (kebun di pagi hari)
        CreateWorld();
        ChangeLocation(Farm, Farm.FrontOfDoorTile);

        // 4. MVP 5: mulai dari layar judul
        _state = new TitleState(this);
        _state.Enter();
    }

    private static string DataPath(string file) => Path.Combine(AppContext.BaseDirectory, GameConfig.DataFolder, file);

    // ---------- MVP 5: membuat dunia, main baru, lanjutkan ----------

    /// <summary>Buat semua yang berubah selama bermain dari nol: waktu, quest, surat, lokasi, pemain.</summary>
    private void CreateWorld(int? seed = null)
    {
        Time = new TimeManager();
        Quests = new QuestLog(DataPath("quests.json"));
        Quests.Validate(Npcs, Items);
        Mail = new MailSystem(DataPath("mail.json"));
        Lessons = new LessonBook();

        WorldSeed = seed ?? NewWorldSeed();
        _locations.Clear();
        DisposeChunkCaches();   // peta bisa berubah (seed hutan baru): cache tile dari dunia lama tidak boleh dipakai
        Farm = new Farm();
        Town = new Town(SeedShop, Npcs);
        FarmHouse = new FarmHouse();
        Forest = new Forest(WorldSeed);
        AddLocation(Farm);
        AddLocation(Town);
        AddLocation(FarmHouse);
        AddLocation(Forest);

        Player = new Player("Petani");
        CurrentLocation = FarmHouse;
        Particles.Clear();
        Popups.Clear();
        Prompt.Clear();
        Toasts.Clear();
        Hud.ResetGold();
    }

    /// <summary>Main Baru: dunia baru, barang awal, bangun di rumah, lalu surat Kakek.</summary>
    public void StartNewGame()
    {
        CreateWorld();
        GiveStartingItems();
        ChangeLocation(FarmHouse, FarmHouse.WakeUpTile);   // MVP 3: hari pertama mulai di dalam rumah
        Mail.Deliver(Date);

        ChangeState(new PlayingState(this));
        _dayBannerPending = true;
        ShowIntro();
    }

    /// <summary>Lanjutkan: dunia baru lalu diisi dari file simpanan. False kalau tidak ada simpanan yang bisa dibaca.</summary>
    public bool ContinueGame()
    {
        SaveData? data = SaveSystem.Read();
        if (data == null)
            return false;

        // Seed 0 = simpanan lama sebelum ada hutan: pakai seed default supaya bentuknya stabil
        CreateWorld(data.WorldSeed == 0 ? GameConfig.DefaultWorldSeed : data.WorldSeed);
        SaveSystem.Apply(this, data);
        Hud.ResetGold();
        ChangeState(new PlayingState(this));
        Hud.ShowBanner(Date.ToString(), "Selamat datang kembali!");
        return true;
    }

    /// <summary>Simpan sekarang. Gagal menulis file (disk penuh, dsb.) = pesan, bukan crash.</summary>
    public bool SaveGame()
    {
        try
        {
            SaveSystem.Save(this);
            Hud.ShowSaved();
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Notify("Gagal menyimpan: " + e.Message, false);
            return false;
        }
    }

    /// <summary>Kembali ke layar judul (dari menu jeda).</summary>
    public void ReturnToTitle()
    {
        CreateWorld();
        ChangeLocation(Farm, Farm.FrontOfDoorTile);
        ChangeState(new TitleState(this));
    }

    public void RequestExit() => ExitRequested = true;

    /// <summary>MVP 4: surat kakek di hari pertama. Setelah dibaca, quest pertama mulai.</summary>
    private void ShowIntro()
    {
        string[] letter =
        {
            "Cucuku tersayang,",
            "Kalau kamu membaca surat ini, berarti kebunku sekarang milikmu.",
            "Dulu kebun ini yang paling subur di Desa Tandur. Sekarang penuh ranting dan batu karena aku sudah terlalu tua.",
            "Aku tinggalkan cangkul, penyiram, kapak, beliung, pancing, dan sedikit bibit parsnip.",
            "Temuilah Pak Darto, kepala desa, di alun-alun desa sebelah selatan. Ia sahabat lamaku.",
            "Oh ya, jangan lupa periksa kotak surat di depan rumah. Warga desa suka berkirim surat.",
            "Rawat kebun ini baik-baik, ya.  - Kakek Wiryo",
        };
        ShowDialogue("Surat Kakek", letter, () =>
        {
            foreach (QuestData quest in Quests.StartAutomaticQuests())
                Notify($"Quest baru: {quest.Title} (J = jurnal)");
        });
    }

    private void GiveStartingItems()
    {
        Player.Inventory.TryAdd(Items.Create("hoe"));
        Player.Inventory.TryAdd(Items.Create("watering_can"));
        Player.Inventory.TryAdd(Items.Create("axe"));
        Player.Inventory.TryAdd(Items.Create("pickaxe"));
        Player.Inventory.TryAdd(Items.Create("fishing_rod"));
        Player.Inventory.TryAdd(Items.Create("parsnip_seeds"), 15);
    }

    /// <summary>Ganti mode game (State pattern).</summary>
    public void ChangeState(GameState next)
    {
        _state.Exit();
        _state = next;
        _state.Enter();
    }

    private void AddLocation(GameLocation location)
    {
        location.Services = this;   // MVP 5: objek dunia bisa memunculkan efek sendiri
        _locations.Add(location.Name, location);
    }

    /// <summary>Seed acak untuk dunia baru (hindari 0 = tanda "belum ada seed" di simpanan lama).</summary>
    private static int NewWorldSeed() => Random.Shared.Next(1, int.MaxValue);

    /// <summary>Buang semua cache potongan peta (dipanggil saat dunia dibuat ulang).</summary>
    private void DisposeChunkCaches()
    {
        foreach (ChunkCache cache in _chunkCaches.Values)
            cache.Dispose();
        _chunkCaches.Clear();
    }

    /// <summary>Cari lokasi dari namanya. Nama salah = bug di kode, jadi langsung error.</summary>
    public GameLocation GetLocation(string name)
    {
        if (!_locations.ContainsKey(name))
            throw new InvalidOperationException($"Lokasi '{name}' tidak ada.");
        return _locations[name];
    }

    /// <summary>Dipanggil PlayingState tiap frame: kalau pemain menginjak warp, mulai pindah lokasi.</summary>
    public bool TryWarp()
    {
        Warp? warp = CurrentLocation.GetWarpAt(Player.TilePosition);
        if (warp == null)
            return false;

        ChangeState(new WarpState(this, warp));
        return true;
    }

    /// <summary>Dari IGameServices: dipanggil meja toko.</summary>
    public void OpenShop(Shop shop) { PlaySound("ui_open"); ChangeState(new ShopState(this, shop)); }

    /// <summary>Dari IGameServices: dipanggil kotak kirim.</summary>
    public void OpenShippingBin(ShippingBin bin) { PlaySound("ui_open"); ChangeState(new ShippingBinState(this, bin)); }

    /// <summary>Dari IGameServices: dipanggil pintu rumah. Sama seperti menginjak warp.</summary>
    public void WarpTo(string locationName, Point tile)
    {
        ChangeState(new WarpState(this, new Warp(Player.TilePosition, locationName, tile)));
    }

    /// <summary>Dari IGameServices: dipanggil pancing.</summary>
    public void StartFishing(Point waterTile) => ChangeState(new FishingState(this, waterTile));

    // ---------- Minigame bercocok tanam ----------

    /// <summary>Buka minigame panen tanaman ini (kalau ada dan belum pernah lulus).</summary>
    public bool RunHarvestLesson(CropData crop, Action apply)
    {
        CropLesson? lesson = LessonData.ForCrop(crop.Id);
        if (lesson == null || !Lessons.ShouldPlay(lesson))
            return false;

        PlaySound("ui_open");
        ChangeState(new CropLessonState(this, lesson, apply));
        return true;
    }

    /// <summary>Dari IGameServices: menangkap hama. Event ini selalu memakai minigame.</summary>
    public bool RunPestLesson(Action apply)
    {
        CropLesson? lesson = LessonData.Pest;
        if (lesson == null)
            return false;

        PlaySound("ui_open");
        ChangeState(new CropLessonState(this, lesson, apply));
        return true;
    }

    /// <summary>Latihan dari Buku Tani: main lagi tanpa mengubah dunia.</summary>
    public void StartPracticeLesson(CropLesson lesson) => ChangeState(new CropLessonState(this, lesson, null));

    // ---------- MVP 4: dialog, pesan, efek ----------

    /// <summary>Dari IGameServices. Tanpa halaman = langsung onFinished.</summary>
    public void ShowDialogue(string speaker, IReadOnlyList<string> pages, Action? onFinished = null, string? portrait = null)
    {
        if (pages.Count == 0)
        {
            onFinished?.Invoke();
            return;
        }
        ChangeState(new DialogueState(this, speaker, pages, onFinished, portrait));
    }

    public void Notify(string message, bool success = true) => Toasts.Push(message, success);

    public void SpawnEffect(EffectKind kind, Vector2 worldPosition) => Particles.Emit(kind, worldPosition);

    // ---------- MVP 5: umpan balik ----------

    public void ShowPickup(Item item, int amount, Vector2 worldPosition) =>
        Popups.Add($"+{amount} {item.Name}", worldPosition, Color.White, item.SpriteKey);

    public void ShowFloatingText(string text, Vector2 worldPosition, Color color) => Popups.Add(text, worldPosition, color);

    public void ShakeCamera(float strength, float seconds) => Camera.Shake(strength, seconds);

    public void PlaySound(string name) => Sounds.PlayVaried(name);

    public void PassTime(int minutes) => Time.AddMinutes(minutes);

    /// <summary>
    /// Dipanggil PlayingState tiap frame: tanda quest warga, partikel, kunang-kunang,
    /// MVP 5: gelembung petunjuk, kursor tile, tembus pandang atap/pohon, teks melayang, riak ikan.
    /// </summary>
    public void UpdateWorldExtras(float deltaSeconds)
    {
        foreach (GameObject obj in CurrentLocation.Objects)
        {
            if (obj is Npc npc)
                npc.Marker = Quests.MarkerFor(npc.Data.Id, Player.Inventory);
        }

        bool fireflyTime = CurrentLocation.IsOutdoors && Time.TimeOfDay >= 19 * 60 + 30;
        Particles.Update(deltaSeconds, fireflyTime ? Camera.ViewArea : null);
        Popups.Update(deltaSeconds);
        _renderer.UpdateFades(CurrentLocation, Player, deltaSeconds);
        UpdatePrompt(deltaSeconds);
        SpawnFishRipples(deltaSeconds);
    }

    /// <summary>Cari benda di depan pemain yang bisa diperiksa, dan tile sasaran alat.</summary>
    private void UpdatePrompt(float deltaSeconds)
    {
        Point facing = Player.FacingTile;
        IInteractable? target = CurrentLocation.GetInteractableAt(facing);
        if (target != null && !target.CanInteract(Player))
            target = null;
        GameObject? targetObject = target as GameObject;
        Prompt.Update(deltaSeconds, targetObject == null ? null : target, targetObject, Player);

        Item? held = Player.Inventory.SelectedItem;
        if ((held is Tool && held is not FishingRod) || held is Seed)
        {
            WorldObject? there = CurrentLocation.GetObjectAt(facing);
            bool useful = there is HoeDirt || there is IBreakable || there is IWaterSource
                          || CurrentLocation.Map.GetTile(facing.X, facing.Y)?.Type is TileType.Dirt or TileType.Water;
            Prompt.SetCursor(facing, useful);
        }
        else
        {
            Prompt.SetCursor(null, false);
        }
    }

    /// <summary>Sesekali riak kecil muncul di air yang terlihat (tanda ada ikan).</summary>
    private void SpawnFishRipples(float deltaSeconds)
    {
        if (!CurrentLocation.IsOutdoors)
            return;
        _rippleTimer -= deltaSeconds;
        if (_rippleTimer > 0)
            return;
        _rippleTimer = 0.5f + (float)_rng.NextDouble() * 0.8f;

        RectangleF view = Camera.ViewArea;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            Vector2 at = new(view.X + (float)_rng.NextDouble() * view.Width, view.Y + (float)_rng.NextDouble() * view.Height);
            Point tile = new(TileMap.WorldToTile(at.X), TileMap.WorldToTile(at.Y));
            if (CurrentLocation.Map.GetTile(tile.X, tile.Y)?.Type == TileType.Water)
            {
                Particles.Emit(EffectKind.Ripple, at);
                return;
            }
        }
    }

    /// <summary>Semua cahaya di lokasi ini + cahaya kecil di sekitar pemain (supaya malam tidak gelap total).</summary>
    private IEnumerable<Light> CollectLights()
    {
        foreach (GameObject obj in CurrentLocation.Objects)
        {
            if (obj is ILightSource source)
                foreach (Light light in source.GetLights())
                    yield return light;
        }
        foreach (Light light in Particles.GetLights())
            yield return light;

        yield return new Light(Player.Center, 44, Color.FromArgb(255, 230, 190), 0.35f);
    }

    /// <summary>Memindahkan pemain ke tile tertentu di sebuah lokasi.</summary>
    public void ChangeLocation(GameLocation tujuan, Point tile)
    {
        CurrentLocation.Remove(Player);
        CurrentLocation = tujuan;
        Particles.Clear();
        Popups.Clear();
        Prompt.Clear();

        Player.PlaceAt(tujuan.GetPositionOnTile(Player, tile));
        tujuan.Add(Player);

        Camera.SnapTo(Player);
        Camera.ClampTo(tujuan.Map.PixelWidth, tujuan.Map.PixelHeight);

        // Atap & pohon langsung dalam keadaan yang benar (tidak memudar setelah datang)
        _renderer.UpdateFades(tujuan, Player, 10f);
    }

    /// <summary>MVP 5: dipanggil WarpState setelah tiba: spanduk nama tempat.</summary>
    public void AnnounceLocation()
    {
        if (LocationBanners.TryGetValue(CurrentLocation.Name, out var banner))
            Hud.ShowBanner(banner.Title, banner.Subtitle);
    }

    /// <summary>MVP 5: dipanggil PlayingState.Enter: spanduk hari baru yang tertunda (setelah ringkasan malam).</summary>
    public void OnPlayingResumed()
    {
        if (!_dayBannerPending)
            return;
        _dayBannerPending = false;
        Hud.ShowBanner(Date.ToString(), $"Tahun {Date.Year}");
    }

    // ---------- Tidur & ganti hari ----------

    /// <summary>Dari IGameServices: dipanggil kasur.</summary>
    public void RequestSleep() => RequestSleep(SleepReason.Normal);

    public void RequestSleep(SleepReason reason)
    {
        if (_state is DayEndState)
            return;   // sudah sedang tidur, abaikan
        ChangeState(new DayEndState(this, reason));
    }

    /// <summary>
    /// Proses malam hari. URUTAN PENTING:
    /// jual dulu -> ganti tanggal -> semua lokasi ganti hari -> energi pulih -> pemain bangun di samping kasur
    /// -> surat baru datang -> simpan otomatis (MVP 5).
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

        // 1. Jual isi SEMUA kotak kirim (kotak di lokasi lain ikut otomatis)
        List<ShipmentLine> sold = ProcessAllShipments();

        // 2. Ganti tanggal
        Time.StartNextDay();

        // 3. Semua lokasi ganti hari: tanaman tumbuh (memakai musim BARU), tunggul pohon tumbuh lagi, barang liar
        foreach (GameLocation location in _locations.Values)
            location.OnNewDay(Time.Date);

        // 4. Energi pulih, warga boleh disapa & diberi hadiah lagi
        Player.Stamina.RestoreTo(energyRatio);
        Player.Relationships.OnNewDay();

        // 5. Bangun di samping kasur
        ChangeLocation(FarmHouse, FarmHouse.WakeUpTile);

        // 6. MVP 5: surat pagi, spanduk hari baru, simpan otomatis
        Mail.Deliver(Time.Date);
        _dayBannerPending = true;
        PlaySound("coin");
        SaveGame();

        return new DaySummary(endedDate, Time.Date, reason, sold, Player.Wallet.Gold, energyRatio);
    }

    /// <summary>
    /// Jual isi semua IShippingContainer di semua lokasi, lalu gabungkan baris laporan
    /// yang jenis barangnya sama (dua kotak berisi parsnip = satu baris di ringkasan malam).
    /// </summary>
    private List<ShipmentLine> ProcessAllShipments()
    {
        List<ShipmentLine> lines = new();
        foreach (GameLocation location in _locations.Values)
        {
            foreach (IShippingContainer bin in location.Objects.OfType<IShippingContainer>())
                lines.AddRange(bin.ProcessShipment(Player.Wallet));
        }

        return lines
            .GroupBy(line => line.ItemName)
            .Select(group => new ShipmentLine(group.Key, group.Sum(line => line.Quantity), group.Sum(line => line.Total)))
            .ToList();
    }

    // ---------- Loop ----------

    public void Update(float deltaSeconds)
    {
        CountFps(deltaSeconds);

        if (Input.WasPressed(Keys.F3))
            Debug.Toggle();

        _state.Update(deltaSeconds);   // polimorfisme: bermain, ganti hari, menu, ...
        Hud.Update(deltaSeconds, Player);
        Camera.UpdateShake(deltaSeconds);

        Input.EndFrame();
    }

    public void Draw(Graphics g)
    {
        // Game digambar di ruang 960x576 (Scale=3); di sini diskalakan ke back buffer
        // sebenarnya (ViewWidth*RenderScale) supaya piksel selalu kelipatan bulat = tajam.
        GraphicsState state = g.Save();
        g.ScaleTransform(GameConfig.UiFactor, GameConfig.UiFactor);
        _state.Draw(g);
        g.Restore(state);
    }

    /// <summary>Kamera mengikuti pemain, lalu dibatasi ke tepi map.</summary>
    public void UpdateCamera(float deltaSeconds)
    {
        Camera.Follow(Player, deltaSeconds);
        Camera.ClampTo(CurrentLocation.Map.PixelWidth, CurrentLocation.Map.PixelHeight);
    }

    /// <summary>MVP 5: gambar dunia saja (latar layar judul): tanpa HUD.</summary>
    public void DrawWorldOnly(Graphics g)
    {
        _renderer.Render(g, CurrentLocation, Particles);
        _lighting.Draw(g, Camera, Time.TimeOfDay, CurrentLocation.IsOutdoors, CollectLights());
        _vignette.Draw(g);
    }

    /// <summary>Gambar dunia, cahaya, info debug (kalau F3), lalu HUD. Dipakai oleh semua state.</summary>
    public void DrawWorldAndHud(Graphics g, bool showToolbar = true)
    {
        _renderer.Render(g, CurrentLocation, Particles);
        _lighting.Draw(g, Camera, Time.TimeOfDay, CurrentLocation.IsOutdoors, CollectLights());   // MVP 4
        _renderer.RenderOverlays(g, CurrentLocation, showToolbar ? Prompt : null, Popups);      // di atas gelap malam
        _vignette.Draw(g);

        if (Debug.IsVisible)
            Debug.Draw(g, this);

        Hud.Draw(g, Player, Time, showToolbar, showToolbar ? HoveredToolbarSlot() : -1);
        if (showToolbar)
            Hud.DrawQuestTracker(g, Quests.Active.Select(q => (q.Title, Quests.ProgressText(q, Player.Inventory, Npcs, Items))));
    }

    /// <summary>Slot toolbar yang sedang ditunjuk mouse (-1 kalau tidak ada).</summary>
    private int HoveredToolbarSlot()
    {
        Point mouse = Input.MousePosition;
        for (int i = 0; i < Player.Inventory.Capacity; i++)
        {
            if (Hud.ToolbarSlotRect(i, Player.Inventory.Capacity).Contains(mouse))
                return i;
        }
        return -1;
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
