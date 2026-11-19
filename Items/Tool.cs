namespace FarmGame.Items;

/// <summary>
/// Kelas induk semua alat. Memakai pola TEMPLATE METHOD:
/// Use() menetapkan urutan yang selalu sama (cek energi -> efek -> bayar energi),
/// kelas turunan cukup mengisi ApplyEffect() dan biaya energinya.
/// </summary>
public abstract class Tool : Item, IUsable
{
    protected Tool(string id, string name, string description)
        : base(id, name, description, 0)
    {
    }

    public override ItemCategory Category => ItemCategory.Tool;
    public override int MaxStack => 1;            // alat tidak bisa ditumpuk
    public override bool IsSellable => false;     // alat tidak bisa dijual

    /// <summary>Biaya energi sekali pakai. WAJIB diisi turunan.</summary>
    public abstract int EnergyCost { get; }

    public bool ConsumesOnUse => false;           // alat tidak habis dipakai

    /// <summary>Urutan tetap untuk SEMUA alat. Turunan tidak bisa mengubah urutannya.</summary>
    public ActionResult Use(ActionContext context)
    {
        int cost = GetEnergyCost(context);

        // 1. Cek energi cukup
        if (!context.User.Stamina.CanSpend(cost))
            return ActionResult.Fail("Kamu terlalu lelah. Istirahatlah.");

        // 2. Jalankan efek khas alat
        ActionResult result = ApplyEffect(context);

        // 3. Energi hanya berkurang kalau berhasil
        if (result.Success && cost > 0)
            context.User.Stamina.Spend(cost);

        return result;
    }

    /// <summary>Default = EnergyCost. Boleh ditimpa kalau biayanya tergantung situasi.</summary>
    protected virtual int GetEnergyCost(ActionContext context) => EnergyCost;

    /// <summary>Efek khas alat. WAJIB ditulis turunan.</summary>
    protected abstract ActionResult ApplyEffect(ActionContext context);
}
