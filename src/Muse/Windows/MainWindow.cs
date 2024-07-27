using Muse.Player;
using NAudio.Wave;
using System.Collections.ObjectModel;
using Terminal.Gui;

namespace Muse.Windows;

public class MainWindow : Window
{
    // TODO: Check if next/preious back/forward is avilable
    // TODO: Remove hardcoded path
    private readonly static string MUSIC_DIRECTORY = @"C:\\Users\\macie\\Music\\Miszmasz\\";

    private readonly IPlayer player;

    private FrameView musicListFrame = null!;
    private ListView musicList = null!;

    // Buttons
    private FrameView buttonsFrame = null!;
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
        // Buttons
        var buttonsFrame = InitButtonsFrameView();
        buttonsFrame.Add(InitPlayPauseButton());
        buttonsFrame.Add(InitForwardButton());
        buttonsFrame.Add(InitBackButton());
        buttonsFrame.Add(InitNextSongButton());
        buttonsFrame.Add(InitPreviousSongButton());
        Add(buttonsFrame);

        Add(InitProgressBar());
        Add(InitVolumeSlider());

        var musicListFrame = InitMusicListFrame();
        musicListFrame.Add(InitMusicList());
        Add(musicListFrame);
        //Add(InitLabel("Hello, World!"));

        Application.MouseEvent += (sender, e) =>
        {
            if (e.View is null)
            {
                return;
            }
            if (e.View is ProgressBar)
            {
                if (e.Flags == MouseFlags.Button1Clicked)
                {
                    var width = (float)e.View.Frame.Width;
                    var position = (float)e.Position.X;
                    var fraction = position / width;
                    if (progressBar is not null)
                    {
                        progressBar.Fraction = fraction;
                        player.ChangeCurrentSongTime((int)(fraction * player.GetSongInfo().Value.TotalTimeInSeconds));
                    }
                }
            }
        };
    }

    public void InitStyles()
    {
        X = 0;
        Y = 1;
        Width = Dim.Fill();
        Height = Dim.Fill();
    }

    private FrameView InitMusicListFrame()
    {
        musicListFrame = new FrameView()
        {
            Title = "Music List",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            // TODO: Calculate height
            Height = 11,
            BorderStyle = LineStyle.Rounded,
        };

        return musicListFrame;
    }

    private ListView InitMusicList()
    {
        musicList = new ListView()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Source = new ListWrapper<string>(new ObservableCollection<string>(playlist.Select(f => f.Name)))
        };

        musicList.OpenSelectedItem += (sender, e) =>
        {
            if (player.State == PlaybackState.Stopped || player.State == PlaybackState.Paused)
            {
                if (playPauseButton is not null)
                {
                    playPauseButton.Text = "||";
                }
            }
            var song = e.Value.ToString();
            player.Load(MUSIC_DIRECTORY + song);
            player.Play();

            Application.AddTimeout(TimeSpan.FromSeconds(0.01), () =>
            {
                TrackSong();
                return true;
            });
        };

        return musicList;
    }

    private Label InitLabel(string text)
    {
        label = new Label()
        {
            Text = text,
            X = 1,
            Y = Pos.Bottom(musicListFrame),
            Height = 1,
        };
        return label;
    }

    private Slider InitVolumeSlider()
    {
        var heightOfVolumeSlider = 4;
        var options = new List<object> { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 90, 95, 100 };
        //var options = Enumerable.Range(1, 100).Cast<object>().ToList();
        volumeSlider = new Slider(options)
        {
            Title = "Volume",
            X = Pos.Center(),
            Y = Pos.Top(progressBar) - heightOfVolumeSlider,
            Height = heightOfVolumeSlider,
            Width = Dim.Fill(),
            Type = SliderType.Single,
            UseMinimumSize = false,
            BorderStyle = LineStyle.Rounded,
            ShowEndSpacing = false,
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
        int heightOfProgressBar = 3;
        progressBar = new ProgressBar()
        {

            Title = "Progress",
            X = 0,
            Y = Pos.Top(buttonsFrame) - heightOfProgressBar,
            Height = heightOfProgressBar,
            Width = Dim.Fill(),
            Fraction = 0,
            BorderStyle = LineStyle.Rounded,
            ProgressBarStyle = ProgressBarStyle.Continuous,
        };

        return progressBar;
    }

    private FrameView InitButtonsFrameView()
    {
        buttonsFrame = new FrameView()
        {
            Title = "Controls",
            X = 0,
            Y = Pos.Bottom(this) - 6,
            Width = Dim.Fill(),
            Height = 3,
        };
        return buttonsFrame;
    }

    private Button InitPlayPauseButton()
    {
        playPauseButton = new Button()
        {
            Text = "||",
            X = Pos.Center(),
            Height = 1,
        };

        playPauseButton.Accept += (sender, e) =>
        {
            if (playPauseButton.Text == "|>")
            {
                playPauseButton.Text = "||";
                var playerResult = player.Play();
                if (playerResult.Success)
                {

                    Application.AddTimeout(TimeSpan.FromSeconds(0.01), () =>
                    {
                        TrackSong();
                        return true;
                    });
                }
                else
                {
                    MessageBox.ErrorQuery("Error", "Please select a song", "OK");
                }
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

    private void TrackSong()
    {
        var songInfo = player.GetSongInfo();
        if (songInfo.Success)
        {
            progressBar.Fraction = (float)songInfo.Value.CurrentTime / songInfo.Value.TotalTimeInSeconds;
            var currentMinutes = songInfo.Value.CurrentTime / 60;
            var currentSeconds = songInfo.Value.CurrentTime % 60;
            var totalMinutes = songInfo.Value.TotalTimeInSeconds / 60;
            var totalSeconds = songInfo.Value.TotalTimeInSeconds % 60;
            var formatedCurrentSeconds = currentSeconds < 10 ? $"0{currentSeconds}" : currentSeconds.ToString();
            var formatedTotalSeconds = totalSeconds < 10 ? $"0{totalSeconds}" : totalSeconds.ToString();
            var timer = $" {currentMinutes}:{formatedCurrentSeconds} / {totalMinutes}:{formatedTotalSeconds}";

            progressBar.Title = "Playing: " + songInfo.Value.Name + $" {timer}";
            Application.Refresh();
        }
        else
        {
            progressBar.Text = songInfo.Error;
        }

        if (songInfo.Value.CurrentTime >= songInfo.Value.TotalTimeInSeconds)
        {
            player.Load(playlist[musicList.SelectedItem + 1].FullName);
            musicList.SelectedItem++;
            player.Play();
        }
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