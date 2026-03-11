using Muse.Player;

namespace Muse.UI.Bus
{
    public sealed record SongSelected(Track Track);
    public sealed record PlayRequested();
    public sealed record PauseRequested();
    public sealed record TogglePlayRequested();
    public sealed record SeekRelativeRequested(int Seconds);
    public sealed record SeekToRequested(int Seconds);
    public sealed record PreviousSongRequested();
    public sealed record NextSongRequested();
    public sealed record VolumeChanged(float Volume);
    public sealed record MuteToggle(bool IsMuted);
    public sealed record ReloadPlaylist(string DirectoryPath, bool Recursive = false);
    public sealed record LoadYoutubePlaylist(string PlaylistUrl);
    public sealed record ChangeSongIndexRequested(int Offset);
    public sealed record TrackProgress(string Name, int CurrentSeconds, int TotalSeconds);
    public sealed record PlaylistUpdated(System.Collections.Generic.IReadOnlyList<Track> Songs);
    public sealed record RefreshPlaylistsRequested();
    public sealed record ChangeThemeRequested(string ThemeName);

    public enum PlayMode
    {
        None,
        Repeat,
        RepeatOne
    }

    public sealed record TogglePlayModeRequested();
    public sealed record ShuffleToggleRequested();
    public sealed record PlayModeChanged(PlayMode NewMode);
    public sealed record ShuffleChanged(bool IsShuffle);

    public enum AppMode
    {
        Search,
        Shortcuts
    }

    public sealed record ChangeModeRequested(AppMode NewMode);
    public sealed record DeleteSongRequested();
}
