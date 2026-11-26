namespace FarmGame.Items;

/// <summary>Beliung: memecah batu dan kerikil. Juga bisa meratakan tanah dicangkul yang kosong.</summary>
public class Pickaxe : BreakingTool
{
    public Pickaxe() : base("pickaxe", "Beliung", "Memecah batu. Meratakan tanah yang dicangkul.", GameConfig.PickaxeDurability)
    {
    }

    public override ToolKind Kind => ToolKind.Pickaxe;
    public override int EnergyCost => 3;

    protected override EffectKind GetUseEffect(ActionContext context) => EffectKind.StoneChips;

    /// <summary>Tidak ada batu? Ratakan tanah dicangkul yang tidak ada tanamannya.</summary>
    protected override ActionResult ApplyToOther(ActionContext context)
    {
        if (context.Location.GetObjectAt(context.TargetTile) is HoeDirt dirt && !dirt.HasCrop)
        {
            context.Location.Remove(dirt);
            return ActionResult.Ok();
        }
        return base.ApplyToOther(context);
    }
}
