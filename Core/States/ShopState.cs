namespace FarmGame.Core.States;

/// <summary>
/// Mode belanja: dunia tetap tergambar, menu toko di atasnya, jam berhenti.
/// W/S pilih barang, C/Enter beli 1 (tahan Shift = beli 5), X/Esc/klik kanan keluar.
/// Mouse: arahkan ke baris (sorot biru), klik kiri = pilih & beli baris itu.
/// </summary>
public class ShopState : GameState
{
    private const int BulkAmount = 5;

    private readonly Shop _shop;
    private List<ShopEntry> _stock = new();
    private int _selected;
    private int _hover = -1;

    public ShopState(Game game, Shop shop) : base(game)
    {
        _shop = shop;
    }

    public override void Enter()
    {
        Game.Time.Pause();
        Game.Player.SetMovementIntent(Vector2.Zero, false);
        _stock = _shop.GetStock(Game.Date);   // isi toko sesuai musim hari ini
    }

    public override void Update(float deltaSeconds)
    {
        InputManager input = Game.Input;
        Game.Toasts.Update(deltaSeconds);

        _hover = ShopMenu.HitTestRow(input.MousePosition, _stock.Count, _selected);

        if (input.WasPressed(Keys.Escape) || input.WasPressed(Keys.X) || input.MouseDownPressed(MouseButtons.Right))
        {
            Game.PlaySound("ui_close");
            Game.ChangeState(new PlayingState(Game));
            return;
        }

        if (_stock.Count == 0)
            return;

        // Pilih baris (berputar di ujung)
        if (input.MenuUpPressed)
            _selected = (_selected - 1 + _stock.Count) % _stock.Count;
        if (input.MenuDownPressed)
            _selected = (_selected + 1) % _stock.Count;

        // Klik kiri di baris = pilih baris itu lalu beli
        if (input.MouseDownPressed(MouseButtons.Left))
        {
            if (_hover >= 0)
            {
                _selected = _hover;
                Buy(input.IsRunning ? BulkAmount : 1);
            }
            return;
        }

        bool confirm = input.WasPressed(Keys.C) || input.WasPressed(Keys.Enter) || input.WasPressed(Keys.Space);
        if (confirm)
            Buy(input.IsRunning ? BulkAmount : 1);   // Shift = beli banyak
    }

    private void Buy(int amount)
    {
        ActionResult result = _shop.Buy(_stock[_selected], amount, Game.Player, Game.Items);
        if (result.Message != null)
            Game.Toasts.Push(result.Message, result.Success);
    }

    public override void Draw(Graphics g)
    {
        Game.DrawWorldAndHud(g);
        Game.ShopMenu.Draw(g, _shop, _stock, _selected, Game.Player.Wallet.Gold, _hover);
        Game.Toasts.Draw(g);
    }
}
