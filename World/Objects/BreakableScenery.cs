namespace FarmGame.World.Objects;

/// <summary>
/// Hiasan yang bisa dihancurkan dengan alat (MVP 3). Setiap pukulan mengurangi "nyawa" dan membuatnya bergetar.
/// Pukulan terakhir memberi bahan ke tas lalu benda hilang (atau berubah, lihat Tree).
/// Turunan cukup mengisi: alat yang dibutuhkan, nyawa, bahan yang didapat, dan jumlahnya.
/// MVP 5: bahan yang didapat muncul melayang ("+6 Kayu"); semuanya ikut disimpan (ISaveable).
/// </summary>
public abstract class BreakableScenery : Scenery, IBreakable, ISaveable
{
    private const float ShakeSeconds = 0.25f;

    private int _health;
    private float _shakeTimer;

    protected BreakableScenery(Point tile) : base(tile)
    {
        _health = MaxHealth;
    }

    public abstract ToolKind RequiredTool { get; }
    protected abstract int MaxHealth { get; }
    protected abstract string DropItemId { get; }
    protected abstract int DropAmount { get; }

    /// <summary>Nama jenis di file simpanan ("tree", "rock", ...).</summary>
    public abstract string SaveKind { get; }

    public virtual ObjectSave ToSave() => new(SaveKind, Tile);

    public ActionResult TakeHit(ActionContext context)
    {
        Item drop = context.Services.Items.Create(DropItemId);

        // Pukulan terakhir hanya boleh kalau bahannya muat di tas (supaya bahan tidak hilang)
        if (_health == 1 && !context.User.Inventory.CanFit(drop, DropAmount))
            return ActionResult.Fail("Tas penuh!");

        _health--;
        _shakeTimer = ShakeSeconds;
        if (_health > 0)
            return ActionResult.Ok();

        context.User.Inventory.TryAdd(drop, DropAmount);
        context.Services.ShowPickup(drop, DropAmount, new Vector2(Center.X, Bounds.Top - 8));
        OnBroken(context);
        return ActionResult.Ok();
    }

    /// <summary>Apa yang terjadi setelah hancur. Default: hilang dari lokasi.</summary>
    protected virtual void OnBroken(ActionContext context)
    {
        context.Location.Remove(this);
    }

    public override void Update(float deltaSeconds)
    {
        base.Update(deltaSeconds);
        _shakeTimer = Math.Max(0, _shakeTimer - deltaSeconds);
    }

    /// <summary>Bergetar kiri-kanan sebentar setelah dipukul.</summary>
    public override void Draw(Graphics g, IAssetProvider assets)
    {
        RectangleF bounds = SpriteBounds;
        if (_shakeTimer > 0)
            bounds.Offset(MathF.Sin(_shakeTimer * 60f) * 1.5f, 0);
        assets.DrawSprite(g, SpriteKey, bounds);
    }
}

/// <summary>
/// Pohon rindang: gambar 3x4 tile (2x tinggi pemain). MVP 3: 5x tebang = 6 kayu, sisa tunggul yang tumbuh lagi.
/// MVP 5: yang menabrak hanya batangnya; daunnya tembus pandang kalau pemain di belakangnya; tumbang dengan animasi.
/// </summary>
public class Tree : BreakableScenery
{
    public Tree(Point tile) : base(tile) { }

    protected override string BaseKey => "tree";
    protected override bool Sways => true;
    public override RectangleF SpriteBounds => new(Position.X - 16, Position.Y - 48, 48, 64);
    public override RectangleF CollisionBox => new(Position.X + 3, Position.Y + 7, 10, 8);
    public override bool FadesWhenBehind => true;
    public override string SaveKind => "tree";

    public override ToolKind RequiredTool => ToolKind.Axe;
    protected override int MaxHealth => 5;
    protected override string DropItemId => "wood";
    protected override int DropAmount => 6;

    /// <summary>Cemara atau bukan (tunggulnya tumbuh lagi jadi jenis yang sama).</summary>
    public virtual bool IsPine => false;

