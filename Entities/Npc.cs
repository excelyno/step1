namespace FarmGame.Entities;

/// <summary>
/// Warga desa. Turunan Character, jadi gerak + tabrakan + animasi jalan sudah gratis.
/// Datanya (nama, kalimat, selera hadiah) dari npcs.json.
///
/// Diajak bicara (X), urutan prioritasnya:
///   1. Ada quest yang bisa diselesaikan di sini  -> selesaikan, cerita penutup
///   2. Ada quest baru                            -> cerita pembuka, quest dimulai
///   3. Pegang makanan/bahan & belum memberi hari ini -> hadiah
///   4. Selain itu                                -> ngobrol biasa (+pertemanan sekali sehari)
///
/// NPC tidak solid, supaya tidak pernah "mengurung" pemain saat berjalan.
/// </summary>
public class Npc : Character, IInteractable
{
    private const float WalkSpeed = 24f;
    private const float TalkPauseSeconds = 3f;

    private static readonly Random Rng = new();

    private readonly RectangleF? _wanderArea;   // null = berdiri diam
    private readonly Direction _homeFacing;     // arah hadap saat diam

    private Vector2 _walkDirection;
    private float _decisionTimer;
    private float _talkPause;
    private int _talkCount;
    private float _markerBob;

    public NpcData Data { get; }

    /// <summary>Tanda di atas kepala. Diisi Game setiap frame dari QuestLog.</summary>
    public QuestMarker Marker { get; set; }

    public Npc(NpcData data, RectangleF? wanderArea = null, Direction facing = Direction.Down) : base(data.Name)
    {
        Data = data;
        _wanderArea = wanderArea;
        _homeFacing = facing;
        Facing = facing;
    }

    // MVP 5: gelembung emosi di atas kepala (hati, senang, sedih, titik-titik)
    private const float EmoteSeconds = 1.8f;
    private string? _emote;
    private float _emoteTimer;

    protected override string SpritePrefix => "npc_" + Data.Id;

    /// <summary>Tampilkan emosi sebentar. key = "heart", "happy", "sad", "dots".</summary>
    public void ShowEmote(string key)
    {
        _emote = key;
        _emoteTimer = EmoteSeconds;
    }

    /// <summary>Sprite yang dipakai sebagai wajah di kotak dialog.</summary>
    private string Portrait => SpritePrefix + "_down_0";

    // ---------- Gerak ----------

    public override void Update(float deltaSeconds)
    {
        _markerBob += deltaSeconds;
        _emoteTimer = Math.Max(0, _emoteTimer - deltaSeconds);

        if (_talkPause > 0)
        {
            _talkPause -= deltaSeconds;
            Move(Vector2.Zero, 0, deltaSeconds);
            if (_talkPause <= 0 && _wanderArea == null)
                Facing = _homeFacing;   // selesai bicara: kembali menghadap semula
            return;
        }

        if (_wanderArea == null)
        {
            Move(Vector2.Zero, 0, deltaSeconds);
            return;
        }

        Wander(_wanderArea.Value, deltaSeconds);
    }

    /// <summary>Sesekali pilih: diam atau jalan ke salah satu dari 4 arah. Keluar area = balik ke tengah.</summary>
    private void Wander(RectangleF area, float deltaSeconds)
    {
        _decisionTimer -= deltaSeconds;
        if (_decisionTimer <= 0)
        {
            _walkDirection = PickRandomDirection();
            _decisionTimer = 1f + (float)Rng.NextDouble() * 2f;
        }

        if (!area.Contains(Center.X, Center.Y))
            _walkDirection = DirectionTowards(new Vector2(area.X + area.Width / 2, area.Y + area.Height / 2));

        Vector2 before = Position;
        Move(_walkDirection, WalkSpeed, deltaSeconds);

        if (_walkDirection != Vector2.Zero && Position == before)
            _decisionTimer = 0;   // menabrak sesuatu: pilih arah baru
    }

