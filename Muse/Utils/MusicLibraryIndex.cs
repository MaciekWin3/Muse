using Muse.Player;

namespace Muse.Utils;

public sealed class MusicLibraryIndex(IMusicLibraryService musicLibrary) : IMusicLibraryIndex
{
    private readonly Dictionary<string, IReadOnlyList<Track>> cache = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Track> GetTracks(string directoryPath, bool includeSubfolders = true)
    {
        var key = CreateKey(directoryPath, includeSubfolders);
        if (cache.TryGetValue(key, out var tracks))
        {
            return tracks;
        }

        tracks = musicLibrary.Scan(directoryPath, includeSubfolders)
            .Select(Track.FromFileInfo)
            .ToList();
        cache[key] = tracks;
        return tracks;
    }

    public void Invalidate(string? directoryPath = null)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            cache.Clear();
            return;
        }

        var normalizedDirectory = Normalize(directoryPath);
        foreach (var key in cache.Keys
                     .Where(key => key.StartsWith(normalizedDirectory + "|", StringComparison.OrdinalIgnoreCase))
                     .ToList())
        {
            cache.Remove(key);
        }
    }

    private static string CreateKey(string directoryPath, bool includeSubfolders) =>
        $"{Normalize(directoryPath)}|{includeSubfolders}";

    private static string Normalize(string directoryPath) =>
        Path.GetFullPath(directoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
