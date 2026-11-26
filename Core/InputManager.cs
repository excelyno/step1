namespace FarmGame.Core;

/// <summary>
/// Mencatat tombol keyboard & mouse yang sedang ditahan dan yang baru ditekan di frame ini.
/// GameForm mengisi datanya, Game / state yang membacanya.
/// </summary>
public class InputManager
{
    private readonly HashSet<Keys> _heldKeys = new();      // tombol yang sedang ditahan
    private readonly HashSet<Keys> _pressedKeys = new();   // tombol yang BARU ditekan frame ini
    private readonly HashSet<MouseButtons> _heldMouse = new();
    private readonly HashSet<MouseButtons> _pressedMouse = new();   // tombol mouse BARU ditekan
    private readonly HashSet<MouseButtons> _releasedMouse = new();  // tombol mouse BARU dilepas
    private int _wheelDelta;                               // putaran roda mouse frame ini

    // Tombol untuk slot toolbar 1..12: 1 2 3 4 5 6 7 8 9 0 - =
    private static readonly Keys[] SlotKeys =
    {
        Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6,
        Keys.D7, Keys.D8, Keys.D9, Keys.D0, Keys.OemMinus, Keys.Oemplus
    };

    // ---------- Diisi oleh GameForm ----------

    public void KeyDown(Keys key)
    {
        // Add() bernilai true hanya kalau tombol belum ditahan sebelumnya
        if (_heldKeys.Add(key))
            _pressedKeys.Add(key);
    }

    public void KeyUp(Keys key) => _heldKeys.Remove(key);

    /// <summary>Catat tombol mouse yang baru ditekan (hanya sekali sampai dilepas).</summary>
    public void MouseDown(MouseButtons button)
    {
        if (_heldMouse.Add(button))
            _pressedMouse.Add(button);
    }

    public void MouseUp(MouseButtons button)
    {
        _heldMouse.Remove(button);
        _releasedMouse.Add(button);
    }

    public void MouseWheel(int delta) => _wheelDelta += delta;

    /// <summary>Posisi mouse di koordinat game (960x576), bukan koordinat jendela. Diisi GameForm.</summary>
    public Point MousePosition { get; private set; } = new(-1, -1);

    public void SetMousePosition(Point position) => MousePosition = position;

    /// <summary>Dipanggil saat jendela kehilangan fokus (Alt+Tab) supaya tombol tidak "nyangkut".</summary>
    public void ReleaseAll()
    {
        _heldKeys.Clear();
        _heldMouse.Clear();
    }

    /// <summary>Dipanggil di akhir setiap frame.</summary>
    public void EndFrame()
    {
        _pressedKeys.Clear();
        _pressedMouse.Clear();
        _releasedMouse.Clear();
        _wheelDelta = 0;
    }

    // ---------- Dibaca oleh game ----------

    public bool IsHeld(Keys key) => _heldKeys.Contains(key);

    public bool WasPressed(Keys key) => _pressedKeys.Contains(key);

    public bool IsMouseHeld(MouseButtons button) => _heldMouse.Contains(button);

    public bool MouseDownPressed(MouseButtons button) => _pressedMouse.Contains(button);

    public bool MouseUpPressed(MouseButtons button) => _releasedMouse.Contains(button);

    public bool IsRunning => IsHeld(Keys.ShiftKey);

    /// <summary>Pakai alat: C atau klik kiri.</summary>
    public bool UseToolPressed => WasPressed(Keys.C) || _pressedMouse.Contains(MouseButtons.Left);

    /// <summary>Periksa / panen / makan: X atau klik kanan.</summary>
    public bool InteractPressed => WasPressed(Keys.X) || _pressedMouse.Contains(MouseButtons.Right);

    /// <summary>Lanjut (layar ringkasan): Enter, Spasi, atau tombol aksi.</summary>
    public bool ConfirmPressed => WasPressed(Keys.Enter) || WasPressed(Keys.Space) || UseToolPressed || InteractPressed;

    // ---------- MVP 2: menu (toko) ----------

    /// <summary>Naik di menu: W / panah atas / roda mouse ke atas.</summary>
    public bool MenuUpPressed => WasPressed(Keys.W) || WasPressed(Keys.Up) || WheelDirection < 0;

    /// <summary>Turun di menu: S / panah bawah / roda mouse ke bawah.</summary>
    public bool MenuDownPressed => WasPressed(Keys.S) || WasPressed(Keys.Down) || WheelDirection > 0;

    /// <summary>Pilih / beli: C, Enter, Spasi, atau klik kiri.</summary>
    public bool MenuSelectPressed => UseToolPressed || WasPressed(Keys.Enter) || WasPressed(Keys.Space);

    /// <summary>Tutup menu: Esc, X, atau klik kanan.</summary>
    public bool MenuClosePressed => WasPressed(Keys.Escape) || InteractPressed;

    /// <summary>Slot yang dipilih lewat tombol angka, atau -1 kalau tidak ada.</summary>
    public int GetPressedSlot()
    {
        for (int i = 0; i < SlotKeys.Length; i++)
        {
            if (WasPressed(SlotKeys[i]))
                return i;
        }
        return -1;
    }

    /// <summary>Arah roda mouse: +1 (putar ke bawah = slot kanan), -1 (ke atas), 0 (diam).</summary>
    public int WheelDirection => Math.Sign(-_wheelDelta);

    /// <summary>Arah gerak 8 arah dari WASD / panah. Dinormalkan supaya diagonal tidak lebih cepat.</summary>
    public Vector2 GetMoveVector()
    {
        Vector2 dir = Vector2.Zero;
        if (IsHeld(Keys.W) || IsHeld(Keys.Up)) dir.Y -= 1;
        if (IsHeld(Keys.S) || IsHeld(Keys.Down)) dir.Y += 1;
        if (IsHeld(Keys.A) || IsHeld(Keys.Left)) dir.X -= 1;
        if (IsHeld(Keys.D) || IsHeld(Keys.Right)) dir.X += 1;

        if (dir != Vector2.Zero)
            dir = Vector2.Normalize(dir);
        return dir;
    }
}
