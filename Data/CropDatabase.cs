namespace FarmGame.Data;

/// <summary>Kumpulan semua data tanaman. Dibaca SEKALI dari crops.json saat game mulai.</summary>
public class CropDatabase
{
    private readonly Dictionary<string, CropData> _crops = new();

    /// <summary>Semua tanaman (hanya bisa dibaca).</summary>
    public IReadOnlyCollection<CropData> All => _crops.Values;

    public CropDatabase(string path)
    {
        string fileName = Path.GetFileName(path);

        // Validasi setiap tanaman
        foreach (CropData crop in JsonDataFile.ReadList<CropData>(path))
        {
            if (crop.DaysToMature <= 0)
                throw new GameDataException($"{fileName}: '{crop.Id}' daysToMature harus lebih dari 0.");
            if (crop.Seasons.Count == 0)
                throw new GameDataException($"{fileName}: '{crop.Id}' belum punya musim tanam.");
            if (_crops.ContainsKey(crop.Id))
                throw new GameDataException($"{fileName}: id tanaman '{crop.Id}' ganda.");

            _crops.Add(crop.Id, crop);
        }
    }

    public CropData Get(string id)
    {
        if (!_crops.ContainsKey(id))
            throw new GameDataException($"Tanaman '{id}' tidak ada di crops.json.");
        return _crops[id];
    }
}
