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

    public ProgressBarView(IUiEventBus uiEventBus, IPlayerService player, Pos x, Pos y)
    {
        this.uiEventBus = uiEventBus;
        this.player = player;

        X = x;
        Y = y;
        Height = 3;
        Width = Dim.Fill();

        Title = "Progress";
        BorderStyle = LineStyle.Rounded;
        ProgressBarStyle = ProgressBarStyle.Continuous;
        Fraction = 0;

        RegisterBusHandlers();
        RegisterMouseHandler();
    }

    private void RegisterBusHandlers()
    {
        uiEventBus.Subscribe<TrackProgress>(msg =>
        {
            Application.Invoke(() =>
            {
                if (msg.TotalSeconds > 0)
                {
                    Fraction = (float)msg.CurrentSeconds / msg.TotalSeconds;
                }
                else
                {
                    Fraction = 0;
                }

                Title = $"Playing: {msg.Name}{FormatTime(msg.CurrentSeconds, msg.TotalSeconds)}";
            });
        });
    }

    private void RegisterMouseHandler()
    {
        Application.Mouse.MouseEvent += (sender, e) =>
        {
            if (e.View != this)
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

    private static string FormatTime(int current, int total)
    {
        static string Format(int s) => $"{s / 60}:{s % 60:00}";
        return $" {Format(current)} / {Format(total)}";
    }
}
