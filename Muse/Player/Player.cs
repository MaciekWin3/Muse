using Muse.Player.Interfaces;
using NAudio.Wave;

namespace Muse.Player;

public class Player : IPlayer, IDisposable
{
    private readonly IWavePlayer waveOutDevice;
    private AudioFileReader audioFileReader = null!;
    public PlaybackState State => waveOutDevice.PlaybackState;

    public Player()
    {
        waveOutDevice = new WaveOutEvent();
    }

    public void Load(string fileName)
    {
        if (audioFileReader is not null)
        {
            audioFileReader?.Dispose();
            waveOutDevice.Stop();
        }
        audioFileReader = new AudioFileReader(fileName);
        waveOutDevice.Init(audioFileReader);
    }

    public void Play()
    {
        waveOutDevice.Play();
    }

    public void Pause()
    {
        waveOutDevice.Pause();
    }

    public void Stop()
    {
        if (audioFileReader is not null)
        {
            audioFileReader.Position = 0;
        }
    }

    public void SetVolume(int percent)
    {
        float volume = (float)Math.Max(0.0, Math.Min(1.0, percent / 10.0));
        if (audioFileReader is not null)
        {
            audioFileReader.Volume = volume;
        }
    }

    public void Dispose()
    {
        waveOutDevice?.Stop();
        waveOutDevice?.Dispose();
        audioFileReader?.Dispose();
    }
}