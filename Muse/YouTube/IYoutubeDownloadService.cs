using Muse.Utils;

namespace Muse.YouTube;

public interface IYoutubeDownloadService
{
    Task<Result> DownloadAsync(string link, string? name = null);
}