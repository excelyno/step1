namespace FarmGame.Rendering;

/// <summary>
/// Sumber grafis. Kode game hanya bilang "gambar APA" (jenis tile / nama sprite).
/// Kelas yang mengimplementasikan menentukan "BAGAIMANA" (kotak warna sekarang, gambar PNG nanti).
/// </summary>
public interface IAssetProvider
{
    void DrawTile(Graphics g, TileType type, RectangleF dest);

    /// <summary>spriteKey berformat "nama_arah", contoh "player_down".</summary>
    void DrawSprite(Graphics g, string spriteKey, RectangleF dest);
}
