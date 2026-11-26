namespace FarmGame.Core.States;

/// <summary>
/// Mode kotak kirim: kiri tas, kanan isi kotak. Jam berhenti selama menu terbuka.
///
/// Mouse : tahan klik kiri di slot lalu lepas di slot tujuan (drag & drop).
///         klik kanan = pindah seluruh tumpukan ke panel seberang.
/// Keyboard: WASD/panah pilih slot, Q/E ganti panel, C pindah 1, Shift+C pindah semua, X/Esc tutup.
///
/// Aturan penting: barang hanya MASUK kotak, tidak langsung terjual. Penjualan terjadi saat tidur.
/// </summary>
public class ShippingBinState : GameState
{
    private readonly ShippingBin _bin;
    private readonly Inventory _bag;

    private bool _focusBin;                 // true = pilihan keyboard di grid kotak
    private int _selected;                  // indeks slot di panel yang sedang fokus
    private (bool InBin, int Index)? _hover; // slot di bawah mouse
    private DragState? _drag;               // barang yang sedang dipegang mouse

    public ShippingBinState(Game game, ShippingBin bin) : base(game)
    {
        _bin = bin;
        _bag = game.Player.Inventory;
    }

    public override void Enter()
    {
        Game.Time.Pause();
        Game.Player.SetMovementIntent(Vector2.Zero, false);
    }

    public override void Update(float deltaSeconds)
    {
        InputManager input = Game.Input;
        Game.Toasts.Update(deltaSeconds);

        _hover = ShippingBinMenu.HitTest(input.MousePosition, _bag.Capacity, _bin.Contents.Capacity);

        if (input.WasPressed(Keys.Escape) || input.WasPressed(Keys.X))
        {
            ReturnDrag();
            Close();
            return;
        }

        HandleMouse(input);
        HandleKeyboard(input);
    }

    public override void Draw(Graphics g)
    {
        Game.DrawWorldAndHud(g, showToolbar: false);
        Game.ShippingBinMenu.Draw(g, _bin, _bag, _focusBin, _selected, _hover, _drag?.Stack, Game.Input.MousePosition);
        Game.Toasts.Draw(g);
    }

    private void Close() { Game.PlaySound("ui_close"); Game.ChangeState(new PlayingState(Game)); }

    // ---------- Mouse ----------

    private void HandleMouse(InputManager input)
    {
        if (_drag == null)
        {
            if (input.MouseDownPressed(MouseButtons.Left) && _hover is { } hit)
            {
                if (input.IsRunning)
                    QuickMove(hit);      // Shift + klik = pindah semua, sama seperti klik kanan
                else
                    BeginDrag(hit);
            }
            else if (input.MouseDownPressed(MouseButtons.Right) && _hover is { } right)
            {
                QuickMove(right);
            }
        }
        else if (input.MouseUpPressed(MouseButtons.Left))
        {
            DropAt(_hover);
        }
    }

    private void BeginDrag((bool InBin, int Index) slot)
    {
        ItemContainer from = Container(slot.InBin);
        ItemStack? stack = from.TakeSlot(slot.Index);
        if (stack == null)
            return;

        _drag = new DragState(from, slot.Index, stack);
        _focusBin = slot.InBin;
        _selected = slot.Index;
    }

    /// <summary>Lepas barang di slot tujuan: kosong = taruh, sejenis = gabung, beda = tukar.</summary>
    private void DropAt((bool InBin, int Index)? target)
    {
        DragState drag = _drag!;

        if (target == null)
        {
            ReturnDrag();
            return;
        }

        var (inBin, index) = target.Value;
        if (inBin && !_bin.Accepts(drag.Stack.Item))
        {
            Game.Notify($"{drag.Stack.Item.Name} tidak bisa dijual.", false);
            ReturnDrag();
            return;
        }

        ItemContainer dest = Container(inBin);
        if (dest == drag.Source && index == drag.SourceIndex)
        {
            ReturnDrag();
            return;
        }

        string place = inBin ? "kotak" : "tas";
        ItemStack? existing = dest.GetSlot(index);

        if (existing == null)
        {
            dest.SetSlot(index, drag.Stack);
            _drag = null;
            Game.Notify($"{drag.Stack.Quantity}x {drag.Stack.Item.Name} → {place}", true);
            return;
        }

        if (existing.Item.IsSameKind(drag.Stack.Item))
        {
            int left = existing.Add(drag.Stack.Quantity);
            int moved = drag.Stack.Quantity - left;
            _drag = null;
            if (moved > 0)
                Game.Notify($"{moved}x {drag.Stack.Item.Name} → {place}", true);
            if (left > 0)
                ReturnStack(drag.Source, drag.SourceIndex, new ItemStack(drag.Stack.Item, left));
            return;
        }

        // Barang berbeda: tukar posisi (barang lama ikut terangkat ke tempat asal)
        dest.SetSlot(index, drag.Stack);
        _drag = null;
        ReturnStack(drag.Source, drag.SourceIndex, existing);
    }