    private static Vector2 PickRandomDirection()
    {
        int choice = Rng.Next(6);   // 0..1 = diam (lebih sering diam), 2..5 = jalan
        if (choice == 2) return new Vector2(0, -1);
        if (choice == 3) return new Vector2(0, 1);
        if (choice == 4) return new Vector2(-1, 0);
        if (choice == 5) return new Vector2(1, 0);
        return Vector2.Zero;
    }

    private Vector2 DirectionTowards(Vector2 target)
    {
        Vector2 diff = target - Center;
        if (Math.Abs(diff.X) > Math.Abs(diff.Y))
            return new Vector2(Math.Sign(diff.X), 0);
        return new Vector2(0, Math.Sign(diff.Y));
    }

    private void FaceTowards(GameObject other)
    {
        Vector2 diff = other.Center - Center;
        if (Math.Abs(diff.X) > Math.Abs(diff.Y))
            Facing = diff.X > 0 ? Direction.Right : Direction.Left;
        else
            Facing = diff.Y > 0 ? Direction.Down : Direction.Up;
    }

    // ---------- Gambar ----------

    /// <summary>
    /// Tanda quest yang naik-turun di atas kepala. Dipanggil Renderer.RenderOverlays SETELAH cahaya,
    /// jadi tandanya tetap terang di malam hari.
    /// </summary>
    public void DrawMarker(Graphics g, IAssetProvider assets)
    {
        if (_emoteTimer > 0 && _emote != null)
        {
            // Muncul dengan efek "pop": membesar cepat lalu kembali normal
            float age = EmoteSeconds - _emoteTimer;
            float pop = age < 0.15f ? age / 0.15f * 1.25f : Math.Max(1f, 1.25f - (age - 0.15f) * 2.5f);
            float size = 14 * pop;
            RectangleF head = SpriteBounds;
            assets.DrawSprite(g, "emote_" + _emote, new RectangleF(head.X + 8 - size / 2, head.Y - 16 - (size - 14) / 2, size, size));
            return;
        }
        if (Marker == QuestMarker.None)
            return;

        float bob = MathF.Sin(_markerBob * 4f) * 1.5f;
        RectangleF body = SpriteBounds;
        var marker = new RectangleF(body.X + 3, body.Y - 15 + bob, 10, 14);
        assets.DrawSprite(g, Marker == QuestMarker.NewQuest ? "marker_new" : "marker_turnin", marker);
    }

    // ---------- Interaksi ----------

    public bool CanInteract(Player player) => true;

    /// <summary>MVP 5: "Beri hadiah" kalau memegang barang yang bisa diberikan, selain itu "Bicara".</summary>
    public string InteractLabel(Player player)
    {
        Item? held = player.Inventory.SelectedItem;
        bool canGift = (held is Food || held is Resource) && !player.Relationships.HasGiftedToday(Data.Id);
        return canGift ? "Beri hadiah" : "Bicara";
    }

    public ActionResult Interact(ActionContext context)
    {
        return TryQuestOrGift(context) ?? Talk(context);
    }

    /// <summary>
    /// Langkah 1-3 (quest & hadiah). Null kalau tidak ada yang terjadi.
    /// Public supaya meja toko bisa meneruskan ke penjualnya (tanpa hadiah, supaya tidak tertukar dengan belanja).
    /// </summary>
    public ActionResult? TryQuestOrGift(ActionContext context, bool allowGift = true)
    {
        StartConversation(context.User);
        IGameServices services = context.Services;
        Player player = context.User;

        // 1. Selesaikan quest
        QuestData? finished = services.Quests.FindQuestToTurnIn(Data.Id, player.Inventory);
        if (finished != null)
            return CompleteQuest(finished, context);

        // 2. Beri quest baru
        QuestData? newQuest = services.Quests.FindQuestToGive(Data.Id);
        if (newQuest != null)
        {
            GiveQuest(newQuest, services);
            return ActionResult.None();
        }

        // 3. Hadiah: hanya makanan & bahan, maksimal sekali sehari
        Item? held = player.Inventory.SelectedItem;
        if (allowGift && (held is Food || held is Resource) && !player.Relationships.HasGiftedToday(Data.Id))
            return ReceiveGift(held, context);

        return null;
    }

