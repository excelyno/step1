namespace FarmGame.Quests;

/// <summary>Tanda di atas kepala warga.</summary>
public enum QuestMarker
{
    None,
    NewQuest,   // "!" kuning: ada quest baru
    TurnIn      // "?" : quest bisa diselesaikan di warga ini
}

/// <summary>
/// Buku catatan quest: quest mana yang aktif, mana yang selesai, dan siapa yang punya quest.
/// Semua aturan quest ada di sini; warga (Npc) hanya bertanya ke QuestLog.
/// </summary>
public class QuestLog
{
    private readonly List<QuestData> _all;
    private readonly List<QuestData> _active = new();
    private readonly HashSet<string> _completed = new();

    public IReadOnlyList<QuestData> Active => _active;
    public int CompletedCount => _completed.Count;
    public int TotalCount => _all.Count;

    public QuestLog(string path)
    {
        _all = JsonDataFile.ReadList<QuestData>(path);
    }

    public bool IsCompleted(string questId) => _completed.Contains(questId);

    /// <summary>MVP 5: id quest yang sudah selesai (untuk file simpanan).</summary>
    public IReadOnlyCollection<string> CompletedIds => _completed;

    /// <summary>MVP 5: kembalikan keadaan dari file simpanan. Id yang tidak dikenal diabaikan.</summary>
    public void Restore(IEnumerable<string> activeIds, IEnumerable<string> completedIds)
    {
        _active.Clear();
        _completed.Clear();
        _completed.UnionWith(completedIds.Where(id => _all.Any(q => q.Id == id)));
        foreach (string id in activeIds)
        {
            QuestData? quest = _all.FirstOrDefault(q => q.Id == id);
            if (quest != null && !_completed.Contains(id))
                _active.Add(quest);
        }
    }

    /// <summary>Cek isi quests.json sekali saat game mulai, supaya salah ketik langsung ketahuan.</summary>
    public void Validate(NpcDatabase npcs, ItemRegistry items)
    {
        HashSet<string> ids = new();
        foreach (QuestData quest in _all)
        {
            string where = $"quests.json '{quest.Id}'";
            if (!ids.Add(quest.Id))
                throw new GameDataException($"{where}: id ganda.");
            if (!quest.IsAutomatic && !npcs.Contains(quest.Giver))
                throw new GameDataException($"{where}: giver '{quest.Giver}' tidak ada.");
            if (!npcs.Contains(quest.Objective.Npc))
                throw new GameDataException($"{where}: objective.npc '{quest.Objective.Npc}' tidak ada.");

            if (quest.Objective.Type == QuestObjective.Bring)
            {
                if (quest.Objective.Amount <= 0)
                    throw new GameDataException($"{where}: amount harus lebih dari 0.");
                if (!quest.Objective.Item.StartsWith("category:"))
                    items.Create(quest.Objective.Item);   // error kalau item tidak ada
            }
            else if (quest.Objective.Type != QuestObjective.Talk)
            {
                throw new GameDataException($"{where}: objective.type harus 'talk' atau 'bring'.");
            }

            foreach (QuestItemReward reward in quest.Reward.Items)
                items.Create(reward.Id);
        }

        foreach (QuestData quest in _all)
        {
            foreach (string required in quest.Requires)
            {
                if (!ids.Contains(required))
                    throw new GameDataException($"quests.json '{quest.Id}': requires '{required}' tidak ada.");
            }
        }
    }

    // ---------- Pertanyaan ----------

    /// <summary>Quest yang siap diberikan warga ini (belum mulai, syarat sudah selesai). Null kalau tidak ada.</summary>
    public QuestData? FindQuestToGive(string npcId)
    {
        return _all.FirstOrDefault(quest => quest.Giver == npcId && IsAvailable(quest));
    }

