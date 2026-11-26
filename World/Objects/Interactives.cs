namespace FarmGame.World.Objects;

/// <summary>
/// Kotak surat di kebun (MVP 5). Bendera merah naik kalau ada surat. X = baca surat paling lama;
/// hadiah di dalam surat (bibit, uang) langsung masuk tas.
/// </summary>
public class Mailbox : WorldObject, IInteractable
{
    public Mailbox(Point tile) : base(tile)
    {
    }

    private bool HasMail => Location?.Services?.Mail.HasUnread == true;

    public override string SpriteKey => HasMail ? "mailbox_1" : "mailbox_0";
    public override RectangleF SpriteBounds => new(Position.X, Position.Y - 12, 16, 28);
    public override RectangleF CollisionBox => new(Position.X + 5, Position.Y + 9, 6, 6);

    public bool CanInteract(Player player) => true;

    public string InteractLabel(Player player) => HasMail ? "Baca surat" : "Periksa";

    public ActionResult Interact(ActionContext context)
    {
        IGameServices services = context.Services;
        Letter? letter = services.Mail.TakeNext();
        if (letter == null)
            return ActionResult.Fail("Kotak surat kosong.");

        services.ShowDialogue("Surat dari " + letter.From, letter.Pages, () => GiveAttachments(letter, context));
        return ActionResult.None();
    }

    /// <summary>Barang terlampir masuk tas; yang tidak muat diberi tahu.</summary>
    private void GiveAttachments(Letter letter, ActionContext context)
    {
        IGameServices services = context.Services;
        Vector2 at = new(Center.X, SpriteBounds.Top);
        foreach (QuestItemReward attached in letter.Items)
        {
            Item item = services.Items.Create(attached.Id);
            int left = context.User.Inventory.TryAdd(item, attached.Amount);
            if (left < attached.Amount)
                services.ShowPickup(item, attached.Amount - left, at);
            if (left > 0)
                services.Notify("Tas penuh, sebagian hadiah surat hilang.", false);
        }
        if (letter.Gold > 0)
        {
            context.User.Wallet.Earn(letter.Gold);
            services.ShowFloatingText($"+{letter.Gold}g", at, Color.Gold);
        }
    }
}

/// <summary>Sumur (MVP 5): isi penyiram (C atau X sambil memegang penyiram), atau minum: +15 energi sekali sehari.</summary>
public class Well : WorldObject, IInteractable, IWaterSource
{
    private const int DrinkEnergy = 15;
    private int _lastDrinkDay = -1;

    public Well(Point tile) : base(tile)
    {
    }

    public override Size SizeInTiles => new(2, 1);
    public override string SpriteKey => "well";
    public override RectangleF SpriteBounds => new(Position.X, Position.Y - 24, 32, 40);
    public override RectangleF CollisionBox => new(Position.X + 1, Position.Y + 2, 30, 13);

    public bool CanInteract(Player player) => true;

    public string InteractLabel(Player player) => player.Inventory.SelectedItem is WateringCan ? "Isi air" : "Minum";

    public ActionResult Interact(ActionContext context)
    {
        if (context.User.Inventory.SelectedItem is WateringCan can)
        {
        if (!can.Refill())
            return ActionResult.Fail("Penyiram sudah penuh.");
        context.Services.SpawnEffect(EffectKind.Splash, Center);
        context.Services.PlaySound("water");
        return ActionResult.Ok("Penyiram terisi penuh.");
        }

        int today = MailSystem.DayNumber(context.Services.Date);
        if (_lastDrinkDay == today)
            return ActionResult.Ok("Airnya segar sekali.");
        _lastDrinkDay = today;
        context.User.Stamina.Restore(DrinkEnergy);
        context.Services.SpawnEffect(EffectKind.Splash, Center);
        context.Services.ShowFloatingText($"+{DrinkEnergy} energi", new Vector2(Center.X, SpriteBounds.Top), Color.LightGreen);
        return ActionResult.None();
    }
}

/// <summary>
/// Air mancur di alun-alun (MVP 5): airnya beranimasi. X = lempar koin (1g) dan membuat permohonan.
/// Kadang-kadang "terkabul": koin memantul kembali bersama 25g.
/// </summary>
public class Fountain : WorldObject, IInteractable, IWaterSource
{
    private static readonly Random Rng = new();

    private static readonly string[] Wishes =
    {
        "Semoga panen tahun ini melimpah...",
        "Semoga kebun Kakek kembali jadi yang tersubur di desa...",
        "Semoga besok dapat ikan besar...",
        "Semoga semua warga Desa Tandur sehat selalu...",
        "Semoga hujan turun tepat waktu...",
    };

    public Fountain(Point tile) : base(tile)
    {
    }

    public override Size SizeInTiles => new(3, 2);
    public override string SpriteKey => "fountain_" + (Environment.TickCount64 / 180 % 3);
    public override RectangleF SpriteBounds => new(Position.X, Position.Y - 16, 48, 48);
    public override RectangleF CollisionBox => new(Position.X + 2, Position.Y + 4, 44, 26);

    public bool CanInteract(Player player) => true;
    public string InteractLabel(Player player) => "Lempar koin";

