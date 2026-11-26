namespace FarmGame.Interfaces;

/// <summary>Kontrak (MVP 4): benda yang memancarkan cahaya saat gelap. Lighting mengumpulkan semuanya.</summary>
public interface ILightSource
{
    IEnumerable<Light> GetLights();
}
