namespace FarmGame.Interfaces;

/// <summary>Kontrak: item yang bisa dimakan untuk memulihkan energi.</summary>
public interface IEdible
{
    int EnergyRestore { get; }

    ActionResult Eat(Player eater);
}
