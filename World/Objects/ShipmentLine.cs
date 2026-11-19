namespace FarmGame.World.Objects;

/// <summary>Satu baris laporan penjualan, contoh: "Parsnip x1 = 35 g".</summary>
public class ShipmentLine
{
    public string ItemName { get; }
    public int Quantity { get; }
    public int Total { get; }

    public ShipmentLine(string itemName, int quantity, int total)
    {
        ItemName = itemName;
        Quantity = quantity;
        Total = total;
    }
}
