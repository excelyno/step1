namespace FarmGame.Core.States;

/// <summary>
/// Mode minigame bercocok tanam. Jam berhenti, pemain diam.
///
/// Alur: Intro (jelaskan cara main) -> Playing (minigame) -> Result (bintang) -> kembali bermain.
/// Spasi = "sudah paham" (lewati), X/Esc = batal (aksi tidak terjadi, bibit/air tidak hilang).
/// Selesai = bintang disimpan ke Buku Tani, lalu aksi aslinya dijalankan (apply).
/// </summary>
public class CropLessonState : GameState
{
    public enum LessonPhase { Intro, Playing, Result }

    private readonly Action? _apply;
    private LessonPhase _phase = LessonPhase.Intro;
    private float _resultTimer;

    /// <summary>Berapa detik layar hasil sudah tampil (bintang muncul satu-satu).</summary>
    public float ResultSeconds => _phase == LessonPhase.Result ? _resultTimer : 0f;

    public CropLessonState(Game game, CropLesson lesson, Action? apply) : base(game)
    {
        Lesson = lesson;
        _apply = apply;
        Widget = LessonWidget.Create(lesson);   // dibuat sekarang supaya Intro bisa menampilkan papannya
        Widget.OnFeedback = success =>
        {
            Game.PlaySound(success ? "good" : "wrong");
            if (!success)
                Game.ShakeCamera(2f, 0.12f);   // layar bergetar pelan: salah terasa
        };
    }

    public CropLesson Lesson { get; }
    public LessonWidget Widget { get; }
    public LessonPhase Phase => _phase;

    /// <summary>Tingkat Muda ke atas: panen dapat +1 buah (diberitahu di layar).</summary>
    public bool GradeBonus => Game.Lessons.Grade >= 2;

    public override void Enter()
    {
        Game.Time.Pause();
        Game.Player.SetMovementIntent(Vector2.Zero, false);
    }

    public override void Update(float deltaSeconds)
    {
        InputManager input = Game.Input;
        Game.Toasts.Update(deltaSeconds);

        switch (_phase)
        {
            case LessonPhase.Intro:
                UpdateIntro(input);
                break;
            case LessonPhase.Playing:
                UpdatePlaying(deltaSeconds, input);
                break;
            case LessonPhase.Result:
                UpdateResult(deltaSeconds, input);
                break;
        }
    }

    private void UpdateIntro(InputManager input)
    {
        if (input.WasPressed(Keys.Escape) || input.WasPressed(Keys.X))
        {
            Cancel();
            return;
        }

        // "Sudah paham": langsung jalankan aksi tanpa bintang
        if (input.WasPressed(Keys.Space))
        {
            Complete(0);
            return;
        }

        bool start = input.WasPressed(Keys.C) || input.WasPressed(Keys.Enter)
                     || input.MouseDownPressed(MouseButtons.Left);
        if (start)
            _phase = LessonPhase.Playing;
    }

    private void UpdatePlaying(float deltaSeconds, InputManager input)
    {
        if (input.WasPressed(Keys.Escape) || input.WasPressed(Keys.X))
        {
            Cancel();
            return;
        }

        Widget.Update(deltaSeconds, input, CropLessonView.Pointer(input.MousePosition));

        if (Widget.IsFinished)
        {
            _phase = LessonPhase.Result;
            _resultTimer = 0f;
        }
    }

    private void UpdateResult(float deltaSeconds, InputManager input)
    {
        _resultTimer += deltaSeconds;
        if (_resultTimer < 0.35f)
            return;   // jangan sampai klik terakhir tadi ikut menutup layar

        if (input.ConfirmPressed)
            Complete(Widget.Stars);
    }

    /// <summary>Simpan bintang, jalankan aksi aslinya, lalu kembali bermain.</summary>
    private void Complete(int stars)
    {
        Game.Lessons.Record(Lesson.Id, stars);
        _apply?.Invoke();
        if (stars >= 2)
            Game.PlaySound("win");
        Game.ChangeState(new PlayingState(Game));
    }

    private void Cancel()
    {
        Game.PlaySound("ui_close");
        Game.Notify("Minigame dibatalkan.", false);
        Game.ChangeState(new PlayingState(Game));
    }

    public override void Draw(Graphics g)
    {
        Game.DrawWorldAndHud(g, showToolbar: false);
        Game.CropLessonView.Draw(g, this, Game.Input.MousePosition);
        Game.Toasts.Draw(g);
    }
}
