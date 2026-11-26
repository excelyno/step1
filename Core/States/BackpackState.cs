namespace FarmGame.Core.States;

/// <summary>
/// MVP 9: mode lihat tas (tombol B). Dunia tetap tergambar di belakang, jam berhenti.
/// Pilih slot (klik / arah / 1..9), X makan atau periksa barang, B/Esc keluar.
/// </summary>
public class BackpackState : GameState
{
    private readonly BackpackMenu _menu;
    private int _hover = -1;

    public BackpackState(Game game) : base(game)
    {
        _menu = new BackpackMenu(game.Assets);
    }

    public override void Enter()
    {
        Game.Time.Pause();
        Game.Player.SetMovementIntent(Vector2.Zero, false);
    }

    public override void Update(float deltaSeconds)
    {
        InputManager input = Game.Input;
        Inventory inventory = Game.Player.Inventory;
        Game.Toasts.Update(deltaSeconds);

        _hover = BackpackMenu.HitTest(input.MousePosition, inventory.Capacity);

        // Keluar
        if (input.WasPressed(Keys.B) || input.WasPressed(Keys.Escape) || input.MenuClosePressed)
        {
            Game.PlaySound("ui_close");
            Game.ChangeState(new PlayingState(Game));
            return;
        }

        // Pilih slot: klik, arah, roda mouse, angka 1..9 & 0
        if (input.MouseDownPressed(MouseButtons.Left) && _hover >= 0)
        {
            inventory.Select(_hover);
            Game.PlaySound("select");
            return;
        }
        if (input.MenuUpPressed) inventory.SelectNext(-1);
        if (input.MenuDownPressed) inventory.SelectNext(1);
        if (input.WheelDirection != 0) inventory.SelectNext(Math.Sign(input.WheelDirection));

        for (int i = 0; i < inventory.Capacity; i++)
        {
            Keys key = i switch
            {
                9 => Keys.D0,
                < 9 => Keys.D1 + i,
                _ => Keys.None,
            };
            if (key != Keys.None && input.WasPressed(key))
            {
                inventory.Select(i);
                Game.PlaySound("select");
                break;
            }
        }

        // X = periksa / makan (pakai logika Interact pemain; dunia di belakang tetap aman)
        if (input.InteractPressed || input.WasPressed(Keys.Enter))
        {
            Item? item = inventory.SelectedItem;
            if (item is Food food)
            {
                ActionResult result = food.Eat(Game.Player);
                if (result.Success)
                {
                    inventory.RemoveFromSlot(inventory.SelectedIndex, 1);
                    Game.PlaySound("eat");
                    Game.Toasts.Push(result.Message ?? $"+{food.EnergyRestore} energi", true);
                }
            }
            else if (item != null)
            {
                Game.Toasts.Push(item.Description, true);
            }
        }
    }

    public override void Draw(Graphics g)
    {
        Game.DrawWorldAndHud(g);
        _menu.Draw(g, Game.Player.Inventory, Game.Player.Inventory.SelectedIndex, _hover);
        Game.Toasts.Draw(g);
    }
}
