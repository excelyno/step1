namespace FarmGame.Interfaces;

/// <summary>Kontrak: sesuatu yang bisa digambar di dunia game.</summary>
public interface IDrawable
{
    /// <summary>Urutan gambar. Nilai kecil digambar duluan (berada di belakang).</summary>
    int DrawOrder { get; }

    void Draw(Graphics g, IAssetProvider assets);
}
