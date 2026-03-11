namespace Muse.Utils;

public static class MusicListHelper
{
    public static IEnumerable<FileInfo> GetMusicList(string directoryPath, bool includeSubfolders = true)
    {
        var directory = new DirectoryInfo(directoryPath);

        if (!directory.Exists)
        {
            yield break;
        }

        var searchOption = includeSubfolders
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var extensions = new[] { "*.mp3", "*.mp4", "*.m4a", "*.webm" };
        var files = extensions.SelectMany(ext => directory.GetFiles(ext, searchOption));

        foreach (var file in files.OrderBy(f => f.Name).ThenBy(f => f.FullName))
        {
            if (seenPaths.Add(file.FullName))
            {
                yield return file;
            }
        }
    }
}
