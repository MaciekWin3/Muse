using Muse.Player;
using Muse.UI.Bus;
using Muse.UI.Views;
using Muse.Utils;
using NAudio.Wave;
using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI;

public sealed class MainWindowView : Window
{
    // TODO: Check if next/preious back/forward is avilable
    // TODO: Remove hardcoded path

    private readonly IPlayerService player;

    private FrameView musicListFrame = null!;
    private ListView musicList = null!;

    // Buttons
    private FrameView buttonsFrame = null!;
    private Button playPauseButton = null!;
    private Button forwardButton = null!;
    private Button backButton = null!;
    private Button nextSongButton = null!;
    private Button previousSongButton = null!;

    private ProgressBar progressBar = null!;
    public Slider volumeSlider = null!;

    // explicit heights used across the UI so we can reuse them without calling internal Dim APIs
    private const int ButtonsFrameHeight = 3;
    private const int ButtonsHeight = 2;
    private const int ProgressBarHeight = 3;
    private const int VolumeSliderHeight = 4;

    private readonly FileSystemWatcher watcher = new();
    private int NumberOfSongs { get; set; }
    public List<FileInfo> Playlist { get; set; }

    private readonly IUiEventBus uiEventBus;


    public MainWindowView(IPlayerService player, IUiEventBus uiEventBus)
    {
        this.player = player;
        this.uiEventBus = uiEventBus;

        Playlist = [.. MusicListHelper.GetMusicList(Globals.MuseDirectory)];
        NumberOfSongs = Playlist.Count;
        RegisterBusHandlers();
        RegisterControls();
        RegisterStyles();
    }

    private void RegisterBusHandlers()
    {
        // SongSelected => load + play
        uiEventBus.Subscribe<SongSelected>(msg =>
        {
            // load & play
            player.Load(msg.FullPath);
            var result = player.Play();
            if (result.Success)
            {
                // start tracking loop - reuse existing TrackSong method
                Application.AddTimeout(TimeSpan.FromMilliseconds(100), () =>
                {
                    TrackSong();
                    return true;
                });
            }
            else
            {
                // show error on UI thread
                Application.Invoke(() => MessageBox.ErrorQuery("Error", "Cannot play file", "OK"));
            }
        });

        // Toggle play/pause requested: decide based on player state
        uiEventBus.Subscribe<TogglePlayRequested>(_ =>
        {
            if (player.State == PlaybackState.Playing)
            {
                player.Pause();
                uiEventBus.Publish(new PlaybackStateChanged(false));
            }
            else
            {
                var res = player.Play();
                uiEventBus.Publish(new PlaybackStateChanged(res.Success));
                if (res.Success)
                {
                    Application.AddTimeout(TimeSpan.FromMilliseconds(100), () =>
                    {
                        TrackSong();
                        return true;
                    });
                }
            }
        });

        // Seek relative
        uiEventBus.Subscribe<SeekRelativeRequested>(msg =>
        {
            var songInfo = player.GetSongInfo();
            if (songInfo.Success)
            {
                var newTime = songInfo.Value.CurrentTime + msg.Seconds;
                if (newTime < 0) newTime = 0;
                if (newTime > songInfo.Value.TotalTimeInSeconds) newTime = songInfo.Value.TotalTimeInSeconds;
                player.ChangeCurrentSongTime(newTime);
            }
        });

        // Volume
        uiEventBus.Subscribe<VolumeChanged>(msg =>
        {
            player.SetVolume(msg.Value);
        });

        // Reload playlist command
        uiEventBus.Subscribe<ReloadPlaylist>(msg =>
        {
            ReloadPlaylist(msg.DirectoryPath);
            // publish updated playlist names for UI consumers
            var names = Playlist.Select(f => f.Name).ToList();
            uiEventBus.Publish(new PlaylistUpdated(names));
        });

        // update UI when playback state changed
        uiEventBus.Subscribe<PlaybackStateChanged>(msg =>
        {
            // update buttons text on the UI thread
            Application.Invoke(() =>
            {
                playPauseButton?.Text = msg.IsPlaying ? "||" : "|>";
            });
        });

        // Example: external publisher can update progress UI by sending TrackProgress,
        // but here we'll keep using TrackSong periodic check
        uiEventBus.Subscribe<TrackProgress>(msg =>
        {
            Application.Invoke(() =>
            {
                if (progressBar is not null)
                {
                    progressBar.Fraction = (float)msg.CurrentSeconds / Math.Max(1, msg.TotalSeconds);
                    progressBar.Title = $"Playing: {msg.Name} {FormatTime(msg.CurrentSeconds, msg.TotalSeconds)}";
                }
            });
        });
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        Playlist = [.. MusicListHelper.GetMusicList(Globals.MuseDirectory)];
    }

