namespace FarmGame.Saving;

/// <summary>
/// Isi file simpanan (MVP 5). Hanya data polos (angka, teks, daftar) supaya mudah diubah jadi JSON
/// dan mudah dibaca manusia. Objek game TIDAK disimpan langsung; SaveSystem yang menerjemahkan.
/// </summary>
public class SaveData
{
    public int Version { get; set; } = 6;   // 2 = kotak kirim per slot, 3 = bintang, 4 = seed hutan, 5 = daya tahan alat, 6 = upgrade tas
    public string SavedAt { get; set; } = "";

    // Seed dunia (bentuk hutan prosedural). 0 = simpanan lama sebelum MVP 7.
    public int WorldSeed { get; set; }

    // Waktu
    public int Year { get; set; } = 1;
    public string Season { get; set; } = "Spring";
    public int Day { get; set; } = 1;
    public int TimeOfDay { get; set; }

    // Pemain
    public string Location { get; set; } = "";
    public int TileX { get; set; }
    public int TileY { get; set; }
    public string Facing { get; set; } = "Down";
    public int Gold { get; set; }
    public int Stamina { get; set; }
    public int SelectedSlot { get; set; }
    public int BackpackLevel { get; set; }        // MVP 9: level tas (0 = standar 12 slot)
    public List<SlotSave> Inventory { get; set; } = new();

    // Warga, quest, surat
    public Dictionary<string, int> Friendship { get; set; } = new();
    public List<string> TalkedToday { get; set; } = new();
    public List<string> GiftedToday { get; set; } = new();
    public List<string> ActiveQuests { get; set; } = new();
    public List<string> CompletedQuests { get; set; } = new();
    public List<string> ReadMail { get; set; } = new();
    public List<string> UnreadMail { get; set; } = new();

    // Kotak kirim dan dunia
    public List<SlotSave> ShippingBin { get; set; } = new();
    public Dictionary<string, List<ObjectSave>> Locations { get; set; } = new();

    // Minigame bercocok tanam: id minigame -> bintang (0..3)
    public Dictionary<string, int> LessonStars { get; set; } = new();
}

/// <summary>Satu slot tas / satu tumpukan di kotak kirim.</summary>
public class SlotSave
{
    public int Slot { get; set; }
    public string Item { get; set; } = "";
    public int Quantity { get; set; }
    public int Water { get; set; } = -1;           // isi penyiram (-1 = bukan penyiram)
    public int Durability { get; set; } = -1;      // sisa daya tahan alat (MVP 8; -1 = bukan alat / penuh)
}
