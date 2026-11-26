namespace FarmGame.Social;

/// <summary>
/// Pertemanan pemain dengan setiap warga. 100 poin = 1 hati, maksimal 10 hati.
/// Bicara pertama setiap hari +10 poin. Hadiah maksimal 1x per warga per hari.
/// </summary>
public class Relationships
{
    public const int PointsPerHeart = 100;
    public const int MaxHearts = 10;
    public const int TalkPoints = 10;

    private readonly Dictionary<string, int> _points = new();
    private readonly HashSet<string> _talkedToday = new();
    private readonly HashSet<string> _giftedToday = new();

    public int PointsWith(string npcId) => _points.GetValueOrDefault(npcId);

    public int HeartsWith(string npcId) => PointsWith(npcId) / PointsPerHeart;

    public bool HasTalkedToday(string npcId) => _talkedToday.Contains(npcId);

    public bool HasGiftedToday(string npcId) => _giftedToday.Contains(npcId);

    /// <summary>MVP 5: semua poin (untuk file simpanan).</summary>
    public IReadOnlyDictionary<string, int> AllPoints => _points;
    public IReadOnlyCollection<string> TalkedToday => _talkedToday;
    public IReadOnlyCollection<string> GiftedToday => _giftedToday;

    /// <summary>MVP 5: kembalikan keadaan dari file simpanan.</summary>
    public void Restore(IReadOnlyDictionary<string, int> points, IEnumerable<string> talked, IEnumerable<string> gifted)
    {
        _points.Clear();
        foreach (var pair in points)
            _points[pair.Key] = pair.Value;
        _talkedToday.Clear();
        _talkedToday.UnionWith(talked);
        _giftedToday.Clear();
        _giftedToday.UnionWith(gifted);
    }

    /// <summary>Tambah (atau kurangi kalau negatif) poin, selalu di antara 0 dan maksimal.</summary>
    public void AddPoints(string npcId, int amount)
    {
        int max = PointsPerHeart * MaxHearts;
        _points[npcId] = Math.Clamp(PointsWith(npcId) + amount, 0, max);
    }

    /// <summary>Catat bicara. Hanya yang pertama setiap hari yang memberi poin.</summary>
    public void RecordTalk(string npcId)
    {
        if (_talkedToday.Add(npcId))
            AddPoints(npcId, TalkPoints);
    }

    /// <summary>Catat hadiah dan beri poin sesuai selera warga.</summary>
    public int RecordGift(string npcId, GiftTaste taste)
    {
        _giftedToday.Add(npcId);
        _talkedToday.Add(npcId);   // memberi hadiah juga dihitung sudah menyapa
        int points = PointsForGift(taste);
        AddPoints(npcId, points);
        return points;
    }

    public static int PointsForGift(GiftTaste taste)
    {
        if (taste == GiftTaste.Love) return 80;
        if (taste == GiftTaste.Like) return 45;
        if (taste == GiftTaste.Hate) return -40;
        return 20;
    }

    /// <summary>Pagi hari: boleh menyapa dan memberi hadiah lagi.</summary>
    public void OnNewDay()
    {
        _talkedToday.Clear();
        _giftedToday.Clear();
    }
}
