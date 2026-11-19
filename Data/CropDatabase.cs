using System.Text.Json;
using System.Text.Json.Serialization;

namespace FarmGame.Data;

/// <summary>Kumpulan semua data tanaman. Dibaca SEKALI dari crops.json saat game mulai.</summary>
public class CropDatabase
{
    private readonly Dictionary<string, CropData> _crops = new();

    /// <summary>Semua tanaman (hanya bisa dibaca).</summary>
    public IReadOnlyCollection<CropData> All => _crops.Values;

    public CropDatabase(string path)
    {
        if (!File.Exists(path))
            throw new GameDataException("File data tidak ditemukan: " + path);

        string fileName = Path.GetFileName(path);
        List<CropData>? list;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,                 // "daysToMature" = DaysToMature
                ReadCommentHandling = JsonCommentHandling.Skip,     // boleh ada komentar //
                AllowTrailingCommas = true,
            };
            options.Converters.Add(new JsonStringEnumConverter()); // "Spring" -> Season.Spring

            list = JsonSerializer.Deserialize<List<CropData>>(File.ReadAllText(path), options);
        }
        catch (JsonException ex)
        {
            throw new GameDataException($"{fileName}: format JSON salah ({ex.Message}).");
        }

        if (list == null || list.Count == 0)
            throw new GameDataException(fileName + ": tidak ada data tanaman.");

        // Validasi setiap tanaman
        foreach (CropData crop in list)
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
