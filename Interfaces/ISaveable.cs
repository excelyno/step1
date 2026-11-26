namespace FarmGame.Interfaces;

/// <summary>
/// Kontrak (MVP 5): benda dunia yang bisa berubah selama bermain (pohon ditebang, tanah dicangkul, barang liar)
/// dan karena itu harus ikut disimpan. Saat dimuat, semua ISaveable dari map dibuang lalu dibuat ulang dari file.
/// </summary>
public interface ISaveable
{
    ObjectSave ToSave();
}
