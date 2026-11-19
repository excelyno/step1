namespace FarmGame.Interfaces;

/// <summary>Kontrak: sesuatu yang berubah saat ganti hari (tanaman tumbuh, tanah mengering).</summary>
public interface IDayUpdatable
{
    void OnNewDay(GameDate newDate);
}
