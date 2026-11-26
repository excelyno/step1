namespace FarmGame.Items;

/// <summary>Pancing: menghadap air lalu C = lempar kail. Minigame-nya diurus FishingState.</summary>
public class FishingRod : Tool
{
    public FishingRod() : base("fishing_rod", "Pancing", "Hadap ke air, lalu lempar kail.")
    {
    }

    public override int EnergyCost => 4;

    protected override EffectKind GetUseEffect(ActionContext context) => EffectKind.Splash;

    protected override ActionResult ApplyEffect(ActionContext context)
    {
        Tile? tile = context.Location.Map.GetTile(context.TargetTile.X, context.TargetTile.Y);
        if (tile == null || tile.Type != TileType.Water)
            return ActionResult.Fail("Hadap ke air untuk memancing.");

        context.Services.StartFishing(context.TargetTile);
        return ActionResult.Ok();
    }
}
