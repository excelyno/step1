namespace FarmGame.Systems;

/// <summary>
/// Tanggal di dalam game: tahun, musim, hari (1..28).
/// Tidak bisa diubah. NextDay() membuat objek tanggal BARU.
/// </summary>
public class GameDate
{
    public const int DaysPerSeason = 28;
    private static readonly string[] DayNames = { "Sen", "Sel", "Rab", "Kam", "Jum", "Sab", "Min" };

    public int Year { get; }
    public Season Season { get; }
    public int Day { get; }

    public GameDate(int year, Season season, int day)
    {
        Year = year;
        Season = season;
        Day = day;
    }

    public string DayName => DayNames[(Day - 1) % 7];

    public string SeasonName
    {
        get
        {
            if (Season == Season.Spring) return "Semi";
            if (Season == Season.Summer) return "Panas";
            if (Season == Season.Fall) return "Gugur";
            return "Dingin";
        }
    }

    /// <summary>Tanggal besok. 28 Semi -> 1 Panas, 28 Dingin -> tahun berikutnya.</summary>
    public GameDate NextDay()
    {
        if (Day < DaysPerSeason)
            return new GameDate(Year, Season, Day + 1);

        if (Season == Season.Winter)
            return new GameDate(Year + 1, Season.Spring, 1);

        return new GameDate(Year, Season + 1, 1);
    }

    /// <summary>Contoh: "Jum, 5 Semi".</summary>
    public override string ToString() => $"{DayName}, {Day} {SeasonName}";
}
