namespace FarmGame.Interfaces;

/// <summary>
/// Paket data untuk sebuah aksi: siapa pelakunya, di lokasi mana, tile mana yang dituju,
/// dan layanan game apa yang boleh dipakai. Satu parameter ini cukup untuk semua aksi.
/// </summary>
public class ActionContext
{
    public Player User { get; }
    public GameLocation Location { get; }
    public Point TargetTile { get; }
    public IGameServices Services { get; }

    public ActionContext(Player user, GameLocation location, Point targetTile, IGameServices services)
    {
        User = user;
        Location = location;
        TargetTile = targetTile;
        Services = services;
    }
}
