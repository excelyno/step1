namespace FarmGame.World;

/// <summary>
/// Membaca file map teks (1 huruf = 1 tile) menjadi TileMap.
/// Legenda: G=rumput D=tanah P=jalan W=air F=pagar B=bangunan .=void S=spawn pemain
/// Baris yang diawali '#' adalah komentar.
/// </summary>
public static class MapLoader
{
    public static TileMap LoadFromFile(string path)
    {
        if (!File.Exists(path))
            throw new MapFormatException("File map tidak ditemukan: " + path);

        string fileName = Path.GetFileName(path);

        // Ambil baris yang berisi saja (abaikan baris kosong dan komentar '#')
        List<string> rows = new();
        foreach (string line in File.ReadAllLines(path))
        {
            string row = line.TrimEnd();
            if (row.Length > 0 && row[0] != '#')
                rows.Add(row);
        }

        if (rows.Count == 0)
            throw new MapFormatException(fileName + ": map kosong.");

        int width = rows[0].Length;
        int height = rows.Count;
        TileType[,] layout = new TileType[width, height];
        Point? spawn = null;

        for (int y = 0; y < height; y++)
        {
            if (rows[y].Length != width)
                throw new MapFormatException(
                    $"{fileName}: baris map ke-{y + 1} panjangnya {rows[y].Length}, seharusnya {width}.");

            for (int x = 0; x < width; x++)
            {
                char c = rows[y][x];

                if (c == 'S')
                {
                    if (spawn != null)
                        throw new MapFormatException(fileName + ": titik spawn 'S' lebih dari satu.");
                    spawn = new Point(x, y);
                    layout[x, y] = TileType.Dirt;   // di bawah titik spawn dianggap tanah
                }
                else
                {
                    layout[x, y] = CharToTile(c, fileName, x, y);
                }
            }
        }

        if (spawn == null)
            throw new MapFormatException(fileName + ": titik spawn 'S' tidak ditemukan.");

        return new TileMap(layout, spawn.Value);
    }

    private static TileType CharToTile(char c, string fileName, int x, int y)
    {
        switch (c)
        {
            case '.': return TileType.Void;
            case 'G': return TileType.Grass;
            case 'D': return TileType.Dirt;
            case 'P': return TileType.Path;
            case 'W': return TileType.Water;
            case 'F': return TileType.Fence;
            case 'B': return TileType.Building;
            default:
                throw new MapFormatException(
                    $"{fileName}: karakter '{c}' tidak dikenal di kolom {x + 1}, baris map ke-{y + 1}.");
        }
    }
}
