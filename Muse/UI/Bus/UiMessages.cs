namespace Muse.UI.Bus
{
    public sealed record SongSelected(string FullPath);
    public sealed record PlayRequested();
    public sealed record PauseRequested();
    public sealed record TogglePlayRequested();
    public sealed record SeekRelativeRequested(int Seconds);
    public sealed record SeekToRequested(int Seconds);
    public sealed record PreviousSongRequested();
    public sealed record NextSongRequested();
    public sealed record VolumeChanged(float Volume);
    public sealed record MuteToggle(bool IsMuted);
    public sealed record ReloadPlaylist(string DirectoryPath);
    public sealed record ChangeSongIndexRequested(int Offset);
    public sealed record TrackProgress(string Name, int CurrentSeconds, int TotalSeconds);
    public sealed record PlaylistUpdated(List<string> Names);
}
