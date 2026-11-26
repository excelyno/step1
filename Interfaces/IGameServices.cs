namespace FarmGame.Interfaces;

/// <summary>
/// Layanan game yang boleh dipakai item dan objek dunia TANPA mereka kenal kelas Game.
/// Contoh: HoeDirt butuh membuat item hasil panen, Seed butuh tahu musim, pintu butuh minta tidur,
/// meja toko butuh tahu jam dan membuka menu toko.
/// Game yang mengimplementasikan interface ini.
/// </summary>
public interface IGameServices
{
    ItemRegistry Items { get; }
    GameDate Date { get; }

    /// <summary>MVP 2: jam sekarang (menit sejak 00:00), untuk jam buka toko.</summary>
    int TimeOfDay { get; }

    /// <summary>Minta hari diakhiri (tidur).</summary>
    void RequestSleep();

    /// <summary>MVP 2: buka menu toko.</summary>
    void OpenShop(Shop shop);

    /// <summary>Buka menu kotak kirim (tas & isi kotak, drag & drop).</summary>
    void OpenShippingBin(ShippingBin bin);

    /// <summary>MVP 3: pindahkan pemain ke lokasi lain (pintu rumah).</summary>
    void WarpTo(string locationName, Point tile);

    /// <summary>MVP 3: mulai memancing di tile air ini.</summary>
    void StartFishing(Point waterTile);

    // ---------- MVP 4: cerita & efek ----------

    /// <summary>Buku quest (warga bertanya: punya quest? quest selesai?).</summary>
    QuestLog Quests { get; }

    /// <summary>
    /// Tampilkan kotak dialog beberapa halaman. onFinished dipanggil setelah halaman terakhir.
    /// portrait = nama sprite wajah (boleh null).
    /// </summary>
    void ShowDialogue(string speaker, IReadOnlyList<string> pages, Action? onFinished = null, string? portrait = null);

    /// <summary>Pesan singkat di kiri bawah (toast).</summary>
    void Notify(string message, bool success = true);

    /// <summary>MVP 8: alat habis daya tahannya — hancur dan hilang dari tas pemain.</summary>
    void BreakTool(Tool tool);

    /// <summary>Efek partikel di posisi dunia (serpihan kayu, percikan air, ...).</summary>
    void SpawnEffect(EffectKind kind, Vector2 worldPosition);

    // ---------- MVP 5: umpan balik & surat ----------

    /// <summary>Ikon barang + "+6 Kayu" yang melayang naik dari posisi dunia ini.</summary>
    void ShowPickup(Item item, int amount, Vector2 worldPosition);

    /// <summary>Teks melayang biasa (contoh "+10 energi", "-1g").</summary>
    void ShowFloatingText(string text, Vector2 worldPosition, Color color);

    /// <summary>Layar bergetar sebentar (pohon tumbang).</summary>
    void ShakeCamera(float strength, float seconds);

    /// <summary>Putar efek suara ("hoe", "coin", "pop", ...). Aman jika suara tidak ada.</summary>
    void PlaySound(string name);

    /// <summary>Kotak surat pemain.</summary>
    MailSystem Mail { get; }

    /// <summary>Jam bertambah (duduk di bangku).</summary>
    void PassTime(int minutes);

    // ---------- Minigame bercocok tanam ----------

    /// <summary>Bintang & tingkat petani (Buku Tani).</summary>
    LessonBook Lessons { get; }

    /// <summary>
    /// Buka minigame panen tanaman ini. True = minigame terbuka dan `apply` dipanggil setelah selesai.
    /// False = tanaman tidak punya minigame / sudah pernah lulus, jadi langsung panen seperti biasa.
    /// </summary>
    bool RunHarvestLesson(CropData crop, Action apply);

    /// <summary>Buka minigame menangkap hama. Sama seperti di atas, tapi event ini selalu muncul.</summary>
    bool RunPestLesson(Action apply);
}
