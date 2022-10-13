using System.Diagnostics;
using System.Diagnostics.Metrics;
using Muse.Player.Interfaces;

namespace Muse.Player.Players;

public abstract class UnixPlayerBase : IPlayer
{
    private Process process;

    internal const string PauseProcessCommand = "kill -STOP {0}";
    internal const string ResumeProcessCommand = "kill -CONT {0}";

    public event EventHandler PlaybackFinished;
    public bool Playing { get; private set; }
    public bool Paused { get; private set; }

    protected abstract string GetBashCommand(string fileName);

    public async Task Play(string fileName)
    {
        await Stop();
        var BashToolName = GetBashCommand(fileName);
        process = StartBashProcess($"{BashToolName} '{fileName}'");
        process.EnableRaisingEvents = true;
        process.Exited += HandlePlaybackFinished;
        process.ErrorDataReceived += HandlePlaybackFinished;
        process.Disposed += HandlePlaybackFinished;
        Playing = true;
    }

    public Task Pause()
    {
        if (Playing && !Paused && process is not null)
        {
            var tempProcess = StartBashProcess(string.Format(PauseProcessCommand, process.Id));
            tempProcess.WaitForExit();
            Paused = true;
        }

        return Task.CompletedTask;
    }

    public Task Resume()
    {
        if (Playing && Paused && process is not null)
        {
            var tempProcess = StartBashProcess(string.Format(ResumeProcessCommand, process.Id));
            tempProcess.WaitForExit();
            Paused = false;
        }

        return Task.CompletedTask;
    }

    public Task Stop()
    {
        if (process is not null)
        {
            process.Kill();
            process.Dispose();
            Paused = false;
        }

        return Task.CompletedTask;
    }

    protected Process StartBashProcess(string command)
    {
        var escapedArgs = command.Replace("\"", "\\\"");
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{escapedArgs}\"",
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        return process;
    }

    internal void HandlePlaybackFinished(object sender, EventArgs e)
    {
        if (Playing)
        {
            Playing = false;
            PlaybackFinished?.Invoke(this, e); 
        }
    }

    public abstract Task SetVolume(byte percent);
}