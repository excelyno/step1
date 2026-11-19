namespace FarmGame.Rendering;

/// <summary>
/// Sumber grafis. Kode game hanya bilang "gambar APA" (jenis tile / nama sprite).
/// Kelas yang mengimplementasikan menentukan "BAGAIMANA" (kotak warna sekarang, gambar PNG nanti).
/// </summary>
public interface IAssetProvider
{
    /// <summary>
    /// Gambar tile (x, y) dari map. Map ikut dikirim supaya penggambar bisa melihat tile tetangga
    /// (misalnya pinggiran rumput di tepi tanah, tepi kolam, pagar yang menyambung).
    /// </summary>
    void DrawTile(Graphics g, TileMap map, int x, int y, RectangleF dest);

    /// <summary>spriteKey berformat "kategori_nama[_detail]", contoh "player_down_2", "crop_parsnip_3".</summary>
    void DrawSprite(Graphics g, string spriteKey, RectangleF dest);
}
