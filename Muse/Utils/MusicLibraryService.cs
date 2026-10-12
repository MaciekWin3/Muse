namespace Muse.Utils;

public sealed class MusicLibraryService : IMusicLibraryService
{
    public IReadOnlyList<FileInfo> Scan(string directoryPath, bool includeSubfolders = true)
    {
        return MusicListHelper.GetMusicList(directoryPath, includeSubfolders).ToList();
    }
}
