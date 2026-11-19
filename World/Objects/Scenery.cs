namespace FarmGame.World.Objects;

/// <summary>
/// Kelas induk hiasan alam dari file map (pohon, semak, batu, rumput liar, bunga, teratai).
/// Turunan cukup bilang: nama gambarnya, ikut goyang kena angin atau tidak, dan punya berapa variasi.
/// </summary>
public abstract class Scenery : WorldObject
{
    // Urutan frame goyang: 0 = tegak, 1 = condong kanan, 2 = condong kiri
    private static readonly int[] SwaySequence = { 0, 1, 1, 0, 0, 2, 2, 0 };

    protected Scenery(Point tile) : base(tile)
    {
    }

    /// <summary>Nama dasar gambar, contoh "tree". WAJIB diisi turunan.</summary>
    protected abstract string BaseKey { get; }

    /// <summary>Ikut bergoyang kena angin? Default: tidak.</summary>
    protected virtual bool Sways => false;

    /// <summary>Jumlah variasi gambar (misalnya warna bunga). Default: 1.</summary>
    protected virtual int Variants => 1;

    /// <summary>Contoh: "rock", "flowers_2_1" (variasi 2, frame 1), "tree_0".</summary>
    public override string SpriteKey
    {
        get
        {
            string key = BaseKey;
            if (Variants > 1)
                key += "_" + (Tile.X * 7 + Tile.Y * 13) % Variants;   // variasi tetap per posisi
            if (Sways)
                key += "_" + SwayFrame();
            return key;
        }
    }

    /// <summary>
    /// Frame goyang dari jam komputer. Posisi X ikut dihitung, jadi angin terlihat
    /// "berjalan" dari kiri ke kanan seperti gelombang, bukan semua bergerak bersamaan.
    /// </summary>
    private int SwayFrame()
    {
        double seconds = Environment.TickCount64 / 1000.0 - Tile.X * 0.11 - Tile.Y * 0.04;
        int step = (int)Math.Floor(seconds * 3);
        return SwaySequence[((step % SwaySequence.Length) + SwaySequence.Length) % SwaySequence.Length];
    }
}

/// <summary>Pohon: menghalangi jalan, gambar 2x3 tile (daun menutupi pemain yang lewat di belakangnya).</summary>
public class Tree : Scenery
{
    public Tree(Point tile) : base(tile) { }

    protected override string BaseKey => "tree";
    protected override bool Sways => true;
    public override RectangleF SpriteBounds => new(Position.X - 8, Position.Y - 32, 32, 48);
}

/// <summary>Semak: menghalangi jalan, sedikit lebih tinggi dari 1 tile.</summary>
public class Bush : Scenery
{
    public Bush(Point tile) : base(tile) { }

    protected override string BaseKey => "bush";
    public override RectangleF SpriteBounds => new(Position.X, Position.Y - 4, 16, 20);
}

/// <summary>Batu besar: menghalangi jalan.</summary>
public class Rock : Scenery
{
    public Rock(Point tile) : base(tile) { }

    protected override string BaseKey => "rock";
}

/// <summary>Rumput liar: bisa dilewati, bergoyang.</summary>
public class GrassTuft : Scenery
{
    public GrassTuft(Point tile) : base(tile) { }

    protected override string BaseKey => "tuft";
    protected override bool Sways => true;
    public override bool IsSolid => false;
}

/// <summary>Rumpun bunga: bisa dilewati, bergoyang, 3 warna.</summary>
public class Flowers : Scenery
{
    public Flowers(Point tile) : base(tile) { }

    protected override string BaseKey => "flowers";
    protected override bool Sways => true;
    protected override int Variants => 3;
    public override bool IsSolid => false;
}

/// <summary>Teratai di kolam: datar, selalu digambar di bawah.</summary>
public class LilyPad : Scenery
{
    public LilyPad(Point tile) : base(tile) { }

    protected override string BaseKey => "lily";
    protected override int Variants => 2;
    public override bool IsSolid => false;
    public override int DrawOrder => int.MinValue + Tile.Y;
}
