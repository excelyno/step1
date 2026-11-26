namespace FarmGame.Interfaces;

/// <summary>
/// Kontrak benda yang bisa menerima barang untuk dijual saat tidur.
/// Game.EndDay memproses SEMUA container di semua lokasi, jadi kotak baru di lokasi lain
/// otomatis ikut terjual tanpa mengubah kode Game.
/// </summary>
public interface IShippingContainer
{
    /// <summary>Isi kotak (dipakai untuk simpan/muat dan laporan).</summary>
    ItemContainer Contents { get; }

    /// <summary>Jual semua isi, bayar ke dompet, lalu kosongkan. Hasilnya untuk ringkasan malam.</summary>
    List<ShipmentLine> ProcessShipment(Wallet wallet);
}
