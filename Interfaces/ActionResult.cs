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

    /// <summary>
    /// Aksi belum terjadi sekarang: pemain sedang membuka minigame, dan perubahan dunia
    /// baru dijalankan setelah minigame selesai. Kalau dibatalkan, tidak ada yang berubah.
    /// </summary>
    public bool Deferred { get; }

    public ActionResult(bool success, string? message, bool deferred = false)
    {
        Success = success;
        Message = message;
        Deferred = deferred;
    }

    /// <summary>Tidak terjadi apa-apa, tanpa pesan.</summary>
    public static ActionResult None() => new(false, null);

    public static ActionResult Ok(string? message = null) => new(true, message);

    public static ActionResult Fail(string message) => new(false, message);

    /// <summary>Menunggu minigame. Jangan tampilkan pesan, jangan lakukan efek apa pun dulu.</summary>
    public static ActionResult Defer() => new(true, null, deferred: true);
}
