namespace FarmGame.Farming;

/// <summary>
/// Minigame "tekan di saat tepat": jarum bergerak bolak-balik, tekan saat di zona hijau.
/// Dipakai untuk mematahkan tongkol jagung.
/// </summary>
public class TimingLessonWidget : LessonWidget
{
    private float _position;
    private float _direction = 1f;

    public TimingLessonWidget(CropLesson lesson) : base(lesson)
    {
        MoveZone();
    }

    public int Hits { get; private set; }
    public float Position => _position;
    public float ZoneStart { get; private set; }
    public float ZoneWidth => Lesson.ZoneWidth;
    public int HitsNeeded => Math.Max(1, Lesson.Hits);

    public override void Update(float deltaSeconds, InputManager input, Vector2 pointer)
    {
        if (IsFinished)
            return;

        Flash = Math.Max(0, Flash - deltaSeconds);

        _position += _direction * Lesson.NeedleSpeed * deltaSeconds;
        if (_position >= 1f) { _position = 1f; _direction = -1f; }
        if (_position <= 0f) { _position = 0f; _direction = 1f; }

        if (PressedThisFrame(input))
            Press();
    }

    public void Press()
    {
        if (IsFinished)
            return;

        bool hit = _position >= ZoneStart && _position <= ZoneStart + ZoneWidth;
        if (hit)
        {
            AddSuccess();
            Hits++;
            if (Hits >= HitsNeeded)
            {
                Finish();
                return;
            }
            MoveZone();
        }
        else
        {
            AddMistake();
        }
    }

    /// <summary>Zona dipindah ke tempat acak yang jauh dari jarum, supaya tidak "kena gratis".</summary>
    private void MoveZone()
    {
        for (int attempt = 0; attempt < 12; attempt++)
        {
            ZoneStart = (float)Random.Shared.NextDouble() * (1f - ZoneWidth);
            if (Math.Abs(ZoneStart + ZoneWidth / 2 - _position) > 0.28f)
                return;
        }
    }
}
