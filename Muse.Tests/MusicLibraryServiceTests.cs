using Muse.Utils;
using Xunit;

namespace Muse.Tests;

public sealed class MusicLibraryServiceTests
{
    [Fact]
    public void ScanReturnsSupportedFilesAndHonorsRecursion()
    {
        string root = Directory.CreateTempSubdirectory("muse-library-").FullName;
        string nested = Directory.CreateDirectory(Path.Combine(root, "nested")).FullName;

        try
        {
            File.WriteAllText(Path.Combine(root, "root.mp3"), string.Empty);
            File.WriteAllText(Path.Combine(root, "ignored.txt"), string.Empty);
            File.WriteAllText(Path.Combine(nested, "nested.m4a"), string.Empty);

            var service = new MusicLibraryService();

            Assert.Single(service.Scan(root, includeSubfolders: false));
            Assert.Equal(
                ["nested.m4a", "root.mp3"],
                service.Scan(root).Select(file => file.Name).OrderBy(name => name)
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ScanReturnsEmptyForMissingDirectory()
    {
        var service = new MusicLibraryService();

        Assert.Empty(service.Scan(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
    }
}
