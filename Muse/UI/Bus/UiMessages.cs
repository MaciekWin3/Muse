namespace Muse.UI.Bus
{
    // UI -> App
    public sealed record SongSelected(string FullPath);
    public sealed record PlayRequested();
    public sealed record PauseRequested();
    public sealed record TogglePlayRequested();
    public sealed record SeekRelativeRequested(int Seconds); // positive or negative
    public sealed record SeekToRequested(int Seconds);
    public sealed record VolumeChanged(int Value);
    public sealed record ReloadPlaylist(string DirectoryPath);

    // App -> UI (optional)
    public sealed record PlaybackStateChanged(bool IsPlaying);
    public sealed record TrackProgress(int CurrentSeconds, int TotalSeconds, string Name);
    public sealed record PlaylistUpdated(System.Collections.Generic.List<string> Names);
}
