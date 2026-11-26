using System.Text.Json;

namespace FarmGame.Saving;

/// <summary>
/// Simpan & muat permainan (MVP 5). File: %APPDATA%/TandurFarm/save.json.
///
/// Cara kerja dunia:
///   - Simpan: setiap objek ISaveable di setiap lokasi (pohon, batu, tanah dicangkul, barang liar, ...)
///     diubah jadi ObjectSave.
///   - Muat: dunia dibuat baru dari file map (seperti main baru), lalu SEMUA objek ISaveable dari map dibuang
///     dan diganti dengan isi file simpanan. Jadi pohon yang sudah ditebang tetap hilang, ladang tetap tercangkul.
/// </summary>
public static class SaveSystem
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <summary>Lokasi file. Tes boleh mengganti ini supaya tidak menimpa simpanan asli.</summary>
    public static string SavePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TandurFarm", "save.json");

    public static bool HasSave => File.Exists(SavePath);

    // ---------- Simpan ----------

    public static void Save(Game game)
    {
        SaveData data = Capture(game);
        Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);

        // Tulis ke file sementara dulu, baru diganti: kalau game tertutup di tengah jalan, simpanan lama tidak rusak
        string temp = SavePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, Options));
        File.Move(temp, SavePath, overwrite: true);
    }

    private static SaveData Capture(Game game)
    {
        Player player = game.Player;
        SaveData data = new()
        {
            SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            WorldSeed = game.WorldSeed,
            Year = game.Time.Date.Year,
            Season = game.Time.Date.Season.ToString(),
            Day = game.Time.Date.Day,
            TimeOfDay = game.Time.TimeOfDay,
            Location = game.CurrentLocation.Name,
            TileX = player.TilePosition.X,
            TileY = player.TilePosition.Y,
            Facing = player.Facing.ToString(),
            Gold = player.Wallet.Gold,
            Stamina = player.Stamina.Current,
            SelectedSlot = player.Inventory.SelectedIndex,
            BackpackLevel = player.Inventory.BackpackLevel,
            LessonStars = game.Lessons.ToSave(),
            Friendship = new Dictionary<string, int>(player.Relationships.AllPoints),
            TalkedToday = player.Relationships.TalkedToday.ToList(),
            GiftedToday = player.Relationships.GiftedToday.ToList(),
            ActiveQuests = game.Quests.Active.Select(q => q.Id).ToList(),
            CompletedQuests = game.Quests.CompletedIds.ToList(),
            ReadMail = game.Mail.ReadIds.ToList(),
            UnreadMail = game.Mail.UnreadIds.ToList(),
        };

        for (int i = 0; i < player.Inventory.Capacity; i++)
        {
            ItemStack? stack = player.Inventory.GetSlot(i);
            if (stack != null)
                data.Inventory.Add(ToSlot(i, stack));
        }
        ItemContainer bin = game.Farm.ShippingBin.Contents;
        for (int i = 0; i < bin.Capacity; i++)
        {
            ItemStack? stack = bin.GetSlot(i);
            if (stack != null)
                data.ShippingBin.Add(ToSlot(i, stack));
        }

        foreach (GameLocation location in game.Locations)
            data.Locations[location.Name] = location.Objects.OfType<ISaveable>().Select(o => o.ToSave()).ToList();

        return data;
    }

    private static SlotSave ToSlot(int index, ItemStack stack) => new()
    {
        Slot = index,
        Item = stack.Item.Id,
        Quantity = stack.Quantity,
        Water = stack.Item is WateringCan can ? can.WaterLeft : -1,
        Durability = stack.Item is Tool tool ? tool.Durability : -1,
    };

    // ---------- Muat ----------

    /// <summary>Baca file simpanan. Null kalau tidak ada atau rusak.</summary>
    public static SaveData? Read()
    {
        if (!HasSave)
            return null;
        try
        {
            return JsonSerializer.Deserialize<SaveData>(File.ReadAllText(SavePath));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Terapkan simpanan ke dunia yang BARU dibuat (Game.CreateWorld sudah dipanggil).</summary>
    public static void Apply(Game game, SaveData data)
    {
        Season season = Enum.TryParse(data.Season, out Season s) ? s : Season.Spring;
        game.Time.Restore(new GameDate(data.Year, season, Math.Clamp(data.Day, 1, GameDate.DaysPerSeason)), data.TimeOfDay);

        Player player = game.Player;
        player.Wallet.Restore(data.Gold);
        player.Stamina.RestoreExact(data.Stamina);
        player.Relationships.Restore(data.Friendship, data.TalkedToday, data.GiftedToday);
        game.Quests.Restore(data.ActiveQuests, data.CompletedQuests);
        game.Mail.Restore(data.ReadMail, data.UnreadMail);
        game.Lessons.Restore(data.LessonStars);

        player.Inventory.Clear();
        // MVP 9: kembalikan dulu ukuran tas sesuai level tersimpan, baru isi slotnya
        int savedBackpack = Math.Clamp(data.BackpackLevel, 0, 2);
        if (savedBackpack >= 1)
            player.Inventory.Expand(GameConfig.BackpackL1Slots - GameConfig.BackpackBaseSlots, 1);
        if (savedBackpack >= 2)
            player.Inventory.Expand(GameConfig.BackpackL2Slots - GameConfig.BackpackL1Slots, 2);
        foreach (SlotSave slot in data.Inventory)
        {
            ItemStack? stack = FromSlot(game.Items, slot);
            if (stack != null && slot.Slot < player.Inventory.Capacity)   // slot di luar kapasitas (mis. save lama) dilewati
                player.Inventory.SetSlot(slot.Slot, stack);
        }
        player.Inventory.Select(data.SelectedSlot);
        RestoreBin(game, data.ShippingBin);

        foreach (GameLocation location in game.Locations)
        {
            if (data.Locations.TryGetValue(location.Name, out List<ObjectSave>? objects))
                RestoreObjects(game, location, objects);
        }

        // Pemain di tempat terakhir (kalau lokasinya hilang, bangun di rumah)
        GameLocation where = game.Locations.FirstOrDefault(l => l.Name == data.Location) ?? game.FarmHouse;
        Point tile = new(data.TileX, data.TileY);
        if (where.Map.GetTile(tile.X, tile.Y)?.IsWalkable != true)
        {
            where = game.FarmHouse;
            tile = FarmHouse.WakeUpTile;
        }
        game.ChangeLocation(where, tile);
        if (Enum.TryParse(data.Facing, out Direction facing))
            player.FaceTo(facing);
    }

    /// <summary>
    /// Isi kotak kirim per slot. Save lama (versi 1) menulis semua isi dengan Slot = 0,
    /// jadi kalau slot sudah terisi barangnya dimasukkan ke slot kosong berikutnya.
    /// </summary>
    private static void RestoreBin(Game game, List<SlotSave> slots)
    {
        ItemContainer bin = game.Farm.ShippingBin.Contents;
        bin.Clear();

        foreach (SlotSave slot in slots)
        {
            ItemStack? stack = FromSlot(game.Items, slot);
            if (stack == null)
                continue;

            if (slot.Slot >= 0 && slot.Slot < bin.Capacity && bin.GetSlot(slot.Slot) == null)
                bin.SetSlot(slot.Slot, stack);
            else
                bin.TryAdd(stack.Item, stack.Quantity);
        }
    }

    private static ItemStack? FromSlot(ItemRegistry items, SlotSave slot)
    {
        try
        {
            Item item = items.Create(slot.Item);
            if (item is WateringCan can && slot.Water >= 0)
                can.RestoreWater(slot.Water);
            if (item is Tool tool && slot.Durability >= 0)
                tool.RestoreDurability(slot.Durability);
            return new ItemStack(item, Math.Max(1, slot.Quantity));
        }
        catch (GameDataException)
        {
            return null;   // barang dari versi lama yang sudah tidak ada: dilewati
        }
    }

    /// <summary>Buang objek ISaveable dari map, lalu buat ulang dari file.</summary>
    private static void RestoreObjects(Game game, GameLocation location, List<ObjectSave> saved)
    {
        foreach (GameObject obj in location.Objects.OfType<ISaveable>().Cast<GameObject>().ToList())
            location.Remove(obj);

        foreach (ObjectSave entry in saved)
        {
            WorldObject? obj = SavedObjectFactory.Create(entry, game.Crops);
            if (obj != null && obj.OccupiedTiles.All(location.IsTileFree))
                location.Add(obj);
        }
    }
}
