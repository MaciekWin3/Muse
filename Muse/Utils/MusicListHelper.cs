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

        foreach (var file in directory.GetFiles("*.mp*", searchOption))
        {
            if (seenPaths.Add(file.FullName))
            {
                yield return file;
            }
        }
    }
}
