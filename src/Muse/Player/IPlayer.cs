using Muse.Utils;
using NAudio.Wave;

namespace Muse.Player;

public interface IPlayer
{
    public PlaybackState State { get; }
    Result Load(string fileName);
    Result Play();
    Result Pause();
    Result Stop();
    Result SetVolume(int percent);
    Result<SongInfo> GetSongInfo();
    Result ChangeCurrentSongTime(int seconds);
    void Dispose();
}