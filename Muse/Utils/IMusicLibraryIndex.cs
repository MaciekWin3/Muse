using Muse.Player;

namespace Muse.Utils;

public interface IMusicLibraryIndex
{
    IReadOnlyList<Track> GetTracks(string directoryPath, bool includeSubfolders = true);
    void Invalidate(string? directoryPath = null);
}
