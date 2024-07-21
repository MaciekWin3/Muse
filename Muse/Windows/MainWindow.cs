using Muse.Player.Interfaces;
using Terminal.Gui;

namespace Muse.Windows;

public class MainWindow : Window
{
    private readonly IPlayer player;
    private Label label = null!;
    private Slider volumeSlider = null!;

    public MainWindow(IPlayer player)
    {
        this.player = player;
        InitControls();
        InitStyles();
    }

    public void InitControls()
    {
        Add(InitLabel("Hello, World!"));
        Add(InitPlayPauseButton());
        Add(InitVolumeSlider());
    }

    public void InitStyles()
    {
        X = 0;
        Y = 1;
        Width = Dim.Fill();
        Height = Dim.Fill();
    }

    private Label InitLabel(string text)
    {
        label = new Label()
        {
            Text = text,
            X = Pos.Center(),
            Y = Pos.Center(),
            Height = 1,
        };
        return label;
    }

    private Slider InitVolumeSlider()
    {
        var options = new List<object> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        volumeSlider = new Slider(options)
        {
            X = Pos.Center(),
            Y = 4,
            Width = Dim.Fill(),
            Type = SliderType.Single,
        };

        volumeSlider.OptionsChanged += (sender, e) =>
        {
            var value = e.Options.FirstOrDefault().Key;
            player.SetVolume(value);
        };

        return volumeSlider;
    }

    private Button InitPlayPauseButton()
    {
        var button = new Button()
        {
            Text = "||",
            X = Pos.Center(),
            Y = Pos.Bottom(this) - 4,
            Height = 1,
        };

        button.Accept += (sender, e) =>
        {
            if (button.Text == "|>")
            {
                button.Text = "||";
                var song = @"C:\Users\macie\Music\Miszmasz\Dua Lipa - Houdini (Official Music Video).mp3";
                label.Text = "Playing: " + song;
                player.Load(song);
                player.Play();
            }
            else
            {
                button.Text = "|>";
                player.Pause();
            }
        };

        return button;
    }
}