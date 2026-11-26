namespace FarmGame.Farming;

/// <summary>
/// Kumpulan minigame dari Content/Data/lessons.json. Dibaca SEKALI saat game mulai.
/// Dicek saat memuat: id ganda, tanaman yang tidak ada, dan tanaman yang punya dua pelajaran.
/// </summary>
public class LessonDatabase
{
    private readonly Dictionary<string, CropLesson> _byCrop = new();
    private readonly List<CropLesson> _all = new();

    public IReadOnlyList<CropLesson> All => _all;

    /// <summary>Minigame event hama (cropId kosong). Null kalau tidak ada.</summary>
    public CropLesson? Pest { get; }

    public LessonDatabase(string path, CropDatabase crops)
    {
        string fileName = Path.GetFileName(path);

        foreach (CropLesson lesson in JsonDataFile.ReadList<CropLesson>(path))
        {
            if (lesson.Id.Length == 0)
                throw new GameDataException($"{fileName}: ada minigame tanpa id.");
            if (_all.Any(other => other.Id == lesson.Id))
                throw new GameDataException($"{fileName}: id minigame '{lesson.Id}' ganda.");

            _all.Add(lesson);

            if (lesson.IsEvent)
            {
                if (lesson.Id != "pest_catch")
                    throw new GameDataException($"{fileName}: event '{lesson.Id}' harus bernama 'pest_catch'.");
                Pest = lesson;
                continue;
            }

            if (!crops.All.Any(crop => crop.Id == lesson.CropId))
                throw new GameDataException($"{fileName}: minigame '{lesson.Id}' memakai tanaman '{lesson.CropId}' yang tidak ada di crops.json.");
            if (_byCrop.ContainsKey(lesson.CropId))
                throw new GameDataException($"{fileName}: tanaman '{lesson.CropId}' punya lebih dari satu minigame.");

            _byCrop[lesson.CropId] = lesson;
        }
    }

    /// <summary>Minigame panen untuk tanaman ini, atau null kalau tidak punya (langsung panen biasa).</summary>
    public CropLesson? ForCrop(string cropId) => _byCrop.GetValueOrDefault(cropId);
}
