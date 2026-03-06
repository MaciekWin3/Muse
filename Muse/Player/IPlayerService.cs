using Muse.Utils;
using NAudio.Wave;

namespace Muse.Player;

public interface IPlayerService
{
    public Track? CurrentTrack { get; }
    public string? CurrentFilePath { get; }
    public PlaybackState State { get; }
    Task<Result> Load(Track track);
    Result Play();
    Result Pause();
    Result Stop();
    Result SetVolume(float volume);
    Result<SongInfo> GetSongInfo();
    Result ChangeCurrentSongTime(int seconds);
    void Dispose();
}