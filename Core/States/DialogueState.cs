namespace FarmGame.Core.States;

/// <summary>
/// Mode dialog (MVP 4): teks muncul huruf demi huruf (efek mesin tik).
/// Tombol lanjut (C / X / Enter / Spasi / Esc):
///   - teks belum selesai -> langsung tampil semua
///   - teks sudah selesai -> halaman berikutnya, atau tutup kalau halaman terakhir
/// Setelah ditutup, onFinished dipanggil (misalnya: quest dimulai). Jam berhenti selama dialog.
/// </summary>
public class DialogueState : GameState
{
    private const float CharactersPerSecond = 45f;

    private readonly string _speaker;
    private readonly IReadOnlyList<string> _pages;
    private readonly Action? _onFinished;
    private readonly string? _portrait;
    private int _page;
    private float _typedCharacters;

    public DialogueState(Game game, string speaker, IReadOnlyList<string> pages, Action? onFinished, string? portrait = null) : base(game)
    {
        _speaker = speaker;
        _pages = pages;
        _onFinished = onFinished;
        _portrait = portrait;
    }

    private string CurrentPage => _pages[_page];
    private bool PageDone => _typedCharacters >= CurrentPage.Length;

    public override void Enter()
    {
        Game.Time.Pause();
        Game.Player.SetMovementIntent(Vector2.Zero, false);
    }

    public override void Update(float deltaSeconds)
    {
        if (_pages.Count == 0)
        {
            Close();
            return;
        }

        _typedCharacters += CharactersPerSecond * deltaSeconds;

        InputManager input = Game.Input;
        if (!input.ConfirmPressed && !input.WasPressed(Keys.Escape))
            return;

        if (!PageDone)
            _typedCharacters = CurrentPage.Length;       // lewati efek mesin tik
        else if (_page < _pages.Count - 1)
        {
            _page++;
            _typedCharacters = 0;
        }
        else
            Close();
    }

    /// <summary>Kembali bermain DULU, baru onFinished (yang boleh membuka dialog lain).</summary>
    private void Close()
    {
        Game.ChangeState(new PlayingState(Game));
        _onFinished?.Invoke();
    }

    public override void Draw(Graphics g)
    {
        Game.DrawWorldAndHud(g, showToolbar: false);
        if (_pages.Count == 0)
            return;

        int shown = Math.Min(CurrentPage.Length, (int)_typedCharacters);
        Game.DialogueBox.Draw(g, _speaker, CurrentPage[..shown], PageDone, _page, _pages.Count, _portrait);
    }
}
