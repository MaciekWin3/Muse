using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Muse.YouTube;

public interface IYoutubeGateway
{
    Task<IVideo> GetVideoAsync(VideoId videoId, CancellationToken cancellationToken);
    IAsyncEnumerable<IVideo> GetPlaylistVideosAsync(PlaylistId playlistId, CancellationToken cancellationToken);
    Task<StreamManifest> GetManifestAsync(VideoId videoId, CancellationToken cancellationToken);
    Task DownloadAsync(IStreamInfo stream, string filePath, IProgress<double>? progress, CancellationToken cancellationToken);
}
