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

    public async Task<Result> DownloadAsync(string link, string? name = null, string relativePath = "", IProgress<double>? progress = null)
    {
        try
        {
            var video = await youtubeClient.Videos.GetAsync(link);

            string targetDirectory = string.IsNullOrWhiteSpace(relativePath) 
                ? Globals.MuseDirectory 
                : Path.Combine(Globals.MuseDirectory, relativePath);

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            var manifest = await youtubeClient.Videos.Streams.GetManifestAsync(link);

            var audioStream = manifest
                .GetAudioOnlyStreams()
                .Where(s => s.Container == Container.Mp4)
                .GetWithHighestBitrate();

            if (audioStream is null)
            {
                return Result.Fail("No audio-only stream found for this video.");
            }

            string fileName = name ?? video.Title;
            fileName = SanitizeFileName(fileName);
            string extension = audioStream.Container.Name;

            string outputPath = Path.Combine(targetDirectory, $"{fileName}.{extension}");
            outputPath = GetUniqueFilePath(outputPath);

            await youtubeClient.Videos.Streams.DownloadAsync(audioStream, outputPath, progress);

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

    private static string GetUniqueFilePath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return filePath;
        }

        string directory = Path.GetDirectoryName(filePath)!;
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);

        int count = 1;
        string newFilePath;
        do
        {
            newFilePath = Path.Combine(directory, $"{fileName} ({count}){extension}");
            count++;
        } while (File.Exists(newFilePath));

        return newFilePath;
    }
}
