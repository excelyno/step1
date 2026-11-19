namespace FarmGame.Systems;

/// <summary>
/// Jam dan kalender game. 10 menit di game = 7 detik nyata.
/// Hari dimulai 06:00. Jam 02:00 (= 26:00) pemain pingsan.
/// </summary>
public class TimeManager : IUpdatable
{
    public const int DayStartMinutes = 6 * 60;    // 06:00
    public const int MidnightMinutes = 24 * 60;   // 24:00
    public const int PassOutMinutes = 26 * 60;    // 02:00 dini hari

    private float _timer;   // penghitung detik nyata

    public GameDate Date { get; private set; } = new(1, Season.Spring, 1);

    /// <summary>Menit sejak 00:00 hari ini. Bisa lebih dari 1440 kalau sudah lewat tengah malam.</summary>
    public int TimeOfDay { get; private set; } = DayStartMinutes;

    public bool IsPaused { get; private set; }

    public bool HasPassedOut => TimeOfDay >= PassOutMinutes;

    /// <summary>Teks jam, contoh "06:00", "01:30".</summary>
    public string ClockText => $"{TimeOfDay / 60 % 24:00}:{TimeOfDay % 60:00}";

    public void Pause() => IsPaused = true;
    public void Resume() => IsPaused = false;

    public void Update(float deltaSeconds)
    {
        if (IsPaused)
            return;

        _timer += deltaSeconds;
        while (_timer >= GameConfig.SecondsPerTenMinutes && !IsPaused)
        {
            _timer -= GameConfig.SecondsPerTenMinutes;
            AddMinutes(10);
        }
    }

    /// <summary>Majukan jam. Public supaya bisa dipakai cheat debug (F6 = +1 jam).</summary>
    public void AddMinutes(int minutes)
    {
        TimeOfDay = Math.Min(TimeOfDay + minutes, PassOutMinutes);
        if (HasPassedOut)
            Pause();   // jam berhenti, PlayingState akan memicu pingsan
    }

    /// <summary>Pindah ke pagi hari berikutnya (dipanggil saat tidur).</summary>
    public void StartNextDay()
    {
        Date = Date.NextDay();
        TimeOfDay = DayStartMinutes;
        _timer = 0;
    }
}
