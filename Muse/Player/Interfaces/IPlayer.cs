namespace Muse.Player.Interfaces;

public interface IPlayer
{
    void Load(string fileName);
    void Play();
    void Pause();
    void Stop();
    void SetVolume(int percent);
}