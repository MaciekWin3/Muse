using System.Runtime.InteropServices;
using Muse.Player.Interfaces;
using Muse.Player.Players;

namespace Muse.Player;

public class Player : IPlayer
{
    private readonly IPlayer internalPlayer;
    public event EventHandler PlaybackFinished;

    public bool Playing => internalPlayer.Playing;
    public bool Paused => internalPlayer.Paused;

    public Player()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            internalPlayer = new WindowsPlayer();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            internalPlayer = new LinuxPlayer();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            internalPlayer = new MacPlayer();
        }
        else
        {
            throw new Exception("No implementation exists for the current OS!");
        }

        internalPlayer.PlaybackFinished += OnPlaybackFinished;
    }

    public async Task Play(string fileName)
    {
        await internalPlayer.Play(fileName);
    }

    public async Task Pause()
    {
        await internalPlayer.Pause();
    }

    public async Task Resume()
    {
        await internalPlayer.Resume();
    }

    public async Task Stop()
    {
        await internalPlayer.Stop();
    }

    private void OnPlaybackFinished(object sender, EventArgs e)
    {
        PlaybackFinished?.Invoke(this, e);
    }

    public async Task SetVolume(byte percent)
    {
        await internalPlayer.SetVolume(percent);
    }
}