namespace Muse.Utils;

public interface IMusicLibraryService
{
    IReadOnlyList<FileInfo> Scan(string directoryPath, bool includeSubfolders = true);
}
