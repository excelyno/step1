namespace FarmGame.Social;

/// <summary>Satu surat dari mail.json. Datang pada hari tertentu (hitungan hari sejak mulai, hari 1 = hari pertama).</summary>
public class Letter
{
    public string Id { get; set; } = "";
    public int Day { get; set; }
    public string From { get; set; } = "";
    public List<string> Pages { get; set; } = new();
    public List<QuestItemReward> Items { get; set; } = new();
    public int Gold { get; set; }
}
