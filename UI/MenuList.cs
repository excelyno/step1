namespace FarmGame.UI;

/// <summary>
/// Daftar pilihan menu sederhana (MVP 5) untuk layar judul dan menu jeda.
/// W/S atau panah = pindah, Enter/Spasi/C = pilih. Pilihan yang dimatikan (abu-abu) dilewati.
/// Pilihan terpilih sedikit membesar dan bergoyang pelan.
/// </summary>
public class MenuList
{
    private readonly List<(string Label, bool Enabled)> _items = new();
    private readonly Font _font = new("Segoe UI", 15f, FontStyle.Bold);
    private float _clock;
    private float _selectAge;

    public int Selected { get; private set; }

    public MenuList Add(string label, bool enabled = true)
    {
        _items.Add((label, enabled));
        if (!_items[Selected].Enabled)
            Selected = _items.Count - 1;
        return this;
    }

    /// <summary>Indeks pilihan yang dipilih frame ini, atau -1.</summary>
    public int Update(float deltaSeconds, InputManager input)
    {
        _clock += deltaSeconds;
        _selectAge += deltaSeconds;
        if (input.MenuUpPressed)
            Move(-1);
        if (input.MenuDownPressed)
            Move(1);
        if (input.MenuSelectPressed && _items[Selected].Enabled)
            return Selected;
        return -1;
    }

    private void Move(int direction)
    {
        for (int step = 1; step <= _items.Count; step++)
        {
            int index = ((Selected + direction * step) % _items.Count + _items.Count) % _items.Count;
            if (_items[index].Enabled)
            {
                Selected = index;
                _selectAge = 0;
                return;
            }
        }
    }

    public void Draw(Graphics g, int centerX, int top, int width = 280)
    {
        const int rowHeight = 50;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        for (int i = 0; i < _items.Count; i++)
        {
            bool chosen = i == Selected;
            float grow = chosen ? Math.Min(1f, _selectAge / 0.12f) * 8f : 0f;
            var box = new Rectangle((int)(centerX - width / 2 - grow / 2), (int)(top + i * rowHeight - grow / 4),
                                    (int)(width + grow), (int)(40 + grow / 2));
            UiPanel.Draw(g, box, paper: chosen);

            string label = _items[i].Label;
            SizeF size = g.MeasureString(label, _font);
            Color color = !_items[i].Enabled ? Color.FromArgb(140, 130, 120)
                        : chosen ? UiPanel.Ink : Color.FromArgb(250, 236, 200);
            using SolidBrush brush = new(color);
            float wobble = chosen ? MathF.Sin(_clock * 4f) * 1.5f : 0;
            g.DrawString(label, _font, brush, box.X + (box.Width - size.Width) / 2, box.Y + (box.Height - size.Height) / 2 + wobble);

            if (chosen)
            {
                // panah kecil di kiri yang maju-mundur
                float arrowX = box.X - 18 + MathF.Sin(_clock * 6f) * 3f;
                float midY = box.Y + box.Height / 2f;
                PointF[] arrow = { new(arrowX, midY - 7), new(arrowX + 10, midY), new(arrowX, midY + 7) };
                using SolidBrush gold = new(Color.Gold);
                g.FillPolygon(gold, arrow);
            }
        }
    }
}
