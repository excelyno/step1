namespace FarmGame.Quests;

/// <summary>Data satu quest dari Content/Data/quests.json. Quest baru = objek JSON baru, tanpa kode baru.</summary>
public class QuestData
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Giver { get; set; } = "";               // "" = mulai otomatis
    public List<string> Requires { get; set; } = new();  // quest yang harus selesai dulu
    public QuestObjective Objective { get; set; } = new();
    public List<string> StartText { get; set; } = new(); // diucapkan pemberi saat memberi quest
    public List<string> EndText { get; set; } = new();   // diucapkan saat quest selesai
    public QuestReward Reward { get; set; } = new();

    public bool IsAutomatic => Giver == "";
}

/// <summary>
/// Tujuan quest. Type "talk" = bicara dengan Npc. Type "bring" = bawakan Amount buah Item ke Npc.
/// Item boleh berupa id ("parsnip") atau kategori ("category:fish").
/// </summary>
public class QuestObjective
{
    public const string Talk = "talk";
    public const string Bring = "bring";

    public string Type { get; set; } = "";
    public string Npc { get; set; } = "";
    public string Item { get; set; } = "";
    public int Amount { get; set; }
}

public class QuestReward
{
    public int Gold { get; set; }
    public List<QuestItemReward> Items { get; set; } = new();
    public int Friendship { get; set; }   // poin pertemanan dengan warga tujuan
}

public class QuestItemReward
{
    public string Id { get; set; } = "";
    public int Amount { get; set; }
}
