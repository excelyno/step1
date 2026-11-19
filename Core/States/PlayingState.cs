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
    }

    public override void Update(float deltaSeconds)
    {
        InputManager input = Game.Input;
        Player player = Game.Player;

        // 1. Pilih slot toolbar (angka 1..= atau roda mouse)
        HandleToolbar(input, player.Inventory);

        // 2. Aksi: C = pakai item, X = periksa / makan
        if (input.UseToolPressed)
            ShowResult(player.UseSelectedItem(Game));
        else if (input.InteractPressed)
            ShowResult(player.Interact(Game));

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
        Game.UpdateCamera(deltaSeconds);
        Game.Toasts.Update(deltaSeconds);
    }

    public override void Draw(Graphics g)
    {
        Game.DrawWorldAndHud(g);
        Game.Toasts.Draw(g);
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

        // F6: maju 1 jam
        if (input.WasPressed(Keys.F6))
            Game.Time.AddMinutes(60);
    }
}
