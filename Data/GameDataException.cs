namespace FarmGame.Data;

/// <summary>Error khusus kalau file data (crops.json) hilang, rusak, atau isinya salah.</summary>
public class GameDataException : Exception
{
    public GameDataException(string message) : base(message)
    {
    }
}
