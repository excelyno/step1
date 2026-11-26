namespace FarmGame.UI;

/// <summary>
/// Jendela game. Tugasnya HANYA: menerima event Windows (tombol, paint, fokus),
/// meneruskannya ke Game, dan menjalankan game loop. Tidak ada logika game di sini.
/// Best practice pixel-art: game digambar ke back buffer seukuran kelipatan bulat dari
/// view 320x192 (RenderScale 1..8), lalu ditempel 1:1 ke jendela (nearest neighbor).
/// Jadi piksel SELALU tajam, jauh dari "buriq" bilinear. Sisi kosong = pita hitam.
/// Jendela boleh diubah ukurannya / dimaksimalkan, F11 = layar penuh.
/// </summary>
public class GameForm : Form
{
    private readonly Game _game;
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly Stopwatch _stopwatch = new();
    private double _lastTime;
    private Bitmap _backBuffer;
    private FormWindowState _windowedState;
    private bool _fullscreen;

    public GameForm()
    {
        Text = "TANDUR - FarmGame MVP 5";
        ClientSize = new Size(GameConfig.ScreenWidth, GameConfig.ScreenHeight);
        MinimumSize = new Size(GameConfig.ScreenWidth / 2, GameConfig.ScreenHeight / 2);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.Black;
        KeyPreview = true;
        AutoScaleMode = AutoScaleMode.None;   // ukuran diatur manual, DPI-aware via PerMonitorV2

        _backBuffer = new Bitmap(GameConfig.SurfaceWidth, GameConfig.SurfaceHeight);

        // Double buffering: gambar ke memori dulu, baru ditampilkan -> layar tidak berkedip
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        DoubleBuffered = true;

        // Baris ini satu-satunya yang diganti saat art (sprite PNG) sudah siap
        _game = new Game(new SpriteAssets());

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
        if (_game.ExitRequested)
        {
            Close();
            return;
        }
        Invalidate();   // minta Windows memanggil OnPaint
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // 1. Game menggambar ke back buffer (semua kode game tetap memakai ruang 960x576;
        //    Game.Draw melewatkan faktor RenderScale/3 ke back buffer sebenarnya).
        using (Graphics g = Graphics.FromImage(_backBuffer))
            _game.Draw(g);

        // 2. Tempel 1:1 (ukuran buffer sudah dihitung kelipatan bulat) -> piksel tajam, tanpa blur.
        Rectangle target = SurfaceRectangle();
        Graphics screen = e.Graphics;
        screen.Clear(Color.Black);
        screen.InterpolationMode = InterpolationMode.NearestNeighbor;
        screen.PixelOffsetMode = PixelOffsetMode.Half;
        screen.DrawImage(_backBuffer, target);
    }

    /// <summary>Pilih RenderScale terbesar (kelipatan bulat) yang muat di jendela, lalu buat ulang back buffer.</summary>
    private void UpdateBackBuffer()
    {
        if (_backBuffer == null)   // dipanggil lebih awal dari constructor (saat ClientSize diatur)
            return;
        float scale = Math.Min(ClientSize.Width / (float)GameConfig.ViewWidth, ClientSize.Height / (float)GameConfig.ViewHeight);
        GameConfig.RenderScale = Math.Max(1, (int)MathF.Floor(scale));
        if (_backBuffer.Width != GameConfig.SurfaceWidth || _backBuffer.Height != GameConfig.SurfaceHeight)
        {
            _backBuffer.Dispose();
            _backBuffer = new Bitmap(GameConfig.SurfaceWidth, GameConfig.SurfaceHeight);
        }
    }

    /// <summary>Kotak back buffer (ViewWidth*RenderScale x ViewHeight*RenderScale) di tengah jendela.</summary>
    private Rectangle SurfaceRectangle()
    {
        int w = GameConfig.SurfaceWidth, h = GameConfig.SurfaceHeight;
        return new Rectangle((ClientSize.Width - w) / 2, (ClientSize.Height - h) / 2, w, h);
    }

    /// <summary>F11: layar penuh tanpa bingkai / kembali ke jendela.</summary>
    private void ToggleFullscreen()
    {
        _fullscreen = !_fullscreen;
        if (_fullscreen)
        {
            _windowedState = WindowState;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal;   // reset dulu supaya Maximized benar-benar menutup taskbar
            WindowState = FormWindowState.Maximized;
        }
        else
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = _windowedState;
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        // OnResize juga dipanggil di dalam constructor (saat ClientSize diatur), sebelum _game ada.
        if (_game == null)
            return;

        // Ukuran jendela berubah: pilih RenderScale baru, lalu posisi mouse ikut dihitung ulang
        UpdateBackBuffer();
        _game.Input.SetMousePosition(ToGamePoint(PointToClient(Cursor.Position)));
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Sengaja kosong: Renderer sudah membersihkan layar -> mencegah kedip
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
            return;
        }
        _game.Input.KeyDown(e.KeyCode);
        e.Handled = true;
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        _game.Input.KeyUp(e.KeyCode);
        base.OnKeyUp(e);
    }

    // Mouse: posisi dipakai menu (hover & drag), klik kiri = pakai alat, klik kanan = periksa, roda = ganti slot
    protected override void OnMouseMove(MouseEventArgs e)
    {
        _game.Input.SetMousePosition(ToGamePoint(e.Location));
        base.OnMouseMove(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _game.Input.SetMousePosition(ToGamePoint(e.Location));
        _game.Input.MouseDown(e.Button);
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _game.Input.SetMousePosition(ToGamePoint(e.Location));
        _game.Input.MouseUp(e.Button);
        base.OnMouseUp(e);
    }

    /// <summary>Ubah posisi mouse jendela (bisa diperbesar/dikecilkan) ke koordinat game 960x576.
    /// Back buffer = ViewWidth*RenderScale piksel; ruang game = 960x576 (3x view), jadi faktornya 3/RenderScale.</summary>
    private Point ToGamePoint(Point client)
    {
        Rectangle target = SurfaceRectangle();
        if (target.Width <= 0 || target.Height <= 0)
            return new Point(-1, -1);

        float scale = GameConfig.Scale / (float)GameConfig.RenderScale;
        return new Point(
            (int)MathF.Floor((client.X - target.X) * scale),
            (int)MathF.Floor((client.Y - target.Y) * scale));
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
