namespace FarmGame.Social;

/// <summary>
/// Kotak surat (MVP 5). Setiap pagi, surat yang sudah waktunya masuk ke antrean "belum dibaca".
/// Kotak surat di kebun menampilkan bendera merah kalau ada surat; X = baca surat paling lama.
/// </summary>
public class MailSystem
{
    private readonly List<Letter> _all;
    private readonly List<string> _unread = new();
    private readonly HashSet<string> _read = new();

    public MailSystem(string path)
    {
        _all = JsonDataFile.ReadList<Letter>(path);
    }

    public bool HasUnread => _unread.Count > 0;
    public IReadOnlyCollection<string> ReadIds => _read;
    public IReadOnlyList<string> UnreadIds => _unread;

    /// <summary>Hari ke berapa sejak mulai (1 = hari pertama). 28 hari per musim, 4 musim per tahun.</summary>
    public static int DayNumber(GameDate date) =>
        (date.Year - 1) * 4 * GameDate.DaysPerSeason + (int)date.Season * GameDate.DaysPerSeason + date.Day;

    /// <summary>Masukkan surat yang sudah waktunya (dan belum pernah datang).</summary>
    public void Deliver(GameDate date)
    {
        int today = DayNumber(date);
        foreach (Letter letter in _all)
        {
            if (letter.Day <= today && !_read.Contains(letter.Id) && !_unread.Contains(letter.Id))
                _unread.Add(letter.Id);
        }
    }

    /// <summary>Ambil surat paling lama yang belum dibaca dan tandai sudah dibaca.</summary>
    public Letter? TakeNext()
    {
        if (_unread.Count == 0)
            return null;
        string id = _unread[0];
        _unread.RemoveAt(0);
        _read.Add(id);
        return _all.First(letter => letter.Id == id);
    }

    /// <summary>Dipakai saat memuat simpanan.</summary>
    public void Restore(IEnumerable<string> read, IEnumerable<string> unread)
    {
        _read.Clear();
        _unread.Clear();
        _read.UnionWith(read);
        _unread.AddRange(unread.Where(id => _all.Any(letter => letter.Id == id)));
    }
}
