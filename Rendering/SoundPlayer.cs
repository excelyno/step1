using System.Media;

namespace FarmGame.Rendering;

/// <summary>
/// Pemutar efek suara (MVP 6): file WAV kecil di Assets/SFX (dibuat Tools/generate_assets.py).
/// Diputar dengan SoundPlayer di thread lain supaya game tidak pernah menunggu suara.
/// Semua file dimuat sekali saat game mulai; nama yang tidak ada = diabaikan (tidak crash).
/// </summary>
public class SoundPlayer
{
    private readonly Dictionary<string, System.Media.SoundPlayer> _sounds = new();
    private readonly Random _rng = new();
    private float _lastPlayed;   // cegah 2 suara sama berturut-turut dalam 0,03 detik (klik dobel)

    public SoundPlayer()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "Assets", "SFX");
        if (!Directory.Exists(folder))
            return;

        foreach (string file in Directory.GetFiles(folder, "*.wav"))
            _sounds[Path.GetFileNameWithoutExtension(file)] = new System.Media.SoundPlayer(file);
    }

    public bool HasAny => _sounds.Count > 0;

    /// <summary>Putar suara by name ("hoe", "coin", ...). Aman dipanggil dari mana saja.</summary>
    public void Play(string name, float pitch = 1f)
    {
        if (_sounds.Count == 0)
            return;

        // jitter: nama + variasi pitch (dibulatkan) supaya suara tidak monoton
        int variant = pitch < 0.98f ? 0 : pitch > 1.02f ? 2 : 1;
        string key = name;
        if (variant != 1 && _sounds.ContainsKey(name + "_p" + variant))
            key = name + "_p" + variant;

        if (Environment.TickCount64 - _lastPlayed < 30 && key == _lastName)
            return;
        _lastName = key;
        _lastPlayed = Environment.TickCount64;

        if (!_sounds.TryGetValue(key, out System.Media.SoundPlayer? player))
            return;

        try
        {
            player.Play();   // async internal SoundPlayer: tidak memblok game loop
        }
        catch
        {
            // suara gagal diputar (perangkat sibuk): abaikan, game tetap jalan
        }
    }

    private string _lastName = "";

    /// <summary>Play dengan pitch acak sedikit berbeda (untuk aksi berulang seperti cangkul).</summary>
    public void PlayVaried(string name) => Play(name, 0.9f + (float)_rng.NextDouble() * 0.2f);
}
