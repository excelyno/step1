namespace FarmGame.World.Objects;

/// <summary>
/// Kotak pengiriman. Pemain memasukkan barang lewat menu (X), lalu barang DIJUAL malam hari saat tidur.
/// Isi kotak disimpan di ItemContainer supaya bisa dilihat, ditarik kembali, dan dibatasi jumlah slotnya.
/// </summary>
public class ShippingBin : WorldObject, IInteractable, IShippingContainer
{
    public ShippingBin(Point tile) : base(tile)
    {
        Contents = new ItemContainer(GameConfig.ShippingBinSize);
    }

    public override Size SizeInTiles => new(2, 1);
    public override string SpriteKey => "bin";

    /// <summary>Gambar 8 piksel lebih tinggi dari hitbox (tutup kotak menonjol ke atas).</summary>
    public override RectangleF SpriteBounds => new(Position.X, Position.Y - 8, 32, 24);
    public override RectangleF CollisionBox => new(Position.X + 1, Position.Y + 2, 30, 13);

    /// <summary>Isi kotak. Hanya barang yang bisa dijual yang boleh masuk.</summary>
    public ItemContainer Contents { get; }

    /// <summary>Aturan kotak: alat & barang tanpa harga jual tidak boleh dimasukkan.</summary>
    public bool Accepts(Item item) => item.IsSellable;

    public bool CanInteract(Player player) => true;

    public string InteractLabel(Player player) => "Buka kotak";

    /// <summary>X di depan kotak membuka menu tas & kotak (drag & drop).</summary>
    public ActionResult Interact(ActionContext context)
    {
        context.Services.OpenShippingBin(this);
        return ActionResult.None();
    }

    /// <summary>Dipanggil saat ganti hari: jual semua isi, bayar ke dompet, kosongkan kotak.</summary>
    public List<ShipmentLine> ProcessShipment(Wallet wallet)
    {
        List<ShipmentLine> report = new();

        // Gabungkan barang sejenis jadi satu baris laporan
        foreach (var group in Contents.Stacks.GroupBy(stack => stack.Item.Id))
        {
            string name = group.First().Item.Name;
            int quantity = group.Sum(stack => stack.Quantity);
            int total = group.Sum(stack => stack.TotalSellValue);
            report.Add(new ShipmentLine(name, quantity, total));

            if (total > 0)
                wallet.Earn(total);
        }

        Contents.Clear();
        return report;
    }
}
