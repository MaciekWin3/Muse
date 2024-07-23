using Muse.Utils;

namespace Muse.Player;

public interface IPlayer
{
    Result Load(string fileName);
    Result Play();
    Result Pause();
    Result Stop();
    Result SetVolume(int percent);
    Result<SongInfo> GetSongInfo();
    Result ChangeCurrentSongTime(int seconds);
    void Dispose();
}