    /// <summary>Pohon tumbang (animasi menjauhi pemain), layar bergetar, lalu tinggal tunggul di tile yang sama.</summary>
    protected override void OnBroken(ActionContext context)
    {
        GameLocation location = context.Location;
        location.Remove(this);
        location.Add(new TreeStump(Tile, IsPine));

        bool fallRight = context.User.Center.X <= Center.X;
        location.Add(new FallingTree(SpriteKey, SpriteBounds, new Vector2(Center.X, Bounds.Bottom - 2), fallRight));
    }
}

/// <summary>MVP 5: cemara. Sama seperti pohon rindang, hanya bentuknya ramping.</summary>
public class Pine : Tree
{
    public Pine(Point tile) : base(tile) { }

    protected override string BaseKey => "pine";
    public override RectangleF SpriteBounds => new(Position.X - 8, Position.Y - 48, 32, 64);
    public override bool IsPine => true;
    public override string SaveKind => "pine";
}

/// <summary>Batu besar: menghalangi jalan. MVP 3: 3x pukul beliung = 3 batu.</summary>
public class Rock : BreakableScenery
{
    public Rock(Point tile) : base(tile) { }

    protected override string BaseKey => "rock";
    public override RectangleF CollisionBox => new(Position.X + 1, Position.Y + 4, 14, 11);
    public override string SaveKind => "rock";

    public override ToolKind RequiredTool => ToolKind.Pickaxe;
    protected override int MaxHealth => 3;
    protected override string DropItemId => "stone";
    protected override int DropAmount => 3;
}

/// <summary>Ranting di tanah kebun (huruf 'w' di map): 1x kapak = 1 kayu. MVP 5: bisa dilangkahi.</summary>
public class Twig : BreakableScenery
{
    public Twig(Point tile) : base(tile) { }

    protected override string BaseKey => "twig";
    protected override int Variants => 2;
    public override bool IsSolid => false;
    public override string SaveKind => "twig";

    public override ToolKind RequiredTool => ToolKind.Axe;
    protected override int MaxHealth => 1;
    protected override string DropItemId => "wood";
    protected override int DropAmount => 1;
}

/// <summary>Kerikil di tanah kebun (huruf 'p' di map): 1x beliung = 1 batu. MVP 5: bisa dilangkahi.</summary>
public class Pebble : BreakableScenery
{
    public Pebble(Point tile) : base(tile) { }

    protected override string BaseKey => "pebble";
    protected override int Variants => 2;
    public override bool IsSolid => false;
    public override string SaveKind => "pebble";

    public override ToolKind RequiredTool => ToolKind.Pickaxe;
    protected override int MaxHealth => 1;
    protected override string DropItemId => "stone";
    protected override int DropAmount => 1;
}

/// <summary>Tunggul pohon. Setiap pagi bertambah umur; setelah beberapa hari tumbuh jadi pohon lagi.</summary>
public class TreeStump : Scenery, IDayUpdatable, ISaveable
{
    public const int DaysToRegrow = 5;

    public TreeStump(Point tile, bool isPine = false, int age = 0) : base(tile)
    {
        IsPine = isPine;
        Age = age;
    }

    public bool IsPine { get; }
    public int Age { get; private set; }

    protected override string BaseKey => "stump";
    public override RectangleF CollisionBox => new(Position.X + 2, Position.Y + 5, 12, 10);

    public ObjectSave ToSave()
    {
        ObjectSave save = new("stump", Tile);
        save.Data["age"] = Age;
        save.Data["pine"] = IsPine ? 1 : 0;
        return save;
    }

    public void OnNewDay(GameDate newDate)
    {
        Age++;
        if (Age < DaysToRegrow || Location == null)
            return;

        GameLocation location = Location;   // simpan dulu: Remove() mengosongkan Location
        location.Remove(this);
        location.Add(IsPine ? new Pine(Tile) : new Tree(Tile));
    }
}
