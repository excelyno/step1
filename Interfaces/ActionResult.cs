namespace FarmGame.Interfaces;

/// <summary>
/// Hasil sebuah aksi (pakai alat, tanam, panen, makan, kirim): berhasil atau tidak + pesan.
/// Item TIDAK menampilkan pesan sendiri. Ia cukup mengembalikan ActionResult,
/// lalu PlayingState yang menampilkannya lewat ToastQueue.
/// </summary>
public class ActionResult
{
    public bool Success { get; }
    public string? Message { get; }   // null = tidak perlu menampilkan pesan

    public ActionResult(bool success, string? message)
    {
        Success = success;
        Message = message;
    }

    /// <summary>Tidak terjadi apa-apa, tanpa pesan.</summary>
    public static ActionResult None() => new(false, null);

    public static ActionResult Ok(string? message = null) => new(true, message);

    public static ActionResult Fail(string message) => new(false, message);
}
