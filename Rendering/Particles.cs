namespace FarmGame.Rendering;

/// <summary>Jenis efek partikel. Alat memilih efeknya sendiri (lihat Tool.GetUseEffect).</summary>
public enum EffectKind
{
    None,
    Dirt,         // cangkul, tanam
    Water,        // siram
    Splash,       // isi penyiram, lempar kail, ikan tertangkap
    WoodChips,    // kapak
    StoneChips,   // beliung
    Leaves,       // panen, rumput diinjak
    Smoke,        // MVP 5: asap cerobong (naik pelan, membesar, memudar)
    Sparkle,      // MVP 5: kilau koin di air mancur, barang liar
    Ripple        // MVP 5: riak ikan di air (lingkaran membesar)
}

/// <summary>
/// Partikel kecil (MVP 4): serpihan yang terlempar lalu jatuh dan memudar,
/// plus kunang-kunang di malam hari yang ikut memancarkan cahaya.
/// Semua posisi dalam piksel logis dunia.
/// </summary>
public class ParticleSystem : ILightSource
{
    private const int MaxFireflies = 14;

    private class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Gravity;
        public float Life;
        public float MaxLife;
        public float Size;
        public Color Color;
        public bool IsFirefly;
        public float Phase;
        public bool IsSmoke;     // MVP 5: membesar dan tidak jatuh
        public bool IsRipple;    // MVP 5: digambar sebagai lingkaran
    }

    private readonly List<Particle> _particles = new();
    private readonly Random _rng = new();

    /// <summary>Hapus semua (dipanggil saat pindah lokasi).</summary>
    public void Clear() => _particles.Clear();

    public void Emit(EffectKind kind, Vector2 at)
    {
        switch (kind)
        {
            case EffectKind.Dirt:       Burst(at, 10, Color.FromArgb(214, 176, 120), 50, 170, 1.8f); break;
            case EffectKind.Water:      Burst(at, 10, Color.FromArgb(130, 190, 240), 35, 200, 1.2f); break;
            case EffectKind.Splash:     Burst(at, 14, Color.FromArgb(200, 230, 255), 55, 220, 1.4f); break;
            case EffectKind.WoodChips:  Burst(at, 9,  Color.FromArgb(196, 146, 92),  60, 260, 1.6f); break;
            case EffectKind.StoneChips: Burst(at, 9,  Color.FromArgb(170, 170, 175), 60, 280, 1.6f); break;
            case EffectKind.Leaves:     Burst(at, 10, Color.FromArgb(110, 180, 70),  40, 90,  1.8f); break;
            case EffectKind.Sparkle:    Burst(at, 8,  Color.FromArgb(255, 236, 140), 45, 40,  1.3f); break;
            case EffectKind.Smoke:      Puff(at); break;
            case EffectKind.Ripple:     _particles.Add(new Particle { Position = at, Life = 1.2f, MaxLife = 1.2f, Size = 2, IsRipple = true, Color = Color.FromArgb(220, 240, 255) }); break;
        }
    }

    /// <summary>Satu gumpal asap: naik pelan, tertiup angin ke kanan, membesar.</summary>
    private void Puff(Vector2 at)
    {
        float life = 2.2f + (float)_rng.NextDouble();
        _particles.Add(new Particle
        {
            Position = at + new Vector2((float)_rng.NextDouble() * 3 - 1.5f, 0),
            Velocity = new Vector2(4f + (float)_rng.NextDouble() * 3f, -11f - (float)_rng.NextDouble() * 4f),
            Life = life,
            MaxLife = life,
            Size = 3f,
            Color = Color.FromArgb(205, 205, 210),
            IsSmoke = true,
        });
    }

    /// <summary>Semburan: partikel terlempar ke atas ke segala arah lalu jatuh.</summary>
    private void Burst(Vector2 at, int count, Color color, float speed, float gravity, float size)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (float)(_rng.NextDouble() * Math.PI) + MathF.PI;   // setengah lingkaran atas
            float power = speed * (0.5f + (float)_rng.NextDouble() * 0.7f);
            float life = 0.35f + (float)_rng.NextDouble() * 0.35f;
            _particles.Add(new Particle
            {
                Position = at,
                Velocity = new Vector2(MathF.Cos(angle) * power, MathF.Sin(angle) * power),
                Gravity = gravity,
                Life = life,
                MaxLife = life,
                Size = size * (0.7f + (float)_rng.NextDouble() * 0.6f),
                Color = Shade(color),
            });
        }
    }

    /// <param name="fireflyArea">area kamera; null = jangan munculkan kunang-kunang (siang / dalam rumah)</param>
    public void Update(float deltaSeconds, RectangleF? fireflyArea)
    {
        foreach (Particle p in _particles)
        {
            p.Life -= deltaSeconds;
            if (p.IsFirefly)
            {
                // Melayang pelan berkelok-kelok
                p.Phase += deltaSeconds;
                p.Velocity = new Vector2(MathF.Cos(p.Phase * 0.9f) * 8f, MathF.Sin(p.Phase * 1.3f) * 6f);
            }
            else if (p.IsSmoke)
            {
                p.Size += deltaSeconds * 3.2f;
            }
            else if (p.IsRipple)
            {
                p.Size += deltaSeconds * 9f;
            }
            else
            {
                p.Velocity.Y += p.Gravity * deltaSeconds;
            }
            p.Position += p.Velocity * deltaSeconds;
        }
        _particles.RemoveAll(p => p.Life <= 0);

        if (fireflyArea != null)
            SpawnFireflies(fireflyArea.Value);
    }

    private void SpawnFireflies(RectangleF area)
    {
        int fireflies = _particles.Count(p => p.IsFirefly);
        if (fireflies >= MaxFireflies || _rng.NextDouble() > 0.05)
            return;

        float life = 4f + (float)_rng.NextDouble() * 4f;
        _particles.Add(new Particle
        {
            Position = new Vector2(area.X + (float)_rng.NextDouble() * area.Width, area.Y + (float)_rng.NextDouble() * area.Height),
            Life = life,
            MaxLife = life,
            Size = 1.5f,
            Color = Color.FromArgb(230, 255, 140),
            IsFirefly = true,
            Phase = (float)_rng.NextDouble() * 10f,
        });
    }

    /// <summary>Digambar di dalam transform dunia (sesudah objek).</summary>
    public void Draw(Graphics g)
    {
        foreach (Particle p in _particles)
        {
            float fade = Math.Clamp(p.Life / (p.IsFirefly ? 1f : p.MaxLife * 0.6f), 0f, 1f);
            if (p.IsFirefly)
                fade *= 0.6f + 0.4f * MathF.Sin(p.Phase * 6f);   // berkedip
            if (p.IsSmoke)
            {
                float alpha = Math.Clamp(p.Life / p.MaxLife, 0f, 1f) * 130f;
                // alpha dibulatkan ke 16 tingkat supaya kuas cache terpakai (MVP 6)
                int a = (int)alpha / 16 * 16;
                g.FillEllipse(ParticleBrush(Color.FromArgb(a, p.Color)),
                              p.Position.X - p.Size / 2, p.Position.Y - p.Size / 2, p.Size, p.Size);
                continue;
            }
            if (p.IsRipple)
            {
                float alpha = Math.Clamp(p.Life / p.MaxLife, 0f, 1f) * 170f;
                int a = (int)alpha / 16 * 16;
                using Pen ring = new(Color.FromArgb(a, p.Color), 0.7f);
                g.DrawEllipse(ring, p.Position.X - p.Size, p.Position.Y - p.Size / 2, p.Size * 2, p.Size);
                continue;
            }
            // alpha dibulatkan ke 16 tingkat: hanya ~16 kuas per warna partikel, bukan 1 per partikel per frame
            int fadeAlpha = (int)(255 * fade) / 16 * 16;
            g.FillRectangle(ParticleBrush(Color.FromArgb(fadeAlpha, p.Color)),
                            p.Position.X - p.Size / 2, p.Position.Y - p.Size / 2, p.Size, p.Size);
        }
    }

    private static readonly Dictionary<int, SolidBrush> ParticleBrushes = new();

    private static SolidBrush ParticleBrush(Color color)
    {
        int argb = color.ToArgb();
        if (!ParticleBrushes.TryGetValue(argb, out SolidBrush? brush))
        {
            brush = new SolidBrush(color);
            ParticleBrushes[argb] = brush;
        }
        return brush;
    }

    /// <summary>Kunang-kunang memancarkan cahaya kecil.</summary>
    public IEnumerable<Light> GetLights()
    {
        foreach (Particle p in _particles)
        {
            if (p.IsFirefly)
                yield return new Light(p.Position, 12, Color.FromArgb(220, 255, 140), 0.55f);
        }
    }

    /// <summary>Sedikit variasi warna supaya semburan tidak terlihat seragam.</summary>
    private Color Shade(Color c)
    {
        float k = 0.8f + (float)_rng.NextDouble() * 0.35f;
        return Color.FromArgb(Math.Min(255, (int)(c.R * k)), Math.Min(255, (int)(c.G * k)), Math.Min(255, (int)(c.B * k)));
    }
}
