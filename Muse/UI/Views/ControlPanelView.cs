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
    private Button repeatButton = null!;
    private Button shuffleButton = null!;

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
            Application.Invoke(() =>
            {
                playPauseButton.Text = "||";
            });
        });

        uiBus.Subscribe<PauseRequested>(msg =>
        {
            Application.Invoke(() =>
            {
                playPauseButton.Text = "|>";
            });
        });

        uiBus.Subscribe<PlayModeChanged>(msg =>
        {
            Application.Invoke(() =>
            {
                repeatButton.Text = msg.NewMode switch
                {
                    PlayMode.None => "Repeat: None",
                    PlayMode.Repeat => "Repeat: All",
                    PlayMode.RepeatOne => "Repeat: One",
                    _ => "Repeat"
                };
            });
        });

        uiBus.Subscribe<ShuffleChanged>(msg =>
        {
            Application.Invoke(() =>
            {
                shuffleButton.Text = msg.IsShuffle ? "Shuffle: On" : "Shuffle: Off";
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

        repeatButton = new Button()
        {
            Text = "Repeat: None",
            Height = ButtonsHeight,
            X = 0,
            ShadowStyle = ShadowStyle.None,
        };

        repeatButton.Accepting += (s, e) =>
        {
            uiBus.Publish(new TogglePlayModeRequested());
            e.Handled = true;
        };

        shuffleButton = new Button()
        {
            Text = "Shuffle: Off",
            Height = ButtonsHeight,
            X = Pos.AnchorEnd(15), 
            ShadowStyle = ShadowStyle.None,
        };

        shuffleButton.Accepting += (s, e) =>
        {
            uiBus.Publish(new ShuffleToggleRequested());
            e.Handled = true;
        };

        Add(repeatButton);
        Add(previousSongButton);
        Add(backButton);
        Add(playPauseButton);
        Add(forwardButton);
        Add(nextSongButton);
        Add(shuffleButton);
    }
}