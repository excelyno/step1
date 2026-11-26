namespace FarmGame.Core.States;

/// <summary>
/// Mode jurnal (MVP 4): J membuka, J / Esc / X menutup. Jam berhenti selama jurnal terbuka.
/// Versi baru: Tab = halaman Buku Tani (bintang minigame + latihan ulang).
/// </summary>
public class JournalState : GameState
{
    private bool _showBook;

    public JournalState(Game game) : base(game)
    {
    }

    public override void Enter()
    {
        Game.Time.Pause();
        Game.Player.SetMovementIntent(Vector2.Zero, false);
    }

    public override void Update(float deltaSeconds)
    {
        InputManager input = Game.Input;

        if (input.WasPressed(Keys.Tab))
        {
            Game.PlaySound("select");
            _showBook = !_showBook;
            return;
        }

        if (_showBook && TryStartPractice(input))
            return;

        if (input.WasPressed(Keys.J) || input.MenuClosePressed)
        {
            Game.PlaySound("ui_close");
            Game.ChangeState(new PlayingState(Game));
        }
    }

    /// <summary>Tombol 1..5 atau klik baris = main lagi minigame itu (latihan, dunia tidak berubah).</summary>
    private bool TryStartPractice(InputManager input)
    {
        IReadOnlyList<CropLesson> lessons = Game.LessonData.All;

        for (int i = 0; i < lessons.Count && i < 6; i++)
        {
            bool byKey = input.WasPressed(Keys.D1 + i);
            bool byMouse = input.MouseDownPressed(MouseButtons.Left)
                           && JournalMenu.BookRowRect(i).Contains(input.MousePosition);
            if (byKey || byMouse)
            {
                Game.StartPracticeLesson(lessons[i]);
                return true;
            }
        }
        return false;
    }

    public override void Draw(Graphics g)
    {
        Game.DrawWorldAndHud(g, showToolbar: false);
        Game.Journal.Draw(g, Game, _showBook);
    }
}
