namespace Muse.Player;

public enum TrackSource
{
    Local,
    YouTube
}

public class Track
{
    public required string Name { get; set; }
    public required string Path { get; set; } // Local path or YouTube URL
    public TrackSource Source { get; set; }
    public string? YouTubeId { get; set; }
    public int? DurationInSeconds { get; set; }
    public string? StreamUrl { get; set; }

    public static Track FromFileInfo(FileInfo fileInfo) => new()
    {
        Name = fileInfo.Name,
        Path = fileInfo.FullName,
        Source = TrackSource.Local
    };
}
