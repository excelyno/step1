namespace FarmGame.UI;

/// <summary>
/// Jendela game. Tugasnya HANYA: menerima event Windows (tombol, paint, fokus),
/// meneruskannya ke Game, dan menjalankan game loop. Tidak ada logika game di sini.
/// </summary>
public class GameForm : Form
{
    private readonly Game _game;
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly Stopwatch _stopwatch = new();
    private double _lastTime;

    public GameForm()
    {
        Text = "FarmGame - MVP 1";
        ClientSize = new Size(GameConfig.ScreenWidth, GameConfig.ScreenHeight);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.Black;
        KeyPreview = true;

        // Double buffering: gambar ke memori dulu, baru ditampilkan -> layar tidak berkedip
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        DoubleBuffered = true;

        // Baris ini satu-satunya yang diganti saat art (sprite PNG) sudah siap
        _game = new Game(new PlaceholderAssets());

        // Game loop: Timer memicu GameLoop() terus-menerus
        _timer.Interval = GameConfig.TimerInterval;
        _timer.Tick += (sender, e) => GameLoop();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _stopwatch.Start();
        _timer.Start();   // loop baru mulai setelah jendela tampil
    }

    /// <summary>Satu frame: hitung delta time, update game, lalu minta digambar ulang.</summary>
    private void GameLoop()
    {
        double now = _stopwatch.Elapsed.TotalSeconds;
        float deltaSeconds = (float)(now - _lastTime);
        _lastTime = now;

        // Dibatasi supaya pemain tidak menembus dinding setelah jendela di-drag
        deltaSeconds = Math.Min(deltaSeconds, GameConfig.MaxDeltaSeconds);

        _game.Update(deltaSeconds);
        Invalidate();   // minta Windows memanggil OnPaint
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        _game.Draw(e.Graphics);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Sengaja kosong: Renderer sudah membersihkan layar -> mencegah kedip
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        _game.Input.KeyDown(e.KeyCode);
        e.Handled = true;
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        _game.Input.KeyUp(e.KeyCode);
        base.OnKeyUp(e);
    }

    // MVP 1: mouse (klik kiri = pakai alat, klik kanan = periksa, roda = ganti slot)
    protected override void OnMouseDown(MouseEventArgs e)
    {
        _game.Input.MouseDown(e.Button);
        base.OnMouseDown(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        _game.Input.MouseWheel(e.Delta);
        base.OnMouseWheel(e);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        _game.Input.ReleaseAll();   // cegah tombol "nyangkut" saat Alt+Tab
        base.OnDeactivate(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _timer.Stop();
        base.OnFormClosing(e);
    }
}
