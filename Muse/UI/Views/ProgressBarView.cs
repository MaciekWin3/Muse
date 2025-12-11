using Muse.Player;
using Muse.UI.Bus;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI.Views;

public sealed class ProgressBarView : ProgressBar
{
    private readonly IUiEventBus uiEventBus;
    private readonly IPlayerService player;

    private const int ProgressBarHeight = 3;
    private const int RefreshIntervalMs = 16; // ~60 FPS

    private int currentSeconds;
    private int totalSeconds;
    private string? currentSongName;

    public ProgressBarView(IUiEventBus uiEventBus, IPlayerService player, Pos x, Pos y)
    {
        this.uiEventBus = uiEventBus;
        this.player = player;

        X = x;
        Y = y;
        Height = ProgressBarHeight;
        Width = Dim.Fill();

        Title = "Progress";
        BorderStyle = LineStyle.Rounded;
        ProgressBarStyle = ProgressBarStyle.Continuous;
        Fraction = 0;

        RegisterBusHandlers();
        RegisterMouseHandler();
        StartTimer();
    }

    private void RegisterBusHandlers()
    {
        uiEventBus.Subscribe<TrackProgress>(msg =>
        {
            currentSongName = msg.Name;
            currentSeconds = msg.CurrentSeconds;
            totalSeconds = msg.TotalSeconds;

            if (currentSeconds >= totalSeconds && totalSeconds > 0)
            {
                uiEventBus.Publish(new NextSongRequested());
            }
        });
    }

    private void RegisterMouseHandler()
    {
        Application.Mouse.MouseEvent += (sender, e) =>
        {
            if (e.View is not ProgressBar)
                return;

            if (e.Flags == MouseFlags.Button1Clicked)
            {
                var width = (float)e.View.Frame.Width;
                var position = (float)e.Position.X;
                var fraction = Math.Clamp(position / width, 0f, 1f);

                Fraction = fraction;

                var info = player.GetSongInfo();
                if (info.Success)
                {
                    var newTime = (int)(fraction * info.Value.TotalTimeInSeconds);
                    player.ChangeCurrentSongTime(newTime);
                }
            }
        };
    }

    private void StartTimer()
    {
        Application.AddTimeout(TimeSpan.FromMilliseconds(RefreshIntervalMs), () =>
        {
            UpdateProgress();
            return true;
        });
    }

    private void UpdateProgress()
    {
        if (totalSeconds > 0)
        {
            Fraction = (float)currentSeconds / totalSeconds;
        }

        if (currentSongName is not null)
        {
            Title = $"Playing: {currentSongName}{FormatTime(currentSeconds, totalSeconds)}";
        }
    }

    private static string FormatTime(int current, int total)
    {
        static string Format(int s) => $"{s / 60}:{s % 60:00}";
        return $" {Format(current)} / {Format(total)}";
    }
}