    /// <summary>Quest aktif yang tujuannya warga ini DAN syaratnya sudah terpenuhi. Null kalau tidak ada.</summary>
    public QuestData? FindQuestToTurnIn(string npcId, Inventory inventory)
    {
        return _active.FirstOrDefault(quest => quest.Objective.Npc == npcId && IsObjectiveMet(quest, inventory));
    }

    public QuestMarker MarkerFor(string npcId, Inventory inventory)
    {
        if (FindQuestToTurnIn(npcId, inventory) != null) return QuestMarker.TurnIn;
        if (FindQuestToGive(npcId) != null) return QuestMarker.NewQuest;
        return QuestMarker.None;
    }

    public bool IsObjectiveMet(QuestData quest, Inventory inventory)
    {
        if (quest.Objective.Type == QuestObjective.Talk)
            return true;   // cukup datang dan bicara
        return inventory.CountMatching(quest.Objective.Item) >= quest.Objective.Amount;
    }

    /// <summary>Teks kemajuan untuk HUD dan jurnal, contoh "Parsnip 3/5" atau "Bicara dengan Raka".</summary>
    public string ProgressText(QuestData quest, Inventory inventory, NpcDatabase npcs, ItemRegistry items)
    {
        string npcName = npcs.Get(quest.Objective.Npc).Name;
        if (quest.Objective.Type == QuestObjective.Talk)
            return $"Bicara dengan {npcName}";

        int have = Math.Min(inventory.CountMatching(quest.Objective.Item), quest.Objective.Amount);
        return $"{ItemLabel(quest.Objective.Item, items)} {have}/{quest.Objective.Amount} -> {npcName}";
    }

    private static string ItemLabel(string pattern, ItemRegistry items)
    {
        if (pattern == "category:fish") return "Ikan apa saja";
        if (pattern == "category:produce") return "Hasil panen apa saja";
        if (pattern.StartsWith("category:")) return pattern["category:".Length..];
        return items.Create(pattern).Name;
    }

    private bool IsAvailable(QuestData quest)
    {
        return !_completed.Contains(quest.Id)
            && !_active.Contains(quest)
            && quest.Requires.All(_completed.Contains);
    }

    // ---------- Perubahan ----------

    public void Start(QuestData quest)
    {
        if (!_active.Contains(quest))
            _active.Add(quest);
    }

    /// <summary>Mulai semua quest otomatis yang syaratnya sudah selesai. Mengembalikan quest yang baru mulai.</summary>
    public List<QuestData> StartAutomaticQuests()
    {
        List<QuestData> started = _all.Where(quest => quest.IsAutomatic && IsAvailable(quest)).ToList();
        foreach (QuestData quest in started)
            Start(quest);
        return started;
    }

    /// <summary>Apakah hadiah barang quest ini muat di tas?</summary>
    public bool CanReceiveReward(QuestData quest, Inventory inventory, ItemRegistry items)
    {
        // Cek kasar per hadiah. Barang quest yang diserahkan dihitung belum keluar, jadi aman.
        return quest.Reward.Items.All(reward => inventory.CanFit(items.Create(reward.Id), reward.Amount));
    }

    /// <summary>Selesaikan quest: ambil barang, beri hadiah, tandai selesai. Cek CanReceiveReward dulu.</summary>
    public void Complete(QuestData quest, Player player, ItemRegistry items)
    {
        if (quest.Objective.Type == QuestObjective.Bring)
            player.Inventory.RemoveMatching(quest.Objective.Item, quest.Objective.Amount);

        if (quest.Reward.Gold > 0)
            player.Wallet.Earn(quest.Reward.Gold);
        foreach (QuestItemReward reward in quest.Reward.Items)
            player.Inventory.TryAdd(items.Create(reward.Id), reward.Amount);
        player.Relationships.AddPoints(quest.Objective.Npc, quest.Reward.Friendship);
        if (player.Location is IGameServices services)
            services.PlaySound("coin");

        _active.Remove(quest);
        _completed.Add(quest.Id);
    }
}
