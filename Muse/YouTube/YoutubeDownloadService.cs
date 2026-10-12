using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using Muse.Utils;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Muse.YouTube;

public interface IYoutubeDownloadService
{
    Task<Result> DownloadAsync(string link, string? name = null, string relativePath = "", IProgress<double>? progress = null, CancellationToken cancellationToken = default);
    Task<Result> DownloadPlaylistAsync(string link, string relativePath = "", IProgress<int>? progress = null, CancellationToken cancellationToken = default);
}

public class YoutubeDownloadService : IYoutubeDownloadService
{
    private readonly IYoutubeGateway youtubeGateway;
    private readonly ILogger<YoutubeDownloadService> logger;

    public YoutubeDownloadService(ILogger<YoutubeDownloadService> logger, IYoutubeGateway youtubeGateway)
    {
        this.logger = logger;
        this.youtubeGateway = youtubeGateway;
        try
        {
            Core.Initialize();
        }
        catch { }
    }

    public async Task<Result> DownloadAsync(string link, string? name = null, string relativePath = "", IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var videoId = VideoId.TryParse(link);
            if (videoId is null)
            {
                return Result.Fail($"Failed to parse Video ID from link: {link}");
            }

            IVideo video;
            try
            {
                video = await youtubeGateway.GetVideoAsync(videoId.Value, cancellationToken);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Failed to retrieve video metadata for {videoId.Value}: {ex.Message}");
            }

            return await DownloadVideoInternalAsync(video, name, relativePath, progress, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "YouTube download failed for {Link}.", link);
            return Result.Fail($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public async Task<Result> DownloadPlaylistAsync(string link, string relativePath = "", IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var playlistId = PlaylistId.TryParse(link);
            if (playlistId is null)
            {
                return Result.Fail($"Failed to parse Playlist ID from link: {link}");
            }

            // Fetch playlist videos
            var videos = youtubeGateway.GetPlaylistVideosAsync(playlistId.Value, cancellationToken);

            int current = 0;
            int failed = 0;
            progress?.Report(current);
            await foreach (var video in videos)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await DownloadVideoInternalAsync(video, null, relativePath, null, cancellationToken);
                if (result.Success)
                {
                    current++;
                    progress?.Report(current);
                }
                else
                {
                    failed++;
                    logger.LogWarning("Failed to download video {VideoId} from playlist {PlaylistId}.", video.Id, playlistId.Value);
                }
            }

            if (current == 0 && failed == 0)
            {
                return Result.Fail("The YouTube playlist contains no downloadable videos.");
            }

            if (failed > 0)
            {
                return Result.Fail($"Downloaded {current} video{(current == 1 ? "" : "s")}; {failed} failed.");
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "YouTube playlist download failed for {Link}.", link);
            return Result.Fail($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task<Result> DownloadVideoInternalAsync(
        IVideo video,
        string? name,
        string relativePath,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        string? outputPath = null;
        try
        {
            if (string.IsNullOrWhiteSpace(Globals.MuseDirectory))
            {
                return Result.Fail("MUSE_DIRECTORY is not set.");
            }

            string baseDirectory = Path.GetFullPath(Globals.MuseDirectory);
            string baseDirWithSeparator = baseDirectory.EndsWith(Path.DirectorySeparatorChar)
                ? baseDirectory
                : baseDirectory + Path.DirectorySeparatorChar;

            string targetDirectory;
            try
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    targetDirectory = baseDirectory;
                }
                else
                {
                    string combinedPath = Path.Combine(baseDirectory, relativePath);
                    string fullTargetDirectory = Path.GetFullPath(combinedPath);

                    // Ensure the target directory stays within the base directory
                    bool isSubDir = fullTargetDirectory.StartsWith(baseDirWithSeparator, StringComparison.OrdinalIgnoreCase);
                    bool isBaseDir = string.Equals(fullTargetDirectory, baseDirectory, StringComparison.OrdinalIgnoreCase);

                    if (!isSubDir && !isBaseDir)
                    {
                        throw new ArgumentException($"Invalid relative path: {relativePath}. Target {fullTargetDirectory} is outside base {baseDirectory}");
                    }

                    targetDirectory = fullTargetDirectory;
                }
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }
            }
            catch (Exception ex)
            {
                return Result.Fail($"Failed to prepare output directory: {ex.Message}");
            }

            StreamManifest manifest;
            try
            {
                manifest = await youtubeGateway.GetManifestAsync(video.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Failed to retrieve stream manifest for {video.Id}: {ex.Message}");
            }

            var audioStream = manifest
                .GetAudioOnlyStreams()
                .OrderByDescending(s => s.Container == Container.Mp4)
                .ThenByDescending(s => s.AudioCodec.Equals("aac", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(s => s.Bitrate)
                .FirstOrDefault();

            if (audioStream is null)
            {
                return Result.Fail("No audio-only stream found for this video.");
            }

            string fileName = string.IsNullOrWhiteSpace(name) ? video.Title : name;
            fileName = SanitizeFileName(fileName);

            // Use .m4a extension for MP4 audio-only streams, as it's more widely recognized as audio.
            string extension = audioStream.Container == Container.Mp4 ? "m4a" : audioStream.Container.Name;

            outputPath = Path.Combine(targetDirectory, $"{fileName}.{extension}");
            outputPath = GetUniqueFilePath(outputPath);

            try
            {
                await youtubeGateway.DownloadAsync(audioStream, outputPath, progress, cancellationToken);
            }
            catch (Exception ex)
            {
                // The download may have already created a partial/empty file on disk before
                // throwing (e.g. connection reset mid-transfer) - remove it so it isn't left
                // behind as a broken "song".
                if (File.Exists(outputPath))
                {
                    try { File.Delete(outputPath); } catch { }
                }
                return Result.Fail($"Failed to download audio stream to {outputPath}: {ex.Message}");
            }

            // Validation: Ensure the file is playable
            var validationResult = await ValidateAudioFile(outputPath);
            if (validationResult.IsFailure)
            {
                try
                {
                    if (File.Exists(outputPath))
                    {
                        File.Delete(outputPath);
                    }
                }
                catch
                {
                    // Ignore deletion errors
                }
                return validationResult;
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            if (outputPath is not null && File.Exists(outputPath))
            {
                try { File.Delete(outputPath); } catch { }
            }
            return Result.Fail($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<Result> ValidateAudioFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Result.Fail("Downloaded file does not exist.");
        }

        if (new FileInfo(filePath).Length == 0)
        {
            return Result.Fail("Downloaded file is empty.");
        }

        try
        {
            using var libvlc = new LibVLC();
            using var media = new Media(libvlc, filePath, FromType.FromPath);

            // Parse is asynchronous - it must be awaited before Duration is populated,
            // otherwise Duration reads as unset (<= 0) even for a perfectly valid file.
            var status = await media.Parse(MediaParseOptions.ParseLocal, 15000);
            if (status != MediaParsedStatus.Done)
            {
                return Result.Fail($"Failed to parse downloaded file (status: {status}).");
            }

            if (media.Duration <= 0)
            {
                return Result.Fail("Downloaded file has zero duration.");
            }
        }
        catch (Exception ex)
        {
            return Result.Fail($"Downloaded file is not a valid audio file or is corrupted: {ex.Message}");
        }

        return Result.Ok();
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