    public ActionResult Interact(ActionContext context)
    {
        if (!context.User.Wallet.TrySpend(1))
            return ActionResult.Fail("Kamu tidak punya koin.");

        IGameServices services = context.Services;
        services.SpawnEffect(EffectKind.Splash, Center);
        services.ShowFloatingText("-1g", new Vector2(Center.X, SpriteBounds.Top + 8), Color.Gold);

        if (Rng.Next(8) == 0)
        {
            context.User.Wallet.Earn(25);
            services.PlaySound("coin");
            services.ShowFloatingText("+25g!", new Vector2(Center.X, SpriteBounds.Top), Color.Gold);
            return ActionResult.Ok("Koinnya berkilau dan memantul kembali... keberuntungan! +25g");
        }
        return ActionResult.Ok(Wishes[Rng.Next(Wishes.Length)]);
    }
}

/// <summary>Bangku taman (MVP 5): X = duduk sebentar. Waktu berjalan 30 menit, energi +20.</summary>
public class Bench : WorldObject, IInteractable
{
    private const int RestMinutes = 30;
    private const int RestEnergy = 20;

    public Bench(Point tile) : base(tile)
    {
    }

    public override Size SizeInTiles => new(2, 1);
    public override string SpriteKey => "bench";
    public override RectangleF SpriteBounds => new(Position.X, Position.Y - 6, 32, 22);
    public override RectangleF CollisionBox => new(Position.X + 1, Position.Y + 2, 30, 10);

    public bool CanInteract(Player player) => true;
    public string InteractLabel(Player player) => "Duduk";

    public ActionResult Interact(ActionContext context)
    {
        context.Services.PassTime(RestMinutes);
        context.User.Stamina.Restore(RestEnergy);
        context.Services.ShowFloatingText($"+{RestEnergy} energi", new Vector2(Center.X, SpriteBounds.Top), Color.LightGreen);
        return ActionResult.Ok("Kamu duduk sebentar menikmati suasana desa. (+30 menit)");
    }
}

/// <summary>Papan pengumuman desa (MVP 5): kabar desa sesuai hari + catatan quest yang sedang aktif.</summary>
public class NoticeBoard : WorldObject, IInteractable
{
    private static readonly string[] News =
    {
        "PENGUMUMAN: Toko Bibit Bu Sari buka setiap hari pukul 09:00 - 17:00.",
        "DICARI: teman memancing. Hubungi Raka di dermaga sungai.",
        "INFO: Jamur, beri hutan, dan bunga liar tumbuh sendiri di padang. Pungut lalu jual atau jadikan hadiah!",
        "PENGINGAT: Sumur di kebun bisa dipakai mengisi penyiram.",
        "KABAR: Warga yang sering disapa lebih cepat akrab. Sapalah tetanggamu setiap hari!",
    };

    public NoticeBoard(Point tile) : base(tile)
    {
    }

    public override Size SizeInTiles => new(2, 1);
    public override string SpriteKey => "noticeboard";
    public override RectangleF SpriteBounds => new(Position.X, Position.Y - 16, 32, 32);
    public override RectangleF CollisionBox => new(Position.X + 3, Position.Y + 8, 26, 7);

    public bool CanInteract(Player player) => true;
    public string InteractLabel(Player player) => "Lihat";

    public ActionResult Interact(ActionContext context)
    {
        IGameServices services = context.Services;
        List<string> pages = new() { News[services.Date.Day % News.Length] };

        foreach (QuestData quest in services.Quests.Active)
            pages.Add($"Catatan \"{quest.Title}\": {quest.Description}");
        if (services.Quests.Active.Count == 0)
            pages.Add("Tidak ada catatan baru. Cari warga dengan tanda \"!\" di atas kepalanya.");

        services.ShowDialogue("Papan Pengumuman", pages);
        return ActionResult.None();
    }
}

/// <summary>
/// Barang liar di tanah (MVP 5): jamur, beri hutan, bunga liar. Tumbuh sendiri tiap pagi di padang.
/// Bisa dilangkahi; X = pungut.
/// </summary>
public class Forage : WorldObject, IInteractable, ISaveable
{
    public static readonly string[] ItemIds = { "mushroom", "berries", "wildflower" };

    public string ItemId { get; }

    public Forage(Point tile, string itemId) : base(tile)
    {
        ItemId = itemId;
    }

    public override string SpriteKey => "forage_" + ItemId;
    public override bool IsSolid => false;

    /// <summary>Naik-turun pelan supaya mudah terlihat.</summary>
    public override RectangleF SpriteBounds
    {
        get
        {
            float bob = MathF.Sin(Environment.TickCount64 / 300f + Tile.X) * 0.8f;
            return new RectangleF(Position.X, Position.Y - 1 + bob, 16, 16);
        }
    }

    public ObjectSave ToSave() => new("forage", Tile, ItemId);

    public bool CanInteract(Player player) => true;
    public string InteractLabel(Player player) => "Pungut";

    public ActionResult Interact(ActionContext context)
    {
        Item item = context.Services.Items.Create(ItemId);
        if (context.User.Inventory.TryAdd(item) > 0)
            return ActionResult.Fail("Tas penuh!");

        context.Location.Remove(this);
        context.Services.SpawnEffect(EffectKind.Leaves, Center);
        context.Services.ShowPickup(item, 1, new Vector2(Center.X, Position.Y));
        return ActionResult.None();
    }
}
