namespace FarmGame.Farming;

/// <summary>
/// Kelas induk semua minigame bercocok tanam (logika saja, tanpa gambar).
/// Aturan penting untuk anak-anak: TIDAK ADA yang namanya kalah total.
/// Salah beberapa kali = minigame tetap selesai dengan 1 bintang.
/// </summary>
public abstract class LessonWidget
{
    protected LessonWidget(CropLesson lesson)
    {
        Lesson = lesson;
    }

    public CropLesson Lesson { get; }

    /// <summary>Sudah selesai (menang atau sudah terlalu banyak salah)?</summary>
    public bool IsFinished { get; protected set; }

    /// <summary>1..3 bintang.</summary>
    public int Stars { get; protected set; } = 1;

    public int Mistakes { get; protected set; }

    public int MaxMistakes => Math.Max(1, Lesson.WrongAllowed);

    /// <summary>Kedipan umpan balik setelah menekan (detik).</summary>
    public float Flash { get; protected set; }

    /// <summary>Percobaan terakhir berhasil?</summary>
    public bool LastSuccess { get; protected set; }

    /// <summary>
    /// Dipanggil setiap tekanan (benar/salah) supaya state bisa memutar suara.
    /// Diisi oleh CropLessonState; kosong di tes sederhana.
    /// </summary>
    public Action<bool>? OnFeedback;

    private void Feedback(bool success) => OnFeedback?.Invoke(success);

    /// <summary>Tombol yang baru ditekan frame ini (supaya satu klik tidak dihitung dua kali).</summary>
    protected bool PressedThisFrame(InputManager input) => input.UseToolPressed;

    public abstract void Update(float deltaSeconds, InputManager input, Vector2 pointer);

    /// <summary>Tandai selesai; bintang mengikuti jumlah kesalahan.</summary>
    protected void Finish()
    {
        IsFinished = true;
        Stars = Mistakes <= 0 ? 3 : Mistakes == 1 ? 2 : 1;
    }

    protected void AddMistake()
    {
        Mistakes++;
        LastSuccess = false;
        Flash = 0.3f;
        Feedback(false);
        if (Mistakes >= MaxMistakes + 1)
            Finish();   // sudah terlalu banyak salah: tetap selesai, tapi 1 bintang
    }

    protected void AddSuccess()
    {
        LastSuccess = true;
        Flash = 0.3f;
        Feedback(true);
    }

    /// <summary>Bintang 0 = pemain memilih "sudah paham" (dilewati).</summary>
    public static LessonWidget Create(CropLesson lesson) => lesson.Kind switch
    {
        LessonWidgetKind.Timing => new TimingLessonWidget(lesson),
        LessonWidgetKind.Hold => new HoldLessonWidget(lesson),
        LessonWidgetKind.Drag => new DragLessonWidget(lesson),
        _ => new GridLessonWidget(lesson)
    };
}
