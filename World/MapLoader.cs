namespace FarmGame.World;

/// <summary>
/// Membaca file map teks (1 huruf = 1 tile) menjadi TileMap.
/// Legenda tile : G=rumput D=tanah P=jalan W=air F=pagar B=bangunan .=void S=spawn pemain
///                O=lantai X=dinding dalam rumah (MVP 3), H=jembatan/dermaga C=batu alun-alun (MVP 5)
/// Legenda hiasan: T=pohon Y=cemara b=semak R=batu g=rumput liar *=bunga L=teratai w=ranting p=kerikil
///   (hiasan dicatat sebagai MapMarker; tile di bawahnya rumput, air untuk teratai, tanah untuk ranting & kerikil)
/// Baris yang diawali '#' adalah komentar. 'S' boleh tidak ada (MVP 2).
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
        List<MapMarker> markers = new();

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
                else if (MapMarker.IsMarker(c))
                {
                    markers.Add(new MapMarker(c, new Point(x, y)));
                    layout[x, y] = MapMarker.GroundUnder(c);
                }
                else
                {
                    layout[x, y] = CharToTile(c, fileName, x, y);
                }
            }
        }

        // MVP 2: 'S' boleh tidak ada (kota tidak butuh). Kalau tidak ada, pakai tengah map.
        Point spawnTile = spawn ?? new Point(width / 2, height / 2);

        return new TileMap(layout, spawnTile, markers);
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
            case 'O': return TileType.Floor;
            case 'X': return TileType.Wall;
            case 'H': return TileType.Bridge;
            case 'C': return TileType.Cobble;
            default:
                throw new MapFormatException(
                    $"{fileName}: karakter '{c}' tidak dikenal di kolom {x + 1}, baris map ke-{y + 1}.");
        }
    }
}
