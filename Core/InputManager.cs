namespace FarmGame.Core;

/// <summary>
/// Mencatat tombol keyboard yang sedang ditahan dan yang baru ditekan di frame ini.
/// GameForm mengisi datanya, Game yang membacanya.
/// </summary>
public class InputManager
{
    private readonly HashSet<Keys> _heldKeys = new();      // tombol yang sedang ditahan
    private readonly HashSet<Keys> _pressedKeys = new();   // tombol yang BARU ditekan frame ini

    public void KeyDown(Keys key)
    {
        // Add() bernilai true hanya kalau tombol belum ditahan sebelumnya
        if (_heldKeys.Add(key))
            _pressedKeys.Add(key);
    }

    public void KeyUp(Keys key) => _heldKeys.Remove(key);

    /// <summary>Dipanggil saat jendela kehilangan fokus (Alt+Tab) supaya tombol tidak "nyangkut".</summary>
    public void ReleaseAll() => _heldKeys.Clear();

    public bool IsHeld(Keys key) => _heldKeys.Contains(key);

    public bool WasPressed(Keys key) => _pressedKeys.Contains(key);

    /// <summary>Dipanggil di akhir setiap frame.</summary>
    public void EndFrame() => _pressedKeys.Clear();

    public bool IsRunning => IsHeld(Keys.ShiftKey);

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
