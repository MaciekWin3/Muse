using Muse.Player;
using Muse.UI.Bus;
using Muse.UI.Views;
using Muse.Utils;
using NAudio.Wave;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI;

public sealed class MainWindowView : Window
{
    private readonly IPlayerService player;

    // Components
    private ControlPanelView controlPanelView = null!;
    private MusicListView musicListView = null!;
    private ProgressBarView progressBarView = null!;
    public VolumeView volumeSlider = null!;

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

        uiEventBus.Publish(new PlaylistUpdated([.. Playlist.Select(f => f.Name)]));
    }

    private void RegisterBusHandlers()
    {
        // SongSelected => load + play
        uiEventBus.Subscribe<SongSelected>(msg =>
        {
            // load & play
            uiEventBus.Publish(new PlayRequested());
            player.Load(msg.FullPath);
            player.SetVolume(Globals.Volume);
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
            var songInfoResult = player.GetSongInfo();
            if (songInfoResult.IsFailure)
            {
                MessageBox.ErrorQuery("Error", "Please select a song", "OK");
                uiEventBus.Publish(new PauseRequested());
                return;
            }

            if (player.State == PlaybackState.Playing)
            {
                uiEventBus.Publish(new PauseRequested());
                player.Pause();
            }
            else
            {
                uiEventBus.Publish(new PlayRequested());
                var res = player.Play();
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
                if (newTime < 0)
                {
                    newTime = 0;
                }
                if (newTime > songInfo.Value.TotalTimeInSeconds)
                {
                    newTime = songInfo.Value.TotalTimeInSeconds;
                }
                player.ChangeCurrentSongTime(newTime);
            }
        });

        // Volume
        uiEventBus.Subscribe<VolumeChanged>(msg =>
        {
            if (msg.Volume < 0f || msg.Volume > 1f)
            {
                return;
            }

            player.SetVolume(msg.Volume);
        });

        // Reload playlist command
        uiEventBus.Subscribe<ReloadPlaylist>(msg =>
        {
            ReloadPlaylist(msg.DirectoryPath);
            // publish updated playlist names for UI consumers
            var names = Playlist.Select(f => f.Name).ToList();
            uiEventBus.Publish(new PlaylistUpdated(names));
        });

        // Example: external publisher can update progress UI by sending TrackProgress,
        // but here we'll keep using TrackSong periodic check
        uiEventBus.Subscribe<TrackProgress>(msg =>
        {
            Application.Invoke(() =>
            {
                if (progressBarView is not null)
                {
                    progressBarView.Fraction = (float)msg.CurrentSeconds / Math.Max(1, msg.TotalSeconds);
                    progressBarView.Title = $"Playing: {msg.Name} {FormatTime(msg.CurrentSeconds, msg.TotalSeconds)}";
                }
            });
        });

        uiEventBus.Subscribe<ReloadPlaylist>(msg =>
        {
            ReloadPlaylist(msg.DirectoryPath);
        });

        uiEventBus.Subscribe<PreviousSongRequested>(_ =>
        {
            uiEventBus.Publish(new ChangeSongIndexRequested(-1));
        });

        uiEventBus.Subscribe<NextSongRequested>(_ =>
        {
            uiEventBus.Publish(new ChangeSongIndexRequested(+1));
        });
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        Playlist = [.. MusicListHelper.GetMusicList(Globals.MuseDirectory)];
    }

    public void RegisterControls()
    {
        controlPanelView = new ControlPanelView(uiEventBus, 0, Pos.Bottom(this) - 6);
        Add(controlPanelView);

        progressBarView = new ProgressBarView(uiEventBus, 0, Pos.Top(controlPanelView) - Globals.PROGRESS_BAR_HEIGHT);
        Add(progressBarView);

        volumeSlider = new VolumeView(uiEventBus, 0, Pos.Top(progressBarView) - Globals.VOLUME_SLIDER_HEIGHT);
        Add(volumeSlider);

        musicListView = new MusicListView(uiEventBus, player, 0, 0, CalculateReservedBottomSpace());
        Add(musicListView);

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
                // TODO
                //musicList.SetSource(new ObservableCollection<string>(Playlist.Select(f => f.Name)));
                // TODO: Check if refresh is needed
                //App.Refresh();
            }
            return true;
        });

        // musicList <- REMOVE
        //Add(musicListFrame);

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
                    if (progressBarView is not null)
                    {
                        progressBarView.Fraction = fraction;
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

    private int CalculateReservedBottomSpace()
    {
        int bottomReserved = 0;
        if (volumeSlider is not null)
        {
            bottomReserved += Globals.VOLUME_SLIDER_HEIGHT;
        }
        if (progressBarView is not null)
        {
            bottomReserved += Globals.PROGRESS_BAR_HEIGHT;
        }
        if (controlPanelView is not null)
        {
            bottomReserved += Globals.BUTTONS_FRAME_HEIGHT;
        }
        return bottomReserved;
    }

    private void TrackSong()
    {
        var songInfoResult = player.GetSongInfo();
        if (!songInfoResult.Success)
        {
            progressBarView.Text = songInfoResult.Error;
            return;
        }

        var info = songInfoResult.Value;

        progressBarView.Fraction = (float)info.CurrentTime / info.TotalTimeInSeconds;

        progressBarView.Title = $"Playing: {info.Name}{FormatTime(info.CurrentTime, info.TotalTimeInSeconds)}";

        if (info.CurrentTime >= info.TotalTimeInSeconds)
        {
            uiEventBus.Publish(new NextSongRequested());
        }
    }

    private static string FormatTime(int current, int total)
    {
        int cm = current / 60;
        int cs = current % 60;
        int tm = total / 60;
        int ts = total % 60;

        return $" {cm}:{cs:00} / {tm}:{ts:00}";
    }

    public void ReloadPlaylist(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return;
        }

        Playlist = [.. MusicListHelper.GetMusicList(path)];
        NumberOfSongs = Playlist.Count;
    }
}