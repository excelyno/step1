namespace FarmGame.Interfaces;

/// <summary>
/// Kontrak untuk benda yang bisa diperiksa dengan tombol X (pintu, papan, warga, tanaman siap panen, ...).
/// </summary>
public interface IInteractable
{
    bool CanInteract(Player player);

    ActionResult Interact(ActionContext context);

    /// <summary>
    /// MVP 5: kata singkat di gelembung petunjuk "[X] Bicara". Default "Periksa".
    /// Ini default interface member: kelas yang tidak menulisnya otomatis memakai "Periksa".
    /// </summary>
    string InteractLabel(Player player) => "Periksa";
}
