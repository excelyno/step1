namespace FarmGame.Rendering;

/// <summary>Info untuk developer (tekan F3): FPS, posisi, tile, garis grid, dan kotak hitbox merah.</summary>
public class DebugOverlay
{
    private readonly Font _font = new("Consolas", 10f);

    public bool IsVisible { get; private set; }

    public void Toggle() => IsVisible = !IsVisible;

    public void Draw(Graphics g, Game game)
    {
        DrawGridAndHitbox(g, game);
        DrawInfoPanel(g, game);
    }

    /// <summary>Grid + hitbox, digambar dalam koordinat dunia (ikut skala dan kamera).</summary>
    private void DrawGridAndHitbox(Graphics g, Game game)
    {
        GameLocation location = game.CurrentLocation;
        RectangleF view = game.Camera.ViewArea;
        int size = GameConfig.TileSize;

        GraphicsState state = g.Save();
        g.ScaleTransform(GameConfig.Scale, GameConfig.Scale);
        g.TranslateTransform(-MathF.Round(view.X), -MathF.Round(view.Y));

        using Pen gridPen = new(Color.FromArgb(60, 255, 255, 255), 1f / GameConfig.Scale);
        using Pen hitboxPen = new(Color.Red, 1f / GameConfig.Scale);

        int x0 = Math.Max(0, TileMap.WorldToTile(view.Left));
        int y0 = Math.Max(0, TileMap.WorldToTile(view.Top));
        int x1 = Math.Min(location.Map.Width, TileMap.WorldToTile(view.Right) + 1);
        int y1 = Math.Min(location.Map.Height, TileMap.WorldToTile(view.Bottom) + 1);

        for (int x = x0; x <= x1; x++)
            g.DrawLine(gridPen, x * size, y0 * size, x * size, y1 * size);
        for (int y = y0; y <= y1; y++)
            g.DrawLine(gridPen, x0 * size, y * size, x1 * size, y * size);

        foreach (GameObject obj in location.Objects)
        {
            RectangleF b = obj.Bounds;
            g.DrawRectangle(hitboxPen, b.X, b.Y, b.Width, b.Height);
        }

        g.Restore(state);
    }

    /// <summary>Panel teks di pojok kiri-atas, dalam koordinat layar.</summary>
    private void DrawInfoPanel(Graphics g, Game game)
    {
        Player player = game.Player;
        string[] lines =
        {
            $"FPS      : {game.Fps}",
            $"Lokasi   : {game.CurrentLocation.Name}",
            $"Posisi   : {player.Position.X:0.0}, {player.Position.Y:0.0}",
            $"Tile     : {player.TilePosition.X}, {player.TilePosition.Y}",
            $"Arah     : {player.Facing}",
            $"Bergerak : {player.IsMoving}",
            $"Objek    : {game.CurrentLocation.Objects.Count}",
        };

        const int pad = 8, lineHeight = 16;
        using SolidBrush panel = new(Color.FromArgb(160, 0, 0, 0));
        g.FillRectangle(panel, pad, pad, 230, lines.Length * lineHeight + pad * 2);

        for (int i = 0; i < lines.Length; i++)
            g.DrawString(lines[i], _font, Brushes.White, pad * 2, pad * 2 + i * lineHeight);
    }
}
