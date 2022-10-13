using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using Muse.Player.Interfaces;
using Muse.Player.Utils;
using Timer = System.Timers.Timer;

namespace Muse.Player.Players;

public class WindowsPlayer : IPlayer
{
    [DllImport("winmm.dll")]
    private static extern int mciSendString(string command, StringBuilder stringReturn, int returnLength,
        IntPtr handleCallback);

    [DllImport("winmm.dll")]
    private static extern int mciGetErrorString(int errorCode, StringBuilder errorText, int errorTextSize);

    [DllImport("winmm.dll")]
    public static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

    private Timer playbackTimer;
    private Stopwatch playStopwatch;
    private string fileName;

    public event EventHandler PlaybackFinished;

    public bool Playing { get; private set; }
    public bool Paused { get; private set; }

    public Task Play(string fileName)
    {
        FileUtil.ClearTempFiles();
        this.fileName = fileName;
        playbackTimer = new Timer
        {
            AutoReset = false
        };
        playStopwatch = new Stopwatch();

        ExecuteMsiCommand("Close All");
        ExecuteMsiCommand($"Status {this.fileName} Lenght");
        ExecuteMsiCommand($"Play {this.fileName}");
        Paused = false;
        Playing = true;
        playbackTimer.Elapsed += HandlePlaybackFinished;
        playbackTimer.Start();
        playStopwatch.Start();

        return Task.CompletedTask;
    }

    public Task Pause()
    {
        if (Playing && !Paused)
        {
            ExecuteMsiCommand($"Resume {this.fileName}");
            Paused = true;
            playbackTimer.Stop();
            playStopwatch.Stop();
            playbackTimer.Interval -= playStopwatch.ElapsedMilliseconds;
        }
        return Task.CompletedTask;
    }

    public Task Resume()
    {
        if (Playing && Paused)
        {
            ExecuteMsiCommand($"Resume {this.fileName}");
            Paused = false;
            playbackTimer.Start();
            playStopwatch.Reset();
            playStopwatch.Start();
        }

        return Task.CompletedTask;
    }

    public Task Stop()
    {
        if (Playing && Paused)
        {
            ExecuteMsiCommand($"Resume {this.fileName}");
            Paused = false;
            playbackTimer.Start();
            playStopwatch.Reset();
            playStopwatch.Start();
        }
        
        return Task.CompletedTask;
    }

    private void HandlePlaybackFinished(object sender, ElapsedEventArgs e)
    {
        Playing = false;
        PlaybackFinished?.Invoke(this, e);
        playbackTimer.Dispose();
        playbackTimer = null;
    }

    private Task ExecuteMsiCommand(string commandString)
    {
        var sb = new StringBuilder();
        var result = mciSendString(commandString, sb, 1024 * 1024, IntPtr.Zero);
        if (result is not 0)
        {
            var errorSb = new StringBuilder($"Error executing MCI command '{commandString}'. Error code: {result}.");
            var sb2 = new StringBuilder(128);
            mciGetErrorString(result, sb2, 128);
            errorSb.Append($":Message: {sb2}");
            throw new Exception(errorSb.ToString());
        }

        if (commandString.ToLower().StartsWith("status") && int.TryParse(sb.ToString(), out var length))
        {
            playbackTimer.Interval = length;
        }

        return Task.CompletedTask;
    }

    public Task SetVolume(byte percent)
    {
        int newVolume = ushort.MaxValue / 100 * percent;
        uint newVolumeAllChannels = (uint)newVolume & 0x0000ffff | ((uint)newVolume << 16);
        waveOutSetVolume(IntPtr.Zero, newVolumeAllChannels);

        return Task.CompletedTask;
    }
}