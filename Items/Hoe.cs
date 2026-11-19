namespace FarmGame.Items;

/// <summary>Cangkul: mengubah tanah (Dirt) yang kosong menjadi HoeDirt yang siap ditanami.</summary>
public class Hoe : Tool
{
    public Hoe() : base("hoe", "Cangkul", "Mengolah tanah agar bisa ditanami.")
    {
    }

    public override int EnergyCost => 2;

    protected override ActionResult ApplyEffect(ActionContext context)
    {
        Point target = context.TargetTile;
        Tile? tile = context.Location.Map.GetTile(target.X, target.Y);

        if (tile == null || !tile.IsTillable)
            return ActionResult.Fail("Tanah ini tidak bisa dicangkul.");

        if (!context.Location.IsTileFree(target))
            return ActionResult.Fail("Ada sesuatu di tanah ini.");

        context.Location.Add(new HoeDirt(target));
        return ActionResult.Ok();
    }
}
