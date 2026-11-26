namespace FarmGame.Entities.Components;

/// <summary>Energi pemain. Nilainya selalu di antara 0 dan Max.</summary>
public class Stamina
{
    public int Max { get; }
    public int Current { get; private set; }   // hanya berubah lewat method di bawah

    public float Ratio => (float)Current / Max;

    public Stamina(int max)
    {
        Max = max;
        Current = max;
    }

    public bool CanSpend(int amount) => amount <= Current;

    /// <summary>MVP 5: dari file simpanan.</summary>
    public void RestoreExact(int value) => Current = Math.Clamp(value, 0, Max);

    public void Spend(int amount)
    {
        if (!CanSpend(amount))
            throw new InvalidOperationException("Energi tidak cukup.");   // penjaga bug
        Current -= amount;
    }

    /// <summary>Tambah energi, tidak boleh lebih dari Max.</summary>
    public void Restore(int amount)
    {
        Current = Math.Min(Max, Current + amount);
    }

    /// <summary>Atur energi ke persentase Max (dipakai saat bangun tidur). 1 = 100%.</summary>
    public void RestoreTo(float ratio)
    {
        Current = (int)(Max * ratio);
    }
}
