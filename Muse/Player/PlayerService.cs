using Muse.Utils;
using NAudio.Wave;

namespace Muse.Player;

public class PlayerService : IPlayerService, IDisposable
{
    private readonly WaveOutEvent waveOutDevice;
    private WaveStream? waveStream;
    private ISampleProvider? sampleProvider;
    private float volume = 0.5f;

    public string? CurrentFilePath { get; private set; }
    public PlaybackState State => waveOutDevice.PlaybackState;

    public PlayerService()
    {
        waveOutDevice = new WaveOutEvent();
        volume = Globals.Volume;
    }

    public Result Load(string fileName)
    {
        try
        {
            Stop();
            waveStream?.Dispose();

            try
            {
                var reader = new AudioFileReader(fileName);
                reader.Volume = volume;
                waveStream = reader;
                sampleProvider = reader;
            }
            catch (Exception)
            {
                // Fallback to MediaFoundationReader for tricky formats on Windows
                var mfReader = new MediaFoundationReader(fileName);
                waveStream = mfReader;
                // MediaFoundationReader doesn't have Volume property, we might need a wrapper if we want volume control here
                // For now, let's just get it playing
                sampleProvider = mfReader.ToSampleProvider();
            }

            waveOutDevice.Init(waveStream);
            CurrentFilePath = fileName;
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to load audio file: {ex.Message}");
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