    public void RegisterControls()
    {
        // Buttons
        var buttonsFrame = InitButtonsFrameView();

        playPauseButton = InitPlayPauseButton();
        forwardButton = InitForwardButton();
        backButton = InitBackButton();
        nextSongButton = InitNextSongButton();
        previousSongButton = InitPreviousSongButton();

        buttonsFrame.Add(previousSongButton);
        buttonsFrame.Add(backButton);
        buttonsFrame.Add(playPauseButton);
        buttonsFrame.Add(forwardButton);
        buttonsFrame.Add(nextSongButton);

        Add(buttonsFrame);

        Add(InitProgressBar());

        //Add(InitVolumeSlider());
        volumeSlider = new VolumeView(uiEventBus, Pos.Top(progressBar) - VolumeSliderHeight);
        Add(volumeSlider);

        var musicListFrame = InitMusicListFrame();
        var musicList = InitMusicList();

        musicListFrame.Add(musicList);

        Application.AddTimeout(TimeSpan.FromSeconds(1), () =>
        {
            watcher.Path = Globals.MuseDirectory;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = "*.*";
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;
            if (NumberOfSongs != Playlist.Count)
            {
                // TODO: Refresh when folder content changes
                // TODO: Same volume after changing song
                //numberOfSongs = playlist.Count;
                musicList.SetSource(new ObservableCollection<string>(Playlist.Select(f => f.Name)));
                // TODO: Check if refresh is needed
                //App.Refresh();
            }
            return true;
        });

        Add(musicListFrame);

        Application.Mouse.MouseEvent += (sender, e) =>
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
                        var songInfo = player.GetSongInfo();
                        if (songInfo.Success)
                        {
                            player.ChangeCurrentSongTime((int)(fraction * player.GetSongInfo().Value.TotalTimeInSeconds));
                        }
                    }
                }
            }
        };
    }

    public void RegisterStyles()
    {
        X = 0;
        Y = 1;
        Width = Dim.Fill();
        Height = Dim.Fill() - 1;
    }

    private FrameView InitMusicListFrame()
    {
        // Calculate reserved space taken by bottom fixed controls so the music list fills remaining area.
        // These controls are created before this method is called in InitControls.
        int bottomReserved = 0;
        if (volumeSlider is not null)
        {
            bottomReserved += VolumeSliderHeight;
        }
        if (progressBar is not null)
        {
            bottomReserved += ProgressBarHeight;
        }
        if (buttonsFrame is not null)
        {
            bottomReserved += ButtonsFrameHeight;
        }

        musicListFrame = new FrameView()
        {
            Title = "Music List",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - bottomReserved,
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
            Source = new ListWrapper<string>(new ObservableCollection<string>(Playlist.Select(f => f.Name)))
        };

        musicList.OpenSelectedItem += (sender, e) =>
        {
            if (player.State == PlaybackState.Stopped || player.State == PlaybackState.Paused)
            {
                playPauseButton?.Text = "||";
            }

            var song = e.Value.ToString();

            if (song is null)
            {
                return;
            }
            player.Load(Path.Combine(Globals.MuseDirectory, song));
            player.Play();

            Application.AddTimeout(TimeSpan.FromMilliseconds(100), () =>
            {
                TrackSong();
                return true;
            });
        };

        return musicList;
    }

    private Slider InitVolumeSlider()
    {
        var options = new List<object> { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 90, 95, 100 };
        volumeSlider = new Slider(options)
        {
            Title = "Volume",
            Y = Pos.Top(progressBar) - VolumeSliderHeight,
            Height = VolumeSliderHeight,
            Width = Dim.Fill(),
            Type = SliderType.Single,
            UseMinimumSize = false,
            BorderStyle = LineStyle.Rounded,
            ShowEndSpacing = false,
        };

        volumeSlider.OptionsChanged += (sender, e) =>
        {
            uiEventBus.Publish(new VolumeChanged(e.Options.FirstOrDefault().Key));
            //var value = e.Options.FirstOrDefault().Key;
            //player.SetVolume(value);
        };

        volumeSlider.SetOption(10);

        return volumeSlider;
    }

    private ProgressBar InitProgressBar()
    {
        progressBar = new ProgressBar()
        {

            Title = "Progress",
            X = 0,
            Y = Pos.Top(buttonsFrame) - ProgressBarHeight,
            Height = ProgressBarHeight,
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
            Height = ButtonsFrameHeight,
        };
        return buttonsFrame;
    }

    private Button InitPlayPauseButton()
    {
        playPauseButton = new Button()
        {
            Text = "||",
            X = Pos.Center(),
            Height = 2,
            ShadowStyle = ShadowStyle.None
        };

        playPauseButton.Accepting += (sender, e) =>
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
            e.Handled = true;
        };

        return playPauseButton;
    }

    private Button InitBackButton()
    {
        backButton = new Button()
        {
            Text = "<",
            X = Pos.Left(playPauseButton) - (4 + 6),
            Height = ButtonsHeight,
            ShadowStyle = ShadowStyle.None
        };


        backButton.Accepting += (sender, e) =>
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
            e.Handled = true;
        };

        return backButton;
    }

    private Button InitForwardButton()
    {
        forwardButton = new Button()
        {
            Text = ">",
            X = Pos.Right(playPauseButton) + 4,
            Height = ButtonsHeight,
            ShadowStyle = ShadowStyle.None
        };
        forwardButton.Accepting += (sender, e) =>
        {
            var currentTime = player.GetSongInfo().Value.CurrentTime;
            player.ChangeCurrentSongTime(currentTime + 10);
            e.Handled = true;
        };
        return forwardButton;
    }

    private Button InitPreviousSongButton()
    {
        previousSongButton = new Button()
        {
            Text = "<<",
            X = Pos.Left(backButton) - (4 + 6),
            Height = ButtonsHeight,
            ShadowStyle = ShadowStyle.None
        };

        previousSongButton.Accepting += (sender, e) =>
        {
            if (musicList.SelectedItem - 1 < 0)
            {
                MessageBox.ErrorQuery("Error", "No previous song", "OK");
            }
            else
            {
                musicList.SelectedItem--;
                player.Load(Playlist[musicList.SelectedItem].FullName);
                player.Play();
            }
            e.Handled = true;
        };

        return previousSongButton;
    }

    private Button InitNextSongButton()
    {
        nextSongButton = new Button()
        {
            Text = ">>",
            X = Pos.Right(forwardButton) + 4,
            Height = ButtonsHeight,
            ShadowStyle = ShadowStyle.None
        };

        nextSongButton.Accepting += (sender, e) =>
        {
            if (musicList.SelectedItem + 1 < Playlist.Count)
            {
                musicList.SelectedItem++;
                player.Load(Playlist[musicList.SelectedItem].FullName);
            }
            else
            {
                musicList.SelectedItem = 0;
                player.Load(Playlist[musicList.SelectedItem].FullName);
            }
            player.Play();
            e.Handled = true;
        };

        return nextSongButton;
    }

    private void TrackSong()
    {
        var songInfoResult = player.GetSongInfo();
        if (!songInfoResult.Success)
        {
            progressBar.Text = songInfoResult.Error;
            return;
        }

        var info = songInfoResult.Value;

        progressBar.Fraction = (float)info.CurrentTime / info.TotalTimeInSeconds;

        progressBar.Title = $"Playing: {info.Name}{FormatTime(info.CurrentTime, info.TotalTimeInSeconds)}";

        if (info.CurrentTime >= info.TotalTimeInSeconds)
        {
            MoveToNextSong();
        }
    }

    private string FormatTime(int current, int total)
    {
        int cm = current / 60;
        int cs = current % 60;
        int tm = total / 60;
        int ts = total % 60;

        return $" {cm}:{cs:00} / {tm}:{ts:00}";
    }

    private void MoveToNextSong()
    {
        if (musicList.SelectedItem + 1 < Playlist.Count)
        {
            musicList.SelectedItem++;
            player.Load(Playlist[musicList.SelectedItem].FullName);
        }
        else
        {
            musicList.SelectedItem = 0;
            player.Load(Playlist[musicList.SelectedItem].FullName);
        }
        player.Play();
    }

    // Add this public helper to the MainWindow class
    public void ReloadPlaylist(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return;
        }

        Playlist = [.. MusicListHelper.GetMusicList(path)];
        NumberOfSongs = Playlist.Count;

        if (musicList is not null)
        {
            // Update ListView source on the UI/main loop
            Application.Invoke(() =>
            {
                musicList.SetSource(new ObservableCollection<string>(Playlist.Select(f => f.Name)));
            });
        }
    }
}