using Muse.Player;
using Muse.Utils;
using Xunit;

namespace Muse.Tests;

public sealed class MusicLibraryIndexTests
{
    [Fact]
    public void ReusesCachedTracksUntilInvalidated()
    {
        using var directory = new TemporaryDirectory();
        var service = new CountingMusicLibraryService();
        var index = new MusicLibraryIndex(service);

        var first = index.GetTracks(directory.Path);
        var second = index.GetTracks(directory.Path);

        Assert.Same(first, second);
        Assert.Equal(1, service.ScanCount);

        index.Invalidate(directory.Path);
        index.GetTracks(directory.Path);

        Assert.Equal(2, service.ScanCount);
    }

    private sealed class CountingMusicLibraryService : IMusicLibraryService
    {
        public int ScanCount { get; private set; }

        public IReadOnlyList<FileInfo> Scan(string directoryPath, bool includeSubfolders = true)
        {
            ScanCount++;
            return [];
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Directory.CreateTempSubdirectory("muse-index-test-").FullName;

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
