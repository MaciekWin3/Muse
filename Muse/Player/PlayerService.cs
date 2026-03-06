using Muse.Utils;
using NAudio.Wave;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Muse.Player;

public class PlayerService : IPlayerService, IDisposable
{
    private readonly WaveOutEvent waveOutDevice;
    private WaveStream? waveStream;
    private ISampleProvider? sampleProvider;
    private float volume = 0.5f;
    private readonly YoutubeClient youtubeClient = new();

    public Track? CurrentTrack { get; private set; }
    public string? CurrentFilePath { get; private set; }
    public PlaybackState State => waveOutDevice.PlaybackState;

    public PlayerService()
    {
        waveOutDevice = new WaveOutEvent();
        volume = Globals.Volume;
    }

    public async Task<Result> Load(Track track)
    {
        try
        {
            Stop();
            waveStream?.Dispose();

            string urlToLoad = track.Path;

            if (track.Source == TrackSource.YouTube)
            {
                var manifest = await youtubeClient.Videos.Streams.GetManifestAsync(track.YouTubeId!);
                var streamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                urlToLoad = streamInfo.Url;
            }

            try
            {
                // AudioFileReader can load from URL as well if the format is supported,
                // but for YouTube streams MediaFoundationReader is safer and more robust for URLs.
                if (track.Source == TrackSource.YouTube)
                {
                    var mfReader = new MediaFoundationReader(urlToLoad);
                    waveStream = mfReader;
                    sampleProvider = mfReader.ToSampleProvider();
                }
                else
                {
                    var reader = new AudioFileReader(urlToLoad);
                    reader.Volume = volume;
                    waveStream = reader;
                    sampleProvider = reader;
                }
            }
            catch (Exception)
            {
                // Fallback to MediaFoundationReader for tricky formats or URLs
                var mfReader = new MediaFoundationReader(urlToLoad);
                waveStream = mfReader;
                sampleProvider = mfReader.ToSampleProvider();
            }

            waveOutDevice.Init(waveStream);
            CurrentTrack = track;
            CurrentFilePath = track.Path;
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to load audio: {ex.Message}");
        }
    }

    public Result Play()
    {
        if (waveStream is not null)
        {
            waveOutDevice.Play();
            return Result.Ok();
        }
        return Result.Fail("Unable to play audio file");
    }

    public Result Pause()
    {
        waveOutDevice.Pause();
        return Result.Ok();
    }

    public Result Stop()
    {
        waveOutDevice.Stop();
        if (waveStream is not null)
        {
            waveStream.Position = 0;
        }
        return Result.Ok();
    }

    public Result SetVolume(float volume)
    {
        this.volume = volume;
        Globals.Volume = volume;
        
        if (waveStream is AudioFileReader afr)
        {
            afr.Volume = volume;
            return Result.Ok();
        }
        
        // If it's not AudioFileReader (e.g. MediaFoundationReader), we'd need a VolumeSampleProvider wrapper
        // but for now we just save the global setting
        return Result.Ok();
    }

    public Result<int> GetVolume()
    {
        return Result.Ok((int)(volume * 10));
    }

    public Result<SongInfo> GetSongInfo()
    {
        if (waveStream is not null)
        {
            return Result.Ok(new SongInfo(waveStream));
        }
        return Result.Fail<SongInfo>("Unable to get song info");
    }

    public Result ChangeCurrentSongTime(int seconds)
    {
        if (waveStream is not null)
        {
            waveStream.CurrentTime = TimeSpan.FromSeconds(seconds);
            return Result.Ok();
        }
        return Result.Fail("Unable to change current song time");
    }

    public void Dispose()
    {
        waveOutDevice?.Stop();
        waveOutDevice?.Dispose();
        waveStream?.Dispose();
    }
}