using Muse.Utils;
using Muse.YouTube;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

public class YoutubeDownloadService : IYoutubeDownloadService
{
    private readonly YoutubeClient youtubeClient;

    public YoutubeDownloadService()
    {
        youtubeClient = new YoutubeClient();
    }

    public async Task<Result> DownloadAsync(string link, string? name = null)
    {
        try
        {
            var video = await youtubeClient.Videos.GetAsync(link);

            if (!Directory.Exists(Globals.MuseDirectory))
            {
                Directory.CreateDirectory(Globals.MuseDirectory);
            }

            var manifest = await youtubeClient.Videos.Streams.GetManifestAsync(link);

            var audioStream = manifest
                .GetAudioOnlyStreams()
                .Where(s => s.Container == Container.Mp4)
                .GetWithHighestBitrate();

            string fileName = name ?? video.Title;
            fileName = SanitizeFileName(fileName);
            string extension = audioStream.Container.Name;

            string outputPath = Path.Combine(Globals.MuseDirectory, $"{fileName}.{extension}");

            await youtubeClient.Videos.Streams.DownloadAsync(audioStream, outputPath);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }
}
