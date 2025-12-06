namespace Muse.UI.Bus
{
    // UI -> App
    public sealed record SongSelected(string FullPath);
    public sealed record PlayRequested();
    public sealed record PauseRequested();
    public sealed record TogglePlayRequested();
    public sealed record SeekRelativeRequested(int Seconds); // positive or negative
    public sealed record SeekToRequested(int Seconds);
    public sealed record PreviousSongRequested();
    public sealed record NextSongRequested();
    public sealed record VolumeChanged(int Value);
    public sealed record ReloadPlaylist(string DirectoryPath);

    public sealed record ChangeSongIndexRequested(int Offset); // positive or negative

    // App -> UI (optional)
    public sealed record TrackProgress(int CurrentSeconds, int TotalSeconds, string Name);
    public sealed record PlaylistUpdated(List<string> Names);
}
