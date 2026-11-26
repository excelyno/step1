namespace FarmGame.Farming;

/// <summary>
/// Minigame grid: klik sel yang benar.
///   pick  = klik semua yang matang (terong), yang pucat jangan
///   order = klik berurutan dari luar ke dalam (daun kale)
///   avoid = klik semua hama, jangan kena daun (event hama)
/// Bisa dimainkan dengan mouse (klik sel) atau keyboard (panah + C).
/// </summary>
public class GridLessonWidget : LessonWidget
{
    public enum Cell
    {
        Empty,
        Good,   // harus diklik
        Bad     // jangan diklik
    }

    private readonly Cell[,] _cells;
    private readonly int[,] _order;
    private readonly bool[,] _done;
    private float _ignore = 0.18f;

    public GridLessonWidget(CropLesson lesson) : base(lesson)
    {
        Rows = Math.Clamp(lesson.Rows, 2, 6);
        Cols = Math.Clamp(lesson.Cols, 2, 6);
        _cells = new Cell[Rows, Cols];
        _order = new int[Rows, Cols];
        _done = new bool[Rows, Cols];

        if (string.Equals(lesson.Mode, "order", StringComparison.OrdinalIgnoreCase))
            BuildOrderLayout();
        else
            BuildRandomLayout();

        GoodTotal = CountGood();
        if (GoodTotal == 0)
        {
            // Data aneh (targets 0): jangan sampai minigame mustahil
            _cells[0, 0] = Cell.Good;
            GoodTotal = 1;
        }
    }

    public int Rows { get; }
    public int Cols { get; }
    public int GoodTotal { get; }
    public int GoodDone { get; private set; }
    public int NextOrder { get; private set; } = 1;
    public int CursorCol { get; private set; }
    public int CursorRow { get; private set; }
    public bool IsOrderMode => string.Equals(Lesson.Mode, "order", StringComparison.OrdinalIgnoreCase);

    public Cell CellAt(int col, int row) => _cells[row, col];
    public bool IsDone(int col, int row) => _done[row, col];
    public int OrderAt(int col, int row) => _order[row, col];

    public override void Update(float deltaSeconds, InputManager input, Vector2 pointer)
    {
        if (IsFinished)
            return;

        Flash = Math.Max(0, Flash - deltaSeconds);
        _ignore = Math.Max(0, _ignore - deltaSeconds);
        if (_ignore > 0)
            return;

        if (input.WasPressed(Keys.A) || input.WasPressed(Keys.Left))
            CursorCol = (CursorCol - 1 + Cols) % Cols;
        if (input.WasPressed(Keys.D) || input.WasPressed(Keys.Right))
            CursorCol = (CursorCol + 1) % Cols;
        if (input.WasPressed(Keys.W) || input.WasPressed(Keys.Up))
            CursorRow = (CursorRow - 1 + Rows) % Rows;
        if (input.WasPressed(Keys.S) || input.WasPressed(Keys.Down))
            CursorRow = (CursorRow + 1) % Rows;

        if (!PressedThisFrame(input))
            return;

        // Klik mouse di dalam papan -> sel yang ditunjuk. Selain itu -> sel kursor keyboard.
        bool inside = pointer.X >= 0f && pointer.X <= 1f && pointer.Y >= 0f && pointer.Y <= 1f;
        if (inside)
        {
            int col = Math.Clamp((int)(pointer.X * Cols), 0, Cols - 1);
            int row = Math.Clamp((int)(pointer.Y * Rows), 0, Rows - 1);
            CursorCol = col;
            CursorRow = row;
            Click(col, row);
        }
        else
        {
            Click(CursorCol, CursorRow);
        }
    }

    public void Click(int col, int row)
    {
        if (IsFinished || _done[row, col])
            return;

        switch (_cells[row, col])
        {
            case Cell.Bad:
                _done[row, col] = true;
                AddMistake();
                break;

            case Cell.Good:
                if (IsOrderMode && _order[row, col] != NextOrder)
                {
                    AddMistake();   // urutan salah: daun luar dulu, baru dalam
                    break;
                }
                _done[row, col] = true;
                GoodDone++;
                NextOrder++;
                AddSuccess();
                if (GoodDone >= GoodTotal)
                    Finish();
                break;
        }
    }

    // ---------- Menyusun papan ----------

    private void BuildRandomLayout()
    {
        int good = Math.Clamp(Lesson.Targets, 1, Rows * Cols - 1);
        int bad = Math.Min(3, Rows * Cols - good);

        List<(int Col, int Row)> cells = AllCells();
        Shuffle(cells);

        for (int i = 0; i < good; i++)
            _cells[cells[i].Row, cells[i].Col] = Cell.Good;
        for (int i = good; i < good + bad; i++)
            _cells[cells[i].Row, cells[i].Col] = Cell.Bad;
    }

    /// <summary>Kale: daun di tepi (luar) diberi nomor searah jarum jam, tengah = jangan diklik.</summary>
    private void BuildOrderLayout()
    {
        List<(int Col, int Row)> border = new();
        for (int col = 0; col < Cols; col++)
            border.Add((col, 0));
        for (int row = 1; row < Rows; row++)
            border.Add((Cols - 1, row));
        for (int col = Cols - 2; col >= 0; col--)
            border.Add((col, Rows - 1));
        for (int row = Rows - 2; row >= 1; row--)
            border.Add((0, row));

        int count = Math.Clamp(Lesson.Targets, 1, border.Count);
        int step = Math.Max(1, border.Count / count);
        int number = 1;
        for (int i = 0; i < border.Count && number <= count; i += step)
        {
            var (col, row) = border[i];
            _cells[row, col] = Cell.Good;
            _order[row, col] = number++;
        }

        // Sisakan sedikit daun yang tidak boleh dipetik (pucuk di tengah)
        _cells[Rows / 2, Cols / 2] = Cell.Bad;
    }

    private int CountGood()
    {
        int total = 0;
        foreach (Cell cell in _cells)
            if (cell == Cell.Good)
                total++;
        return total;
    }

    private List<(int Col, int Row)> AllCells()
    {
        List<(int, int)> list = new();
        for (int row = 0; row < Rows; row++)
            for (int col = 0; col < Cols; col++)
                list.Add((col, row));
        return list;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
