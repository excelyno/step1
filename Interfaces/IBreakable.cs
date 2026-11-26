namespace FarmGame.Interfaces;

/// <summary>Kontrak: benda dunia yang bisa dipukul sampai hancur dengan alat tertentu (pohon, batu).</summary>
public interface IBreakable
{
    ToolKind RequiredTool { get; }

    /// <summary>Satu pukulan. Pukulan terakhir menghancurkan benda dan memberi bahan ke tas.</summary>
    ActionResult TakeHit(ActionContext context);
}
