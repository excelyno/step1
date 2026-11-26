namespace FarmGame.Items;

/// <summary>Cangkul: mengubah tanah (Dirt) yang kosong menjadi HoeDirt yang siap ditanami.</summary>
public class Hoe : Tool
{
    public Hoe() : base("hoe", "Cangkul", "Mengolah tanah agar bisa ditanami.")
    {
    }

    public override int EnergyCost => 2;

    protected override EffectKind GetUseEffect(ActionContext context) => EffectKind.Dirt;

    protected override ActionResult ApplyEffect(ActionContext context)
    {
        Point target = context.TargetTile;
        Tile? tile = context.Location.Map.GetTile(target.X, target.Y);

        if (tile == null || !tile.IsTillable)
            return ActionResult.Fail("Tanah ini tidak bisa dicangkul.");

        if (!context.Location.IsTileFree(target))
            return ActionResult.Fail("Ada sesuatu di tanah ini.");

        context.Location.Add(new HoeDirt(target));

        // MVP 6: petak tetangga ikut ganti gambar (bedeng menyambung), jadi nama gambarnya dihitung ulang
        Point t = target;
        foreach (Point around in new[] { new Point(t.X, t.Y - 1), new Point(t.X, t.Y + 1), new Point(t.X - 1, t.Y), new Point(t.X + 1, t.Y) })
        {
            if (context.Location.GetObjectAt(around) is HoeDirt dirt)
                dirt.InvalidateSprite();
        }
        return ActionResult.Ok();
    }
}
