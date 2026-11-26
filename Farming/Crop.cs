namespace FarmGame.Farming;

/// <summary>
/// Satu tanaman yang sedang tumbuh. BUKAN GameObject: ia tinggal di dalam HoeDirt (komposisi),
/// jadi tidak punya posisi sendiri.
/// Tahap gambar: 0 = bibit, 1..3 = tumbuh, 4 = siap panen.
/// </summary>
public class Crop
{
    public const int StageCount = 5;

    public CropData Data { get; }
    public int DaysGrown { get; private set; }
    public bool IsDead { get; private set; }

    public bool IsReady => !IsDead && DaysGrown >= Data.DaysToMature;

    public Crop(CropData data)
    {
        Data = data;
    }

    /// <summary>MVP 5: tanaman dari file simpanan (sudah tumbuh beberapa hari).</summary>
    public Crop(CropData data, int daysGrown, bool isDead) : this(data)
    {
        DaysGrown = daysGrown;
        IsDead = isDead;
    }

    /// <summary>Tahap gambar 0..4 berdasarkan berapa hari sudah tumbuh.</summary>
    public int Stage
    {
        get
        {
            if (IsReady) return StageCount - 1;   // 4 = siap panen
            if (DaysGrown == 0) return 0;         // 0 = baru ditanam

            // Hari tumbuh dibagi rata ke tahap 1, 2, 3
            int span = Math.Max(1, Data.DaysToMature - 1);
            return 1 + (DaysGrown - 1) * 3 / span;
        }
    }

    /// <summary>Contoh "crop_parsnip_2" atau "crop_dead".</summary>
    public string SpriteKey => IsDead ? "crop_dead" : $"crop_{Data.Id}_{Stage}";

    /// <summary>Dipanggil HoeDirt tiap pagi. Tumbuh 1 hari kalau kemarin disiram.</summary>
    public void AdvanceDay(bool wasWatered, Season season)
    {
        if (IsDead)
            return;

        if (!Data.CanGrowIn(season))
        {
            IsDead = true;   // ganti ke musim yang tidak cocok -> mati
            return;
        }

        if (wasWatered && !IsReady)
            DaysGrown++;
    }

    /// <summary>
    /// Setelah dipanen: tanaman berbuah lagi (tomat, kale, jagung) mundur beberapa hari,
    /// atau hilang sama sekali (kentang, parsnip). HoeDirt yang memutuskan mana yang dipakai.
    /// </summary>
    public void AfterHarvest()
    {
        DaysGrown = Math.Max(0, DaysGrown - Data.RegrowDays);
    }
}
