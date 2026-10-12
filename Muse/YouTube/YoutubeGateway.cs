using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Muse.YouTube;

public sealed class YoutubeGateway : IYoutubeGateway
{
    private readonly YoutubeClient client = new();

    public async Task<IVideo> GetVideoAsync(VideoId videoId, CancellationToken cancellationToken) =>
        await client.Videos.GetAsync(videoId, cancellationToken);

    public IAsyncEnumerable<IVideo> GetPlaylistVideosAsync(PlaylistId playlistId, CancellationToken cancellationToken) =>
        client.Playlists.GetVideosAsync(playlistId, cancellationToken);

    public async Task<StreamManifest> GetManifestAsync(VideoId videoId, CancellationToken cancellationToken) =>
        await client.Videos.Streams.GetManifestAsync(videoId, cancellationToken);

    public async Task DownloadAsync(
        IStreamInfo stream,
        string filePath,
        IProgress<double>? progress,
        CancellationToken cancellationToken) =>
        await client.Videos.Streams.DownloadAsync(stream, filePath, progress, cancellationToken: cancellationToken);
}
