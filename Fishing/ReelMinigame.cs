namespace FarmGame.Fishing;

/// <summary>
/// Minigame menarik ikan (logika saja, tanpa gambar):
/// sebuah jarum berayun kiri-kanan di sepanjang bar 0..1. Ada zona hijau di posisi acak.
/// Tekan tombol saat jarum di dalam zona = KENA (zona pindah). Di luar zona = MELESET.
/// Kena 3x = ikan tertangkap. Meleset 3x = senar putus.
/// Ikan makin susah = jarum makin cepat, zona makin sempit.
/// </summary>
public class ReelMinigame
{
    public const int HitsToWin = 3;
    public const int MissesToLose = 3;

    private readonly Random _rng;
    private float _direction = 1f;   // +1 ke kanan, -1 ke kiri

    public float NeedleSpeed { get; }   // panjang bar per detik
    public float ZoneWidth { get; }     // lebar zona (0..1)
    public float NeedlePosition { get; private set; }   // 0 = ujung kiri, 1 = ujung kanan
    public float ZoneStart { get; private set; }
    public int Hits { get; private set; }
    public int Misses { get; private set; }

    /// <summary>Sisa waktu kedip setelah menekan (untuk efek gambar). Positif = baru saja kena / meleset.</summary>
    public float FlashTimer { get; private set; }
    public bool LastPressWasHit { get; private set; }

    public bool IsWon => Hits >= HitsToWin;
    public bool IsLost => Misses >= MissesToLose;
    public bool IsOver => IsWon || IsLost;

    public ReelMinigame(int difficulty, Random rng)
    {
        _rng = rng;
        NeedleSpeed = 0.55f + 0.22f * difficulty;   // difficulty 1 = 0,77 ... 5 = 1,65
        ZoneWidth = 0.30f - 0.04f * difficulty;     // difficulty 1 = 0,26 ... 5 = 0,10
        MoveZone();
    }

    public void Update(float deltaSeconds)
    {
        if (IsOver)
            return;

        FlashTimer = Math.Max(0, FlashTimer - deltaSeconds);

        // Jarum memantul di ujung bar
        NeedlePosition += _direction * NeedleSpeed * deltaSeconds;
        if (NeedlePosition >= 1f) { NeedlePosition = 1f; _direction = -1f; }
        if (NeedlePosition <= 0f) { NeedlePosition = 0f; _direction = 1f; }
    }

    /// <summary>Pemain menekan tombol tarik. True kalau kena.</summary>
    public bool Press()
    {
        if (IsOver)
            return false;

        LastPressWasHit = NeedlePosition >= ZoneStart && NeedlePosition <= ZoneStart + ZoneWidth;
        FlashTimer = 0.25f;

        if (LastPressWasHit)
        {
            Hits++;
            MoveZone();
        }
        else
        {
            Misses++;
        }
        return LastPressWasHit;
    }

    /// <summary>Taruh zona di tempat acak yang jauh dari jarum, supaya tidak "kena gratis".</summary>
    private void MoveZone()
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            ZoneStart = (float)_rng.NextDouble() * (1f - ZoneWidth);
            float zoneCenter = ZoneStart + ZoneWidth / 2;
            if (Math.Abs(zoneCenter - NeedlePosition) > 0.25f)
                return;
        }
    }
}
