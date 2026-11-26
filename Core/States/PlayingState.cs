namespace FarmGame.Core.States;

/// <summary>Mode bermain biasa: jalan, pakai alat, periksa, jam berjalan.</summary>
public class PlayingState : GameState
{
    public PlayingState(Game game) : base(game)
    {
    }

    public override void Enter()
    {
        Game.Time.Resume();
        Game.OnPlayingResumed();   // MVP 5: spanduk hari baru
    }

    public override void Update(float deltaSeconds)
    {
        InputManager input = Game.Input;
        Player player = Game.Player;

        // 0. Klik kiri di toolbar = pilih slot (klik ini tidak memakai alat)
        if (input.MouseDownPressed(MouseButtons.Left) && TryClickToolbar(player, input.MousePosition))
            return;

        // 1. Pilih slot toolbar (angka 1..= atau roda mouse)
        HandleToolbar(input, player.Inventory);

        // 2. Aksi: C = pakai item, X = periksa / makan
        if (input.UseToolPressed)
            ShowResult(player.UseSelectedItem(Game));
        else if (input.InteractPressed)
            ShowResult(player.Interact(Game));

        // Aksi tadi bisa mengganti mode (pintu = tidur, meja = toko). Kalau iya, berhenti di sini.
        if (Game.CurrentState != this)
            return;

        // MVP 4: J = buka jurnal quest
        if (input.WasPressed(Keys.J))
        {
            Game.ChangeState(new JournalState(Game));
            Game.PlaySound("ui_open");
            return;
        }

        // MVP 5: Esc = menu jeda (lanjut / simpan / keluar ke judul)
        if (input.WasPressed(Keys.Escape))
        {
            Game.ChangeState(new PauseState(Game));
            return;
        }

        // 3. Cheat (hanya saat debug F3 tampil)
        if (Game.Debug.IsVisible)
            HandleDebugCheats(input);

        // 4. Gerak
        player.SetMovementIntent(input.GetMoveVector(), input.IsRunning);

        // 5. Jam berjalan. Kalau sudah 02:00 -> pingsan
        Game.Time.Update(deltaSeconds);
        if (Game.Time.HasPassedOut)
        {
            Game.RequestSleep(SleepReason.PassedOut);
            return;
        }

        // 6. Dunia, kamera, pesan
        Game.CurrentLocation.Update(deltaSeconds);
        Game.UpdateWorldExtras(deltaSeconds);   // MVP 4: tanda quest, partikel
        Game.UpdateCamera(deltaSeconds);
        Game.Toasts.Update(deltaSeconds);

        // 7. MVP 2: menginjak ujung jalan? -> pindah lokasi
        Game.TryWarp();
    }

    public override void Draw(Graphics g)
    {
        Game.DrawWorldAndHud(g);
        Game.Toasts.Draw(g);
    }

    /// <summary>
    /// Klik di bingkai toolbar selalu "ditelan" supaya tidak ikut memakai alat.
    /// Klik tepat di slot = pilih slot itu.
    /// </summary>
    private static bool TryClickToolbar(Player player, Point mouse)
    {
        int capacity = player.Inventory.Capacity;
        if (!Hud.ToolbarAreaRect(capacity).Contains(mouse))
            return false;

        for (int i = 0; i < capacity; i++)
        {
            if (Hud.ToolbarSlotRect(i, capacity).Contains(mouse))
            {
                player.Inventory.Select(i);
                break;
            }
        }
        return true;
    }

    private void HandleToolbar(InputManager input, Inventory inventory)
    {
        int slot = input.GetPressedSlot();
        if (slot >= 0)
            inventory.Select(slot);

        if (input.WheelDirection != 0)
            inventory.SelectNext(input.WheelDirection);
    }

    /// <summary>Tampilkan pesan hasil aksi (hijau = berhasil, oranye = gagal).</summary>
    private void ShowResult(ActionResult result)
    {
        if (result.Deferred)
            return;   // aksi baru terjadi setelah minigame selesai

        if (result.Message != null)
            Game.Toasts.Push(result.Message, result.Success);
    }

    private void HandleDebugCheats(InputManager input)
    {
        // F4: +5 semua jenis bibit
        if (input.WasPressed(Keys.F4))
        {
            foreach (CropData crop in Game.Crops.All)
                Game.Player.Inventory.TryAdd(Game.Items.Create(crop.SeedId), 5);
            Game.Toasts.Push("Cheat: +5 semua bibit", true);
        }

        // F5: +500 uang (untuk mencoba toko)
        if (input.WasPressed(Keys.F5))
        {
            Game.Player.Wallet.Earn(500);
            Game.Toasts.Push("Cheat: +500g", true);
        }

        // F7: paksa semua tanaman siap panen (untuk mencoba minigame panen)
        if (input.WasPressed(Keys.F7))
        {
            int count = 0;
            foreach (GameObject obj in Game.CurrentLocation.Objects)
            {
                if (obj is HoeDirt { Crop: { } crop } dirt && !crop.IsReady && !crop.IsDead)
                {
                    while (!crop.IsReady)
                        crop.AdvanceDay(true, Game.Date.Season);
                    count++;
                }
            }
            Game.Toasts.Push($"Cheat: {count} tanaman siap panen", true);
        }

        // F6: maju 1 jam
        if (input.WasPressed(Keys.F6))
            Game.Time.AddMinutes(60);
    }
}
