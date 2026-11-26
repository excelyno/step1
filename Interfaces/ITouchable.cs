namespace FarmGame.Interfaces;

/// <summary>Kontrak (MVP 5): benda yang bisa diinjak dan bereaksi (rumput liar & bunga bergoyang, daun beterbangan).</summary>
public interface ITouchable
{
    void OnTouched(Character by);
}
