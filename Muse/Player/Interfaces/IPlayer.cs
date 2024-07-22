using Muse.Player.Utils;

namespace Muse.Player.Interfaces;

public interface IPlayer
{
    Result Load(string fileName);
    Result Play();
    Result Pause();
    Result Stop();
    Result SetVolume(int percent);
    Result<SongInfo> GetSongInfo();
}