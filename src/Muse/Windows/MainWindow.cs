using Muse.Player.Interfaces;
using System.Collections.ObjectModel;
using Terminal.Gui;

namespace Muse.Windows;

public class MainWindow : Window
{
    // TODO: Check if next/preious back/forward is avilable
    // TODO: Remove hardcoded path
    private readonly static string MUSIC_DIRECTORY = @"C:\\Users\\macie\\Music\\Miszmasz\\";

    private readonly IPlayer player;

    private ListView musicList = null!;

    // Buttons
    private Button playPauseButton = null!;
    private Button forwardButton = null!;
    private Button backButton = null!;
    private Button nextSongButton = null!;
    private Button previousSongButton = null!;

    private Label label = null!;
    private ProgressBar progressBar = null!;
    private Slider volumeSlider = null!;

    private readonly List<FileInfo> playlist = GetMusicList(MUSIC_DIRECTORY).ToList();
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

        // Buttons
        Add(InitPlayPauseButton());
        Add(InitForwardButton());
        Add(InitBackButton());
        Add(InitNextSongButton());
        Add(InitPreviousSongButton());

        Add(InitProgressBar());
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
        musicList = new ListView()
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(),
            Height = 5,
            Source = new ListWrapper<string>(new ObservableCollection<string>(playlist.Select(f => f.Name)))
        };

        musicList.OpenSelectedItem += (sender, e) =>
        {
            var song = e.Value.ToString();
            player.Load(MUSIC_DIRECTORY + song);
            var songInfo = player.GetSongInfo();
            if (songInfo.Success)
            {
                label.Text = "Playing: " + song + $" {songInfo.Value.TotalTimeInSeconds}";
            }
            else
            {
                label.Text = songInfo.Error;
            }
            player.Load(MUSIC_DIRECTORY + song);
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
        //var options = Enumerable.Range(1, 100).Cast<object>().ToList();
        volumeSlider = new Slider(options)
        {
            X = Pos.Center(),
            Y = Pos.Top(progressBar) - 4,
            Width = Dim.Fill(),
            Type = SliderType.Single,
            UseMinimumSize = false,
            BorderStyle = LineStyle.Rounded,
            ShowEndSpacing = false,
            //Orientation = Orientation.Vertical,
        };

        volumeSlider.OptionsChanged += (sender, e) =>
        {
            var value = e.Options.FirstOrDefault().Key;
            player.SetVolume(value);
        };

        return volumeSlider;
    }

    private ProgressBar InitProgressBar()
    {
        progressBar = new ProgressBar()
        {
            X = 0,
            Y = Pos.Top(playPauseButton) - 4,
            Width = Dim.Fill(),
            Fraction = 0,
            BorderStyle = LineStyle.Rounded,
            ProgressBarStyle = ProgressBarStyle.Continuous,
        };

        return progressBar;
    }


    private Button InitPlayPauseButton()
    {
        playPauseButton = new Button()
        {
            Text = "||",
            X = Pos.Center(),
            //Y = Pos.Bottom(this) - 4,
            Y = Pos.Bottom(this) - 4,
            Height = 1,
        };

        playPauseButton.Accept += (sender, e) =>
        {
            if (playPauseButton.Text == "|>")
            {
                playPauseButton.Text = "||";
                player.Play();

                Application.AddTimeout(TimeSpan.FromSeconds(0.01), () =>
                {
                    var songInfo = player.GetSongInfo();
                    if (songInfo.Success)
                    {
                        progressBar.Fraction = (float)songInfo.Value.CurrentTime / songInfo.Value.TotalTimeInSeconds;
                        // TODO: Timer when seconds < 10
                        var timer = $" {songInfo.Value.CurrentTime / 60}:{songInfo.Value.CurrentTime % 60} / {songInfo.Value.TotalTimeInSeconds / 60}:{songInfo.Value.TotalTimeInSeconds % 60}";
                        label.Text = "Playing: " + songInfo.Value.Name + $" {timer}";
                        Application.Refresh();
                    }
                    else
                    {
                        label.Text = songInfo.Error;
                    }

                    if (songInfo.Value.CurrentTime >= songInfo.Value.TotalTimeInSeconds)
                    {
                        player.Load(playlist[musicList.SelectedItem + 1].FullName);
                        musicList.SelectedItem++;
                        player.Play();

                    }
                    return true;
                });
            }
            else
            {
                playPauseButton.Text = "|>";
                player.Pause();
            }
        };

        return playPauseButton;
    }

    private Button InitBackButton()
    {
        backButton = new Button()
        {
            Text = "<",
            X = Pos.Left(playPauseButton) - (4 + 6),
            Y = Pos.Bottom(this) - 4,
            Height = 1,
        };


        backButton.Accept += (sender, e) =>
        {
            var currentTime = player.GetSongInfo().Value.CurrentTime;
            if (currentTime - 10 < 0)
            {
                player.ChangeCurrentSongTime(0);
            }
            else
            {
                player.ChangeCurrentSongTime(currentTime - 10);
            }
        };

        return backButton;
    }

    private Button InitForwardButton()
    {
        forwardButton = new Button()
        {
            Text = ">",
            X = Pos.Right(playPauseButton) + 4,
            Y = Pos.Bottom(this) - 4,
            Height = 1,
        };
        forwardButton.Accept += (sender, e) =>
        {
            var currentTime = player.GetSongInfo().Value.CurrentTime;
            player.ChangeCurrentSongTime(currentTime + 10);
        };
        return forwardButton;
    }
    private Button InitPreviousSongButton()
    {
        previousSongButton = new Button()
        {
            Text = "<<",
            X = Pos.Left(backButton) - (4 + 6),
            Y = Pos.Bottom(this) - 4,
            Height = 1,
        };

        previousSongButton.Accept += (sender, e) =>
        {
            if (musicList.SelectedItem - 1 < 0)
            {
                MessageBox.ErrorQuery("Error", "No previous song", "OK");
            }
            else
            {
                musicList.SelectedItem--;
                player.Load(playlist[musicList.SelectedItem].FullName);
                player.Play();
            }
        };

        return previousSongButton;
    }

    private Button InitNextSongButton()
    {
        nextSongButton = new Button()
        {
            Text = ">>",
            X = Pos.Right(forwardButton) + 4,
            Y = Pos.Bottom(this) - 4,
            Height = 1,
        };

        nextSongButton.Accept += (sender, e) =>
        {
            if (musicList.SelectedItem + 1 < playlist.Count)
            {
                musicList.SelectedItem++;
                player.Load(playlist[musicList.SelectedItem].FullName);
            }
            else
            {
                musicList.SelectedItem = 0;
                player.Load(playlist[musicList.SelectedItem].FullName);
            }
            musicList.SelectedItem++;
            player.Play();
        };

        return nextSongButton;
    }
    private static IEnumerable<FileInfo> GetMusicList(string directoryPath)
    {
        var d = new DirectoryInfo(directoryPath);

        FileInfo[] Files = d.GetFiles("*.mp3");

        foreach (FileInfo file in Files)
        {
            yield return file;
        }
    }
}