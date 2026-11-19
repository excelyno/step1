namespace FarmGame.Entities.Components;

/// <summary>Dompet. Uang hanya berubah lewat Earn / TrySpend, dan tidak pernah negatif.</summary>
public class Wallet
{
    public int Gold { get; private set; }

    public Wallet(int startingGold)
    {
        Gold = startingGold;
    }

    public void Earn(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Jumlah harus positif.");
        Gold += amount;
    }

    /// <summary>Kurangi uang kalau cukup (dipakai toko di MVP 2).</summary>
    public bool TrySpend(int amount)
    {
        if (amount <= 0 || amount > Gold)
            return false;
        Gold -= amount;
        return true;
    }
}
