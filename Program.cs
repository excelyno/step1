using FarmGame.UI;

namespace FarmGame;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            Application.Run(new GameForm());
        }
        catch (MapFormatException ex)
        {
            // Map rusak / tidak ada: tampilkan pesan yang jelas, jangan crash diam-diam.
            MessageBox.Show(ex.Message, "Gagal memuat map", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
