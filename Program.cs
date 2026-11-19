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
            // farm.txt rusak / tidak ada
            MessageBox.Show(ex.Message, "Gagal memuat map", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (GameDataException ex)
        {
            // crops.json rusak / tidak ada
            MessageBox.Show(ex.Message, "Gagal memuat data", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
