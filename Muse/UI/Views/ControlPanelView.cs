using Muse.UI.Bus;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI.Views;

public class ControlPanelView : FrameView
{
    private readonly IUiEventBus uiBus;

    private const int ButtonsFrameHeight = 3;
    private const int ButtonsHeight = 2;

    private Button playPauseButton = null!;
    private Button forwardButton = null!;
    private Button backButton = null!;
    private Button nextSongButton = null!;
    private Button previousSongButton = null!;

    public ControlPanelView(IUiEventBus uiBus, Pos x, Pos y)
    {
        this.uiBus = uiBus;
        X = x;
        Y = y;

        Title = "Controls";
        Width = Dim.Fill();
        Height = ButtonsFrameHeight;
        RegisterButtons();
        RegisterBusHandlers();
    }

    private void RegisterBusHandlers()
    {
        uiBus.Subscribe<PlayRequested>(msg =>
        {
            App?.Invoke(() =>
            {
                playPauseButton.Text = "Pause";
            });
        });

        uiBus.Subscribe<PauseRequested>(msg =>
        {
            App?.Invoke(() =>
            {
                playPauseButton.Text = "Play";
            });
        });
    }

    private void RegisterButtons()
    {
        playPauseButton = new Button { Text = "Play", Height = ButtonsHeight, X = Pos.Center() };

        backButton = new Button { Text = "-10s", Height = ButtonsHeight, X = Pos.Left(playPauseButton) - 10 };
        forwardButton = new Button { Text = "+10s", Height = ButtonsHeight, X = Pos.Right(playPauseButton) + 4 };

        previousSongButton = new Button { Text = "Prev", Height = ButtonsHeight, X = Pos.Left(backButton) - 10 };
        nextSongButton = new Button { Text = "Next", Height = ButtonsHeight, X = Pos.Right(forwardButton) + 6 };

        playPauseButton.Accepting += (s, e) => { uiBus.Publish(new TogglePlayRequested()); e.Handled = true; };
        backButton.Accepting += (s, e) => { uiBus.Publish(new SeekRelativeRequested(-10)); e.Handled = true; };
        forwardButton.Accepting += (s, e) => { uiBus.Publish(new SeekRelativeRequested(10)); e.Handled = true; };
        previousSongButton.Accepting += (s, e) => { uiBus.Publish(new PreviousSongRequested()); e.Handled = true; };
        nextSongButton.Accepting += (s, e) => { uiBus.Publish(new NextSongRequested()); e.Handled = true; };

        Add(previousSongButton, backButton, playPauseButton, forwardButton, nextSongButton);
    }
}