    private void QuickMove((bool InBin, int Index) slot)
    {
        if (slot.InBin)
            MoveBetween(_bin.Contents, slot.Index, _bag, int.MaxValue, "tas");
        else
            MoveBetween(_bag, slot.Index, _bin.Contents, int.MaxValue, "kotak");
    }

    // ---------- Keyboard ----------

    private void HandleKeyboard(InputManager input)
    {
        if (_drag != null)
            return;   // saat barang dipegang mouse, keyboard diabaikan (Esc/X sudah ditangani)

        if (input.WasPressed(Keys.Q) || input.WasPressed(Keys.E))
        {
            _focusBin = !_focusBin;
            _selected = 0;
            return;
        }

        int count = _focusBin ? _bin.Contents.Capacity : _bag.Capacity;
        int cols = ShippingBinMenu.Columns;

        if (input.WasPressed(Keys.A) || input.WasPressed(Keys.Left))
            _selected = (_selected - 1 + count) % count;
        if (input.WasPressed(Keys.D) || input.WasPressed(Keys.Right))
            _selected = (_selected + 1) % count;
        if (input.WasPressed(Keys.W) || input.WasPressed(Keys.Up))
            _selected = (_selected - cols + count) % count;
        if (input.WasPressed(Keys.S) || input.WasPressed(Keys.Down))
            _selected = (_selected + cols) % count;

        bool confirm = input.WasPressed(Keys.C) || input.WasPressed(Keys.Enter) || input.WasPressed(Keys.Space);
        if (!confirm)
            return;

        int amount = input.IsRunning ? int.MaxValue : 1;
        if (_focusBin)
            MoveBetween(_bin.Contents, _selected, _bag, amount, "tas");
        else
            MoveBetween(_bag, _selected, _bin.Contents, amount, "kotak");
    }

    // ---------- Aturan pindah barang ----------

    private ItemContainer Container(bool inBin) => inBin ? _bin.Contents : _bag;

    /// <summary>Pindah sebagian/seluruh tumpukan. Sisa yang tidak muat tetap di tempat asal.</summary>
    private void MoveBetween(ItemContainer from, int index, ItemContainer to, int amount, string toName)
    {
        ItemStack? stack = from.GetSlot(index);
        if (stack == null)
            return;

        if (to == _bin.Contents && !_bin.Accepts(stack.Item))
        {
            Game.Notify($"{stack.Item.Name} tidak bisa dijual.", false);
            return;
        }

        ItemStack? taken = TakeFrom(from, index, Math.Min(amount, stack.Quantity));
        if (taken == null)
            return;

        int left = to.TryAdd(taken.Item, taken.Quantity);
        int moved = taken.Quantity - left;

        if (moved > 0)
            Game.Notify($"{moved}x {taken.Item.Name} → {toName}", true);

        if (left > 0)
        {
            ReturnStack(from, index, new ItemStack(taken.Item, left));
            Game.Notify(toName == "kotak" ? "Kotak kirim penuh." : "Tas penuh.", false);
        }
    }

    private static ItemStack? TakeFrom(ItemContainer container, int index, int amount)
    {
        ItemStack? stack = container.GetSlot(index);
        if (stack == null)
            return null;

        int take = Math.Min(amount, stack.Quantity);
        if (take <= 0)
            return null;

        Item item = stack.Item;
        container.RemoveFromSlot(index, take);
        return new ItemStack(item, take);
    }

    /// <summary>Kembalikan barang ke tempat asal (slot yang sama kalau masih kosong).</summary>
    private static void ReturnStack(ItemContainer container, int preferredIndex, ItemStack stack)
    {
        if (container.GetSlot(preferredIndex) == null)
        {
            container.SetSlot(preferredIndex, stack);
            return;
        }
        container.TryAdd(stack.Item, stack.Quantity);
    }

    private void ReturnDrag()
    {
        if (_drag == null)
            return;
        DragState drag = _drag;
        _drag = null;
        ReturnStack(drag.Source, drag.SourceIndex, drag.Stack);
    }

    private sealed class DragState
    {
        public ItemContainer Source { get; }
        public int SourceIndex { get; }
        public ItemStack Stack { get; }

        public DragState(ItemContainer source, int sourceIndex, ItemStack stack)
        {
            Source = source;
            SourceIndex = sourceIndex;
            Stack = stack;
        }
    }
}
