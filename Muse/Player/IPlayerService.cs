using Muse.Utils;
using NAudio.Wave;

namespace Muse.Player;

public interface IPlayerService
{
    public PlaybackState State { get; }
    Result Load(string fileName);
    Result Play();
    Result Pause();
    Result Stop();
    Result SetVolume(float volume);
    Result<SongInfo> GetSongInfo();
    Result ChangeCurrentSongTime(int seconds);
    void Dispose();
}