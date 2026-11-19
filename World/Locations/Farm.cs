namespace FarmGame.World.Locations;

/// <summary>Kebun milik pemain. MVP 0: hanya map. MVP 1+: tanah dicangkul, tanaman, shipping bin.</summary>
public class Farm : GameLocation
{
    public Farm() : base("Farm", "farm.txt")
    {
    }

    public override bool IsOutdoors => true;
}
