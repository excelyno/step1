using System.Text.Json;
using System.Text.Json.Serialization;

namespace FarmGame.Data;

/// <summary>
/// Membaca file JSON berisi daftar data (crops.json, fish.json) dengan aturan yang SAMA:
/// boleh ada komentar //, koma di akhir, nama properti tidak peka huruf besar/kecil, musim ditulis "Spring".
/// Semua masalah file diubah jadi GameDataException yang mudah dibaca.
/// </summary>
public static class JsonDataFile
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,                 // "daysToMature" = DaysToMature
            ReadCommentHandling = JsonCommentHandling.Skip,     // boleh ada komentar //
            AllowTrailingCommas = true,
        };
        options.Converters.Add(new JsonStringEnumConverter()); // "Spring" -> Season.Spring
        return options;
    }

    public static List<T> ReadList<T>(string path)
    {
        if (!File.Exists(path))
            throw new GameDataException("File data tidak ditemukan: " + path);

        string fileName = Path.GetFileName(path);
        List<T>? list;
        try
        {
            list = JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), Options);
        }
        catch (JsonException ex)
        {
            throw new GameDataException($"{fileName}: format JSON salah ({ex.Message}).");
        }

        if (list == null || list.Count == 0)
            throw new GameDataException(fileName + ": tidak ada data.");
        return list;
    }
}
