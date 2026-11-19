namespace FarmGame.Interfaces;

/// <summary>Kontrak: objek dunia yang bisa "diperiksa" (tombol X / klik kanan). Contoh: tanaman, kotak kirim, pintu.</summary>
public interface IInteractable
{
    /// <summary>Apakah sekarang ada sesuatu yang bisa dilakukan dengan objek ini?</summary>
    bool CanInteract(Player player);

    ActionResult Interact(ActionContext context);
}
