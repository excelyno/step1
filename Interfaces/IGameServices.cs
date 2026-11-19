namespace FarmGame.Interfaces;

/// <summary>
/// Layanan game yang boleh dipakai item dan objek dunia TANPA mereka kenal kelas Game.
/// Contoh: HoeDirt butuh membuat item hasil panen, Seed butuh tahu musim, pintu butuh minta tidur.
/// Game yang mengimplementasikan interface ini.
/// </summary>
public interface IGameServices
{
    ItemRegistry Items { get; }
    GameDate Date { get; }

    /// <summary>Minta hari diakhiri (tidur).</summary>
    void RequestSleep();
}
