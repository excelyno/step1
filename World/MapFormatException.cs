namespace FarmGame.World;

/// <summary>
/// Error khusus kalau file map tidak ada atau formatnya salah.
/// Mewarisi Exception bawaan .NET, jadi bisa ditangkap secara spesifik di Program.
/// </summary>
public class MapFormatException : Exception
{
    public MapFormatException(string message) : base(message)
    {
    }
}
