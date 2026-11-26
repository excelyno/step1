namespace FarmGame.Items;

/// <summary>
/// Induk alat pemukul (kapak, beliung). Cara kerjanya sama:
/// cari IBreakable di tile depan yang butuh alat ini, lalu pukul.
/// Kalau tidak ada, turunan boleh punya efek lain lewat ApplyToOther().
/// </summary>
public abstract class BreakingTool : Tool
{
    protected BreakingTool(string id, string name, string description, int maxDurability)
        : base(id, name, description, maxDurability)
    {
    }

    /// <summary>Jenis alat ini. WAJIB diisi turunan.</summary>
    public abstract ToolKind Kind { get; }

    protected override ActionResult ApplyEffect(ActionContext context)
    {
        if (context.Location.GetObjectAt(context.TargetTile) is IBreakable target && target.RequiredTool == Kind)
            return target.TakeHit(context);   // polimorfisme: pohon, batu, ranting, kerikil

        return ApplyToOther(context);
    }

    /// <summary>Efek kalau tidak ada yang bisa dipukul. Default: gagal dengan pesan.</summary>
    protected virtual ActionResult ApplyToOther(ActionContext context)
    {
        return ActionResult.Fail($"Tidak ada yang bisa dipukul dengan {Name.ToLower()}.");
    }
}