    private ActionResult CompleteQuest(QuestData quest, ActionContext context)
    {
        IGameServices services = context.Services;
        if (!services.Quests.CanReceiveReward(quest, context.User.Inventory, services.Items))
            return ActionResult.Fail("Tasmu penuh. Kosongkan satu slot untuk hadiahnya.");

        services.Quests.Complete(quest, context.User, services.Items);
        context.User.Relationships.RecordTalk(Data.Id);
        ShowEmote("heart");

        // Setelah cerita penutup: kabar quest selesai, lalu kalau ada quest berikutnya, langsung diberikan
        services.ShowDialogue(Name, quest.EndText, portrait: Portrait, onFinished: () =>
        {
            string gold = quest.Reward.Gold > 0 ? $" +{quest.Reward.Gold}g" : "";
            services.Notify($"Quest selesai: {quest.Title}!{gold}");
            foreach (QuestData auto in services.Quests.StartAutomaticQuests())
                services.Notify($"Quest baru: {auto.Title}");

            QuestData? next = services.Quests.FindQuestToGive(Data.Id);
            if (next != null)
                GiveQuest(next, services);
        });
        return ActionResult.None();
    }

    private void GiveQuest(QuestData quest, IGameServices services)
    {
        services.ShowDialogue(Name, quest.StartText, portrait: Portrait, onFinished: () =>
        {
            services.Quests.Start(quest);
            services.Notify($"Quest baru: {quest.Title} (J = jurnal)");
        });
    }

    private ActionResult ReceiveGift(Item gift, ActionContext context)
    {
        Player player = context.User;
        player.Inventory.RemoveFromSlot(player.Inventory.SelectedIndex, 1);

        GiftTaste taste = Data.TasteOf(gift);
        player.Relationships.RecordGift(Data.Id, taste);
        ShowEmote(taste switch { GiftTaste.Love => "heart", GiftTaste.Like => "happy", GiftTaste.Hate => "sad", _ => "dots" });

        string line = taste switch
        {
            GiftTaste.Love => $"Wah! {gift.Name}! Ini kesukaanku! Terima kasih banyak!",
            GiftTaste.Like => $"{gift.Name}? Terima kasih, aku suka.",
            GiftTaste.Hate => $"Hmm... {gift.Name}. Maaf, aku tidak suka ini.",
            _ => $"Oh, {gift.Name}. Terima kasih.",
        };
        context.Services.ShowDialogue(Name, new[] { line }, portrait: Portrait);
        return ActionResult.None();
    }

    /// <summary>Langkah 4: ngobrol biasa. Kalimat ganti tiap hari; kalau sudah akrab, kadang kalimat khusus.</summary>
    public ActionResult Talk(ActionContext context, string? line = null)
    {
        StartConversation(context.User);
        Relationships relationships = context.User.Relationships;
        relationships.RecordTalk(Data.Id);

        if (line == null)
        {
            List<string> lines = new(Data.DailyLines);
            if (relationships.HeartsWith(Data.Id) >= 3)
                lines.AddRange(Data.HeartLines);
            line = lines[(context.Services.Date.Day + _talkCount) % lines.Count];
            _talkCount++;
        }

        ShowEmote(relationships.HeartsWith(Data.Id) >= 2 ? "happy" : "dots");
        context.Services.ShowDialogue(Name, new[] { line }, portrait: Portrait);
        return ActionResult.None();
    }

    private void StartConversation(Player player)
    {
        FaceTowards(player);
        _talkPause = TalkPauseSeconds;
    }
}
