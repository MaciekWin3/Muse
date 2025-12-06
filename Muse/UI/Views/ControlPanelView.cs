using Muse.UI.Bus;
using Terminal.Gui.App;
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
        uiBus.Subscribe<TogglePlayRequested>(msg =>
        {
            Application.Invoke(() =>
            {
                // Toggle the button text
                playPauseButton.Text = playPauseButton.Text == "|>" ? "||" : "|>";
            });
        });
    }

    private void RegisterButtons()
    {
        playPauseButton = new Button()
        {
            Text = "|>",
            Height = ButtonsHeight,
            X = Pos.Center(),
            ShadowStyle = ShadowStyle.None,
        };

        playPauseButton.Accepting += (s, e) =>
        {
            uiBus.Publish(new TogglePlayRequested());
            playPauseButton.Text = playPauseButton.Text == "|>" ? "||" : "|>";
            e.Handled = true;
        };

        backButton = new Button()
        {
            Text = "<",
            Height = ButtonsHeight,
            X = Pos.Left(playPauseButton) - (4 + 6),
            ShadowStyle = ShadowStyle.None,
        };

        backButton.Accepting += (s, e) =>
        {
            uiBus.Publish(new SeekRelativeRequested(-10));
            e.Handled = true;
        };

        forwardButton = new Button()
        {
            Text = ">",
            Height = ButtonsHeight,
            X = Pos.Right(playPauseButton) + 4,
            ShadowStyle = ShadowStyle.None,
        };

        forwardButton.Accepting += (s, e) =>
        {
            uiBus.Publish(new SeekRelativeRequested(+10));
            e.Handled = true;
        };

        previousSongButton = new Button()
        {
            Text = "<<",
            Height = ButtonsHeight,
            X = Pos.Left(backButton) - (4 + 6),
            ShadowStyle = ShadowStyle.None,
        };

        previousSongButton.Accepting += (s, e) =>
        {
            uiBus.Publish(new PreviousSongRequested());
            e.Handled = true;
        };

        nextSongButton = new Button()
        {
            Text = ">>",
            Height = ButtonsHeight,
            X = Pos.Right(forwardButton) + 6,
            ShadowStyle = ShadowStyle.None,
        };

        nextSongButton.Accepting += (s, e) =>
        {
            uiBus.Publish(new NextSongRequested());
            e.Handled = true;
        };

        Add(previousSongButton);
        Add(backButton);
        Add(playPauseButton);
        Add(forwardButton);
        Add(nextSongButton);
    }
}