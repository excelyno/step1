namespace FarmGame.Core;

/// <summary>
/// Inti game: membuat dan memiliki semua objek utama (input, kamera, renderer, player, lokasi),
/// lalu menjalankan urutan Update dan Draw setiap frame.
/// </summary>
public class Game
{
    private readonly Renderer _renderer;
    private readonly DebugOverlay _debug = new();

    // Penghitung FPS
    private int _frameCount;
    private float _fpsTimer;

    public InputManager Input { get; } = new();
    public Camera Camera { get; } = new(GameConfig.ViewWidth, GameConfig.ViewHeight);
    public Player Player { get; }
    public GameLocation CurrentLocation { get; private set; }
    public int Fps { get; private set; }

    public Game(IAssetProvider assets)
    {
        _renderer = new Renderer(assets, Camera);
        Player = new Player("Petani");

        CurrentLocation = new Farm();
        ChangeLocation(CurrentLocation);
    }

    /// <summary>Memindahkan pemain ke sebuah lokasi (MVP 2 nanti dipakai untuk warp).</summary>
    public void ChangeLocation(GameLocation tujuan)
    {
        CurrentLocation.Remove(Player);
        CurrentLocation = tujuan;

        Player.PlaceAt(tujuan.GetSpawnPosition(Player));
        tujuan.Add(Player);

        Camera.SnapTo(Player);
        Camera.ClampTo(tujuan.Map.PixelWidth, tujuan.Map.PixelHeight);
    }

    public void Update(float deltaSeconds)
    {
        CountFps(deltaSeconds);

        // 1. Input
        if (Input.WasPressed(Keys.F3))
            _debug.Toggle();

        // 2. Teruskan "niat gerak" ke player (player tidak membaca keyboard sendiri)
        Player.SetMovementIntent(Input.GetMoveVector(), Input.IsRunning);

        // 3. Update dunia beserta semua objek di dalamnya
        CurrentLocation.Update(deltaSeconds);

        // 4. Kamera mengikuti player, lalu dibatasi ke tepi map
        Camera.Follow(Player, deltaSeconds);
        Camera.ClampTo(CurrentLocation.Map.PixelWidth, CurrentLocation.Map.PixelHeight);

        // 5. Selesai frame: hapus status "baru ditekan"
        Input.EndFrame();
    }

    public void Draw(Graphics g)
    {
        _renderer.Render(g, CurrentLocation);

        if (_debug.IsVisible)
            _debug.Draw(g, this);
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
