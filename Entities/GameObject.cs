namespace FarmGame.Entities;

/// <summary>
/// Kelas induk SEMUA benda di dunia game (player, NPC, batu, pohon, dll).
/// Position = pojok kiri-atas HITBOX (area tabrakan), dalam piksel logis.
/// Kelas ini abstract: tidak bisa di-new langsung, harus lewat turunannya.
/// </summary>
public abstract class GameObject : IUpdatable, IDrawable
{
    public Vector2 Position { get; protected set; }

    /// <summary>Lokasi tempat benda ini berada. Diisi oleh GameLocation.Add().</summary>
    public GameLocation? Location { get; set; }

    /// <summary>Ukuran area tabrakan. WAJIB ditentukan oleh kelas turunan.</summary>
    public abstract SizeF HitboxSize { get; }

    /// <summary>Apakah benda ini menghalangi benda lain? Default: tidak.</summary>
    public virtual bool IsSolid => false;

    public RectangleF Bounds => new(Position.X, Position.Y, HitboxSize.Width, HitboxSize.Height);

    /// <summary>
    /// MVP 5: kotak tabrakan yang SEBENARNYA dipakai saat berjalan. Default = Bounds.
    /// Turunan boleh mengecilkannya (pohon: hanya batang, lampu: hanya tiang) supaya terasa pas,
    /// seperti di game sungguhan: gambar boleh besar, tapi yang menabrak hanya "kaki"-nya.
    /// </summary>
    public virtual RectangleF CollisionBox => Bounds;

    /// <summary>MVP 5: jadi tembus pandang kalau pemain berjalan di belakangnya (atap rumah, daun pohon).</summary>
    public virtual bool FadesWhenBehind => false;

    /// <summary>MVP 5: tingkat kelihatan saat ini (1 = penuh). Diatur Renderer pelan-pelan, bukan langsung.</summary>
    public float Opacity { get; set; } = 1f;

    public Vector2 Center => new(Position.X + HitboxSize.Width / 2f, Position.Y + HitboxSize.Height / 2f);

    public Point TilePosition => new(TileMap.WorldToTile(Center.X), TileMap.WorldToTile(Center.Y));

    /// <summary>Area gambar. Default sama dengan hitbox; Character membuatnya lebih tinggi.</summary>
    public virtual RectangleF SpriteBounds => Bounds;

    /// <summary>Y bagian bawah hitbox. Benda yang lebih "ke bawah" digambar belakangan (tampil di depan).</summary>
    /// MVP 1: virtual, supaya HoeDirt bisa selalu digambar paling bawah.
    public virtual int DrawOrder => (int)Bounds.Bottom;

    /// <summary>Pindah langsung tanpa cek tabrakan (dipakai saat spawn).</summary>
    public void PlaceAt(Vector2 position) => Position = position;

    /// <summary>Default tidak melakukan apa-apa. Turunan boleh menimpa (override).</summary>
    public virtual void Update(float deltaSeconds) { }

    /// <summary>MVP 4: bayangan di tanah, digambar sebelum SEMUA objek. Default: tidak ada (sprite sudah punya bayangan).</summary>
    public virtual void DrawShadow(Graphics g) { }

    /// <summary>Cara menggambar. WAJIB ditulis oleh kelas turunan.</summary>
    public abstract void Draw(Graphics g, IAssetProvider assets);
}
