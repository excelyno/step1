namespace FarmGame.Rendering;

/// <summary>
/// Cache kuas dan pena (MVP 6). Ratusan "new SolidBrush" per frame membuat GC sibuk dan game patah-patah.
/// Semua kuas dicatat berdasarkan warna (Argb) dan dipakai ulang; jumlahnya tetap karena warna UI itu terbatas.
/// Dipakai dengan "using" tetap aman: Dispose kuas cache tidak menghapus kuas aslinya (penanda _owned).
/// </summary>
public static class BrushCache
{
    private static readonly Dictionary<int, SolidBrush> _brushes = new();
    private static readonly Dictionary<(int Argb, float Width), Pen> _pens = new();

    public static SolidBrush Get(Color color) => Get(color.ToArgb());

    public static SolidBrush Get(int argb)
    {
        if (!_brushes.TryGetValue(argb, out SolidBrush? brush))
        {
            brush = new SolidBrush(Color.FromArgb(argb));
            _brushes[argb] = brush;
        }
        return brush;
    }

    /// <summary>Pena dengan lebar 1 (paling sering dipakai untuk bingkai).</summary>
    public static Pen GetPen(Color color) => GetPen(color, 1f);

    public static Pen GetPen(Color color, float width)
    {
        var key = (color.ToArgb(), width);
        if (!_pens.TryGetValue(key, out Pen? pen))
        {
            pen = new Pen(color, width);
            _pens[key] = pen;
        }
        return pen;
    }

    public static int Count => _brushes.Count + _pens.Count;
}
