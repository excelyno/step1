namespace FarmGame.Saving;

/// <summary>
/// "Pabrik" kebalikan ISaveable.ToSave(): ObjectSave dari file -> objek dunia (MVP 5).
/// Jenis baru yang bisa disimpan cukup ditambah satu baris di sini.
/// </summary>
public static class SavedObjectFactory
{
    public static WorldObject? Create(ObjectSave save, CropDatabase crops)
    {
        Point tile = save.Tile;
        switch (save.Kind)
        {
            case "tree": return new Tree(tile);
            case "pine": return new Pine(tile);
            case "rock": return new Rock(tile);
            case "twig": return new Twig(tile);
            case "pebble": return new Pebble(tile);
            case "stump": return new TreeStump(tile, save.Get("pine") == 1, save.Get("age"));
            case "forage": return Forage.ItemIds.Contains(save.Text) ? new Forage(tile, save.Text) : null;
            case "hoedirt":
            {
                HoeDirt dirt = new(tile);
                CropData? data = crops.All.FirstOrDefault(c => c.Id == save.Text);
                Crop? crop = data == null ? null : new Crop(data, save.Get("days"), save.Get("dead") == 1);
                HazardKind hazard = Enum.IsDefined(typeof(HazardKind), save.Get("hazard"))
                    ? (HazardKind)save.Get("hazard")
                    : HazardKind.None;
                dirt.Restore(save.Get("watered") == 1, crop, hazard, save.Get("hazardDays"), save.Get("penalty") == 1);
                return dirt;
            }
            default:
                return null;   // jenis tidak dikenal (file dari versi lain): dilewati
        }
    }
}
