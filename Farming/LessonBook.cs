namespace FarmGame.Farming;

/// <summary>
/// Catatan bintang pemain untuk setiap minigame (Buku Tani).
/// 0 bintang tetap dicatat = "sudah pernah dilihat", supaya tutorial tidak muncul terus.
/// </summary>
public class LessonBook
{
    private readonly Dictionary<string, int> _stars = new();

    public IReadOnlyDictionary<string, int> Stars => _stars;

    public int TotalStars => _stars.Values.Sum();

    public int MaxStars => Math.Max(1, _stars.Count) * 3;

    /// <summary>0 = Tunas, 1 = Bibit, 2 = Muda, 3 = Ahli. Mulai Muda, panen dapat +1 buah.</summary>
    public int Grade
    {
        get
        {
            int total = TotalStars;
            if (total >= 13) return 3;
            if (total >= 10) return 2;
            if (total >= 6) return 1;
            return 0;
        }
    }

    public string GradeName => Grade switch
    {
        3 => "Ahli Tani",
        2 => "Petani Muda",
        1 => "Bibit Harapan",
        _ => "Tunas Baru"
    };

    public int StarsFor(string lessonId) => _stars.GetValueOrDefault(lessonId);

    public bool HasSeen(string lessonId) => _stars.ContainsKey(lessonId);

    /// <summary>Tutorial muncul kalau belum pernah lulus/dilewati. Event (hama) selalu muncul.</summary>
    public bool ShouldPlay(CropLesson lesson) => !lesson.TutorialOnce || !HasSeen(lesson.Id);

    /// <summary>Simpan bintang. Bintang lama tidak pernah turun (latihan tidak menghapus hasil terbaik).</summary>
    public void Record(string lessonId, int stars)
    {
        int clamped = Math.Clamp(stars, 0, 3);
        if (_stars.TryGetValue(lessonId, out int old))
            _stars[lessonId] = Math.Max(old, clamped);
        else
            _stars[lessonId] = clamped;
    }

    public void Restore(Dictionary<string, int>? stars)
    {
        _stars.Clear();
        if (stars == null)
            return;
        foreach (var (id, value) in stars)
            _stars[id] = Math.Clamp(value, 0, 3);
    }

    public Dictionary<string, int> ToSave() => new(_stars);
}
