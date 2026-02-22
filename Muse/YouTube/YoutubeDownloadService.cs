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

            string baseDirectory = Path.GetFullPath(Globals.MuseDirectory);
            string targetDirectory;
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                targetDirectory = baseDirectory;
            }
            else
            {
                string combinedPath = Path.Combine(baseDirectory, relativePath);
                string fullTargetDirectory = Path.GetFullPath(combinedPath);

                // Ensure the target directory stays within the base directory to prevent path traversal
                string baseDirWithSeparator = baseDirectory.EndsWith(Path.DirectorySeparatorChar)
                    ? baseDirectory
                    : baseDirectory + Path.DirectorySeparatorChar;

                if (!fullTargetDirectory.StartsWith(baseDirWithSeparator, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(fullTargetDirectory, baseDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Invalid relative path.");
                }

                targetDirectory = fullTargetDirectory;
            }
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
