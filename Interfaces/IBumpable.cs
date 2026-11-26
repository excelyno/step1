namespace FarmGame.Interfaces;

/// <summary>Kontrak (MVP 5): benda yang bereaksi kalau ditabrak (semak bergoyang).</summary>
public interface IBumpable
{
    void OnBumped(Character by);
}
