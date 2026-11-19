namespace FarmGame.Interfaces;

/// <summary>Kontrak: sesuatu yang berubah seiring waktu, dipanggil sekali setiap frame.</summary>
public interface IUpdatable
{
    void Update(float deltaSeconds);
}
