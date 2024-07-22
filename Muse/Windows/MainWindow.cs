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
            player.Load(x + song);
            var songInfo = player.GetSongInfo();
            if (songInfo.Success)
            {
                label.Text = "Playing: " + song + $" {songInfo.Value.TotalTimeInSeconds}";
            }
            else
            {
                label.Text = songInfo.Error;
            }
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
        var options = new List<object> { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 90, 95, 100 };
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
            //Y = Pos.Bottom(this) - 4,
            Y = Pos.Bottom(this) - 4,
            Height = 1,
        };

        button.Accept += (sender, e) =>
        {
            if (button.Text == "|>")
            {
                button.Text = "||";
                player.Play();

                Application.AddTimeout(TimeSpan.FromSeconds(1), () =>
                {
                    var songInfo = player.GetSongInfo();
                    if (songInfo.Success)
                    {
                        label.Text = "Playing: " + songInfo.Value.Name + $" {songInfo.Value.CurrentTime}/{songInfo.Value.TotalTimeInSeconds}";
                        Application.Refresh();
                    }
                    else
                    {
                        label.Text = songInfo.Error;
                    }
                    return true;
                });

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