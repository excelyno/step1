namespace FarmGame.Saving;

/// <summary>
/// Satu benda dunia di file simpanan: jenisnya, tile-nya, dan angka-angka tambahan (umur tunggul, hari tumbuh, ...).
/// Bentuknya sengaja sederhana supaya file JSON mudah dibaca manusia.
/// </summary>
public class ObjectSave
{
    public string Kind { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public Dictionary<string, int> Data { get; set; } = new();
    public string Text { get; set; } = "";   // id tanaman / id barang liar

    public ObjectSave()
    {
    }

    public ObjectSave(string kind, Point tile, string text = "")
    {
        Kind = kind;
        X = tile.X;
        Y = tile.Y;
        Text = text;
    }

    public Point Tile => new(X, Y);

    public int Get(string key) => Data.GetValueOrDefault(key);
}
