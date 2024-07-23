﻿using Muse.Player.Interfaces;
using Muse.Player.Utils;
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

    public Result Load(string fileName)
    {
        if (audioFileReader is not null)
        {
            audioFileReader?.Dispose();
            waveOutDevice.Stop();
        }
        audioFileReader = new AudioFileReader(fileName);
        waveOutDevice.Init(audioFileReader);
        return Result.Ok();
    }

    public Result Play()
    {
        if (audioFileReader is not null)
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
        if (audioFileReader is not null)
        {
            audioFileReader.Position = 0;
            return Result.Ok();
        }
        return Result.Fail("Unable to stop audio file");
    }

    public Result SetVolume(int percent)
    {
        float volume = (float)Math.Max(0.0, Math.Min(1.0, percent / 10.0));
        if (audioFileReader is not null)
        {
            audioFileReader.Volume = volume;
            return Result.Ok();
        }
        return Result.Fail("Unable to set volume");
    }

    public Result<SongInfo> GetSongInfo()
    {
        if (audioFileReader is not null)
        {
            return Result.Ok(new SongInfo(audioFileReader));
        }
        return Result.Fail<SongInfo>("Unable to get song info");
    }

    public void Dispose()
    {
        waveOutDevice?.Stop();
        waveOutDevice?.Dispose();
        audioFileReader?.Dispose();
    }
}