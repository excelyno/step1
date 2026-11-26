namespace FarmGame.Farming;

/// <summary>
/// Minigame "tahan lalu lepas": tahan tombol untuk memutar/mengisi meter,
/// lalu LEPAS saat penanda di zona hijau. Terlalu lama = kelewatan (buah pecah).
/// Dipakai untuk memetik tomat dengan halus.
/// </summary>
public class HoldLessonWidget : LessonWidget
{
    private float _value;
    private bool _holding;
    private float _ignore = 0.25f;

    public HoldLessonWidget(CropLesson lesson) : base(lesson)
    {
    }

    public float Value => _value;
    public float TargetMin => Lesson.TargetMin;
    public float TargetMax => Lesson.TargetMax;
    public int Rounds { get; private set; }
    public int RoundsNeeded => Math.Max(1, Lesson.Rounds);
    public bool IsHolding => _holding;

    public override void Update(float deltaSeconds, InputManager input, Vector2 pointer)
    {
        if (IsFinished)
            return;

        Flash = Math.Max(0, Flash - deltaSeconds);
        _ignore = Math.Max(0, _ignore - deltaSeconds);

        bool held = input.IsMouseHeld(MouseButtons.Left) || input.IsHeld(Keys.C);
        if (_ignore > 0)
            held = false;

        if (held)
        {
            _holding = true;
            _value += Lesson.FillSpeed * deltaSeconds;
            if (_value >= 1f)
            {
                // Terlalu lama menahan: buahnya pecah. Pemain harus melepas lalu menekan lagi.
                _value = 1f;
                _holding = false;
                AddMistake();
                Reset();
            }
        }
        else if (_holding)
        {
            _holding = false;
            Release();
        }
    }

    /// <summary>Pemain melepas tombol.</summary>
    private void Release()
    {
        if (IsFinished)
            return;

        bool inZone = _value >= TargetMin && _value <= TargetMax;
        if (inZone)
        {
            AddSuccess();
            Rounds++;
            if (Rounds >= RoundsNeeded)
            {
                Finish();
                return;
            }
        }
        else if (_value > 0.02f)
        {
            AddMistake();   // dilepas terlalu cepat / terlalu lambat
        }

        Reset();
    }

    private void Reset() => _value = 0f;
}
