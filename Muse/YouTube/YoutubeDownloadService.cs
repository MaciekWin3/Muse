using Muse.Utils;
using Muse.YouTube;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Playlists;
using LibVLCSharp.Shared;

public class YoutubeDownloadService : IYoutubeDownloadService
{
    private readonly YoutubeClient youtubeClient;

    public YoutubeDownloadService()
    {
        youtubeClient = new YoutubeClient();
        try
        {
            Core.Initialize();
        }
        catch { }
    }

    public async Task<Result> DownloadAsync(string link, string? name = null, string relativePath = "", IProgress<double>? progress = null)
    {
        try
        {
            var videoId = VideoId.TryParse(link);
            if (videoId == null)
            {
                return Result.Fail($"Failed to parse Video ID from link: {link}");
            }

            IVideo video;
            try
            {
                video = await youtubeClient.Videos.GetAsync(videoId.Value);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Failed to retrieve video metadata for {videoId.Value}: {ex.Message}");
            }

            return await DownloadVideoInternalAsync(video, name, relativePath, progress);
        }
        catch (Exception ex)
        {
            return Result.Fail($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    public async Task<Result> DownloadPlaylistAsync(string link, string relativePath = "", IProgress<int>? progress = null)
    {
        try
        {
            var playlistId = PlaylistId.TryParse(link);
            if (playlistId == null)
            {
                return Result.Fail($"Failed to parse Playlist ID from link: {link}");
            }

            // Fetch playlist videos
            var videos = youtubeClient.Playlists.GetVideosAsync(playlistId.Value);
            
            int current = 0;
            progress?.Report(current);
            await foreach (var video in videos)
            {
                var result = await DownloadVideoInternalAsync(video, null, relativePath, null);
                if (result.Success)
                {
                    current++;
                    progress?.Report(current);
                }
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task<Result> DownloadVideoInternalAsync(IVideo video, string? name, string relativePath, IProgress<double>? progress)
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
                manifest = await youtubeClient.Videos.Streams.GetManifestAsync(video.Id);
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
                await youtubeClient.Videos.Streams.DownloadAsync(audioStream, outputPath, progress);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Failed to download audio stream to {outputPath}: {ex.Message}");
            }

            // Validation: Ensure the file is playable
            var validationResult = ValidateAudioFile(outputPath);
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
            if (outputPath != null && File.Exists(outputPath))
            {
                try { File.Delete(outputPath); } catch { }
            }
            return Result.Fail($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static Result ValidateAudioFile(string filePath)
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
            media.Parse(MediaParseOptions.ParseLocal);
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
