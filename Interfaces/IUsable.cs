namespace FarmGame.Interfaces;

/// <summary>Kontrak: item yang bisa "dipakai" ke tile di depan pemain (tombol C / klik kiri). Contoh: alat, bibit.</summary>
public interface IUsable
{
    /// <summary>True kalau item berkurang 1 setiap berhasil dipakai (bibit). Alat = false.</summary>
    bool ConsumesOnUse { get; }

    ActionResult Use(ActionContext context);
}
