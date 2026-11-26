namespace FarmGame.Data;

/// <summary>Kumpulan semua data ikan dari fish.json, plus cara memilih ikan yang menggigit kail.</summary>
public class FishDatabase
{
    public const int MinDifficulty = 1;
    public const int MaxDifficulty = 5;

    private readonly List<FishData> _fish = new();

    public IReadOnlyList<FishData> All => _fish;

    public FishDatabase(string path)
    {
        string fileName = Path.GetFileName(path);

        foreach (FishData fish in JsonDataFile.ReadList<FishData>(path))
        {
            if (fish.Difficulty < MinDifficulty || fish.Difficulty > MaxDifficulty)
                throw new GameDataException($"{fileName}: '{fish.Id}' difficulty harus {MinDifficulty}..{MaxDifficulty}.");
            if (fish.Seasons.Count == 0)
                throw new GameDataException($"{fileName}: '{fish.Id}' belum punya musim.");
            if (_fish.Any(other => other.Id == fish.Id))
                throw new GameDataException($"{fileName}: id ikan '{fish.Id}' ganda.");

            _fish.Add(fish);
        }
    }

    /// <summary>
    /// Pilih ikan acak yang ada di musim ini. Ikan susah lebih jarang menggigit:
    /// peluang = (6 - difficulty), jadi difficulty 1 lima kali lebih sering dari difficulty 5.
    /// Null kalau tidak ada ikan di musim ini.
    /// </summary>
    public FishData? PickRandom(Season season, Random rng)
    {
        List<FishData> available = _fish.Where(fish => fish.IsAvailableIn(season)).ToList();
        if (available.Count == 0)
            return null;

        int totalWeight = available.Sum(Weight);
        int roll = rng.Next(totalWeight);
        foreach (FishData fish in available)
        {
            roll -= Weight(fish);
            if (roll < 0)
                return fish;
        }
        return available[^1];
    }

    private static int Weight(FishData fish) => MaxDifficulty + 1 - fish.Difficulty;
}
