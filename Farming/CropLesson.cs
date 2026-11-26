namespace FarmGame.Farming;

/// <summary>Jenis permainan yang dipakai sebuah pelajaran. Gambar & aturannya sudah ada di kode.</summary>
public enum LessonWidgetKind
{
    Timing,   // jarum bergerak + zona hijau, tekan di saat tepat
    Grid,     // grid: klik yang benar (atau berurutan)
    Hold,     // tahan tombol sampai penanda di zona, lalu lepas
    Drag      // tahan & putari lingkaran
}

/// <summary>
/// Data satu minigame bercocok tanam, dibaca dari Content/Data/lessons.json.
/// Menambah minigame = menambah baris JSON, bukan kelas baru.
/// Satu pelajaran = satu tanaman (cara rawat/panennya) atau satu event (hama).
/// </summary>
public class CropLesson
{
    public string Id { get; set; } = "";
    public string CropId { get; set; } = "";         // "" = event (hama), berlaku untuk semua tanaman
    public string Widget { get; set; } = "Grid";
    public string Title { get; set; } = "";
    public string Goal { get; set; } = "";
    public string Fact { get; set; } = "";
    public string Tip { get; set; } = "";

    /// <summary>true = muncul sekali saja (tutorial). false = selalu muncul (mis. menangkap hama).</summary>
    public bool TutorialOnce { get; set; } = true;

    // ---------- Grid ----------
    public int Rows { get; set; } = 3;
    public int Cols { get; set; } = 4;
    /// <summary>pick = klik yang benar, order = klik berurutan dari luar, avoid = klik hama saja.</summary>
    public string Mode { get; set; } = "pick";
    public int Targets { get; set; } = 3;

    // ---------- Hold ----------
    public float TargetMin { get; set; } = 0.45f;
    public float TargetMax { get; set; } = 0.70f;
    public float FillSpeed { get; set; } = 0.55f;
    public int Rounds { get; set; } = 2;

    // ---------- Drag ----------
    /// <summary>Lebar jalur yang aman (0..1). Makin kecil, makin susah.</summary>
    public float Tolerance { get; set; } = 0.13f;

    // ---------- Timing ----------
    public int Hits { get; set; } = 3;
    public float NeedleSpeed { get; set; } = 0.80f;
    public float ZoneWidth { get; set; } = 0.24f;

    // ---------- Umum ----------
    public int WrongAllowed { get; set; } = 2;

    public LessonWidgetKind Kind =>
        Enum.TryParse(Widget, ignoreCase: true, out LessonWidgetKind kind) ? kind : LessonWidgetKind.Grid;

    /// <summary>Event (hama) atau pelajaran tanaman?</summary>
    public bool IsEvent => CropId.Length == 0;
}
