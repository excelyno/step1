namespace FarmGame.Items;

/// <summary>Kapak: menebang pohon dan membersihkan ranting. Hasilnya kayu.</summary>
public class Axe : BreakingTool
{
    public Axe() : base("axe", "Kapak", "Menebang pohon dan ranting.")
    {
    }

    public override ToolKind Kind => ToolKind.Axe;
    public override int EnergyCost => 3;

    protected override EffectKind GetUseEffect(ActionContext context) => EffectKind.WoodChips;
}
