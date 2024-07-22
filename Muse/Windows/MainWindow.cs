using Muse.Player.Interfaces;
using System.Collections.ObjectModel;
using Terminal.Gui;

namespace Muse.Windows;

public class MainWindow : Window
{
    private readonly IPlayer player;
    private Label label = null!;
    private Slider volumeSlider = null!;
    private ListView musicList = null!;

    public MainWindow(IPlayer player)
    {
        this.player = player;
        InitControls();
        InitStyles();
    }

    public void InitControls()
    {
        Add(InitMusicList());
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

    private ListView InitMusicList()
    {
        var x = @"C:\\Users\\macie\\Music\\Miszmasz\\";
        var items = GetMusicList(x);
        musicList = new ListView()
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(),
            Height = 5,
            Source = new ListWrapper<string>(new ObservableCollection<string>(items))
        };

        musicList.OpenSelectedItem += (sender, e) =>
        {
            var song = e.Value.ToString();
            label.Text = "Playing: " + song;
            player.Load(x + song);
            player.Play();
        };

        return musicList;
    }

    private Label InitLabel(string text)
    {
        label = new Label()
        {
            Text = text,
            X = 1,
            Y = Pos.Bottom(musicList),
            Height = 1,
        };
        return label;
    }

    private Slider InitVolumeSlider()
    {
        var options = new List<object> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        volumeSlider = new Slider(options)
        {
            X = 1,
            Y = Pos.Bottom(label),
            Width = Dim.Fill(),
            Type = SliderType.Single,
            //Orientation = Orientation.Vertical,
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

    private IEnumerable<string> GetMusicList(string directoryPath)
    {
        var d = new DirectoryInfo(directoryPath);

        FileInfo[] Files = d.GetFiles("*.mp3");

        foreach (FileInfo file in Files)
        {
            yield return file.Name;
        }
    }
}