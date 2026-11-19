namespace FarmGame.World.Objects;

/// <summary>
/// Kotak pengiriman. Pemain memasukkan barang (X), lalu barang DIJUAL malam hari saat tidur.
/// </summary>
public class ShippingBin : WorldObject, IInteractable
{
    private readonly List<ItemStack> _contents = new();

    public ShippingBin(Point tile) : base(tile)
    {
    }

    public override Size SizeInTiles => new(2, 1);
    public override string SpriteKey => "bin";

    public IReadOnlyList<ItemStack> Contents => _contents;

    public bool CanInteract(Player player) => true;

    /// <summary>Masukkan SELURUH tumpukan di slot terpilih ke kotak.</summary>
    public ActionResult Interact(ActionContext context)
    {
        Inventory bag = context.User.Inventory;
        Item? item = bag.SelectedItem;

        if (item == null)
            return ActionResult.Fail("Pilih barang yang mau dikirim.");

        if (!item.IsSellable)
            return ActionResult.Fail($"{item.Name} tidak bisa dijual.");

        ItemStack stack = bag.TakeSlot(bag.SelectedIndex)!;
        _contents.Add(stack);
        return ActionResult.Ok($"{stack.Quantity}x {item.Name} masuk kotak ({stack.TotalSellValue}g)");
    }

    /// <summary>Dipanggil saat ganti hari: jual semua isi, bayar ke dompet, kosongkan kotak.</summary>
    public List<ShipmentLine> ProcessShipment(Wallet wallet)
    {
        List<ShipmentLine> report = new();

        // Gabungkan barang sejenis jadi satu baris laporan
        foreach (var group in _contents.GroupBy(stack => stack.Item.Id))
        {
            string name = group.First().Item.Name;
            int quantity = group.Sum(stack => stack.Quantity);
            int total = group.Sum(stack => stack.TotalSellValue);
            report.Add(new ShipmentLine(name, quantity, total));

            if (total > 0)
                wallet.Earn(total);
        }

        _contents.Clear();
        return report;
    }
}
