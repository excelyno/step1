namespace FarmGame.Farming;

/// <summary>
/// Minigame "putari lingkaran": tahan klik lalu gerakkan mouse mengelilingi umbi.
/// Kalau menyentuh bagian tengah (umbinya) = salah. Satu putaran penuh = selesai.
/// Dipakai untuk menggali kentang.
/// </summary>
public class DragLessonWidget : LessonWidget
{
    /// <summary>Jari-jari jalur yang aman, di ruang 0..1 (tengah papan = 0,5).</summary>
    public const float RingRadius = 0.30f;

    private Vector2? _last;
    private float _swept;
    private bool _insideCenter;

    public DragLessonWidget(CropLesson lesson) : base(lesson)
    {
    }

    /// <summary>0..1 = sudah berapa bagian lingkaran yang digaruk.</summary>
    public float Progress => Math.Clamp(_swept / MathF.Tau, 0f, 1f);

    public float Band => Math.Max(0.05f, Lesson.Tolerance);

    /// <summary>Posisi terakhir di jalur (untuk gambar penanda).</summary>
    public Vector2? Marker { get; private set; }

    public override void Update(float deltaSeconds, InputManager input, Vector2 pointer)
    {
        if (IsFinished)
            return;

        Flash = Math.Max(0, Flash - deltaSeconds);

        if (!input.IsMouseHeld(MouseButtons.Left) && !input.IsHeld(Keys.C))
        {
            _last = null;
            Marker = null;
            return;
        }

        Vector2 offset = pointer - new Vector2(0.5f, 0.5f);
        float distance = offset.Length();

        // Menyentuh umbi di tengah = salah (sekali per masuk, bukan tiap frame)
        if (distance < RingRadius - Band)
        {
            if (!_insideCenter)
            {
                _insideCenter = true;
                AddMistake();
            }
            _last = null;
            Marker = null;
            return;
        }
        _insideCenter = false;

        // Terlalu jauh dari jalur: tidak dihitung, tapi tidak dihukum
        if (distance > RingRadius + Band)
        {
            _last = null;
            Marker = null;
            return;
        }

        Marker = offset;
        float angle = MathF.Atan2(offset.Y, offset.X);
        if (_last != null)
        {
            float delta = angle - MathF.Atan2(_last.Value.Y, _last.Value.X);
            while (delta > MathF.PI) delta -= MathF.Tau;
            while (delta < -MathF.PI) delta += MathF.Tau;
            _swept += MathF.Abs(delta);

            if (_swept >= MathF.Tau)
            {
                Finish();
                return;
            }
        }
        _last = offset;
    }
}
