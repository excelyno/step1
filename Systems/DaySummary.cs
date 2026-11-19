namespace FarmGame.Systems;

/// <summary>Ringkasan akhir hari yang tampil di layar gelap setelah tidur.</summary>
public class DaySummary
{
    public GameDate EndedDate { get; }
    public GameDate NewDate { get; }
    public SleepReason Reason { get; }
    public List<ShipmentLine> SoldItems { get; }
    public int TotalEarned { get; }
    public int GoldAfter { get; }
    public float EnergyRatio { get; }   // 1 = 100%

    public DaySummary(GameDate endedDate, GameDate newDate, SleepReason reason,
                      List<ShipmentLine> soldItems, int goldAfter, float energyRatio)
    {
        EndedDate = endedDate;
        NewDate = newDate;
        Reason = reason;
        SoldItems = soldItems;
        GoldAfter = goldAfter;
        EnergyRatio = energyRatio;

        foreach (ShipmentLine line in soldItems)
            TotalEarned += line.Total;
    }
}
