namespace FarmGame.Data;

/// <summary>Kumpulan data warga dari npcs.json.</summary>
public class NpcDatabase
{
    private readonly Dictionary<string, NpcData> _npcs = new();

    public IReadOnlyCollection<NpcData> All => _npcs.Values;

    public NpcDatabase(string path)
    {
        string fileName = Path.GetFileName(path);
        foreach (NpcData npc in JsonDataFile.ReadList<NpcData>(path))
        {
            if (npc.DailyLines.Count == 0)
                throw new GameDataException($"{fileName}: '{npc.Id}' belum punya dailyLines.");
            if (_npcs.ContainsKey(npc.Id))
                throw new GameDataException($"{fileName}: id warga '{npc.Id}' ganda.");
            _npcs.Add(npc.Id, npc);
        }
    }

    public bool Contains(string id) => _npcs.ContainsKey(id);

    public NpcData Get(string id)
    {
        if (!_npcs.ContainsKey(id))
            throw new GameDataException($"Warga '{id}' tidak ada di npcs.json.");
        return _npcs[id];
    }
}
