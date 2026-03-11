using Muse.Player;
using Muse.UI.Bus;
using Muse.UI.Views;
using Muse.Utils;
using System.Threading;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
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
    public List<Track> Playlist { get; set; }

    private readonly IUiEventBus uiEventBus;
    private CancellationTokenSource? _debounceCts;
    private string _currentDirectory;

    private PlayMode _playMode = PlayMode.None;
    private bool _isShuffle = false;
    private readonly Random _random = new();
    private readonly YoutubeClient _youtubeClient = new();
    private bool _isTransitioning = false;

    public MainWindowView(IPlayerService player, IUiEventBus uiEventBus)
    {
        this.player = player;
        this.uiEventBus = uiEventBus;
        _currentDirectory = Globals.MuseDirectory;

        Playlist = MusicListHelper.GetMusicList(_currentDirectory).Select(Track.FromFileInfo).ToList();
        NumberOfSongs = Playlist.Count;
        RegisterBusHandlers();
        RegisterControls();
        RegisterStyles();

        uiEventBus.Publish(new PlaylistUpdated(Playlist));
        
        // Single persistent timer for tracking playback
        Application.AddTimeout(TimeSpan.FromMilliseconds(200), () =>
        {
            return TrackSong();
        });
    }

    private void RegisterBusHandlers()
    {
        uiEventBus.Subscribe<SongSelected>(async msg =>
        {
            _isTransitioning = false;
            if (player.CurrentTrack?.Path == msg.Track.Path)
            {
                if (player.State == PlaybackState.Playing)
                {
                    return;
                }

                if (player.State == PlaybackState.Paused)
                {
                    uiEventBus.Publish(new PlayRequested());
                    player.Play();
                    return;
                }
            }

            var loadResult = await player.Load(msg.Track);
            if (!loadResult.Success)
            {
                uiEventBus.Publish(new PauseRequested());
                Application.Invoke(() => 
                {
                    MessageBox.ErrorQuery("Error", $"Cannot load track: {loadResult.Error}", "OK");
                    uiEventBus.Publish(new NextSongRequested());
                });
                return;
            }
            uiEventBus.Publish(new PlayRequested());
            player.SetVolume(Globals.Volume);
            var result = player.Play();
            if (!result.Success)
            {
                Application.Invoke(() => MessageBox.ErrorQuery("Error", result.Error, "OK"));
            }
        });

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
                player.Play();
            }
        });

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

        uiEventBus.Subscribe<VolumeChanged>(msg =>
        {
            if (msg.Volume < 0f || msg.Volume > 1f)
            {
                return;
            }

            player.SetVolume(msg.Volume);
        });

        uiEventBus.Subscribe<MuteToggle>(msg =>
        {
            uiEventBus.Publish(new VolumeChanged(msg.IsMuted ? 0f : Globals.Volume));
        });

        uiEventBus.Subscribe<ReloadPlaylist>(msg =>
        {
            ReloadPlaylist(msg.DirectoryPath, msg.Recursive);
            uiEventBus.Publish(new PlaylistUpdated(Playlist));
        });

        uiEventBus.Subscribe<LoadYoutubePlaylist>(async msg =>
        {
            try
            {
                var playlist = await _youtubeClient.Playlists.GetAsync(msg.PlaylistUrl);
                
                var newTracks = new List<Track>();
                await foreach (var v in _youtubeClient.Playlists.GetVideosAsync(playlist.Id))
                {
                    newTracks.Add(new Track
                    {
                        Name = v.Title,
                        Path = v.Url,
                        Source = TrackSource.YouTube,
                        YouTubeId = v.Id,
                        DurationInSeconds = (int?)(v.Duration?.TotalSeconds)
                    });
                }

                Application.Invoke(() =>
                {
                    Playlist = newTracks;
                    NumberOfSongs = Playlist.Count;
                    uiEventBus.Publish(new PlaylistUpdated(Playlist));
                });
            }
            catch (Exception ex)
            {
                Application.Invoke(() => MessageBox.ErrorQuery("Error", $"Failed to load YouTube playlist: {ex.Message}", "OK"));
            }
        });

        uiEventBus.Subscribe<PreviousSongRequested>(_ =>
        {
            if (_isShuffle && Playlist.Count > 0)
            {
                int newIndex = _random.Next(Playlist.Count);
                uiEventBus.Publish(new SongSelected(Playlist[newIndex]));
                return;
            }
            uiEventBus.Publish(new ChangeSongIndexRequested(-1));
        });

        uiEventBus.Subscribe<NextSongRequested>(_ =>
        {
            if (_isShuffle && Playlist.Count > 0)
            {
                int newIndex = _random.Next(Playlist.Count);
                uiEventBus.Publish(new SongSelected(Playlist[newIndex]));
                return;
            }

            uiEventBus.Publish(new ChangeSongIndexRequested(+1));
        });

        uiEventBus.Subscribe<TogglePlayModeRequested>(_ =>
        {
            _playMode = (PlayMode)(((int)_playMode + 1) % Enum.GetValues<PlayMode>().Length);
            uiEventBus.Publish(new PlayModeChanged(_playMode));
        });

        uiEventBus.Subscribe<ShuffleToggleRequested>(_ =>
        {
            _isShuffle = !_isShuffle;
            uiEventBus.Publish(new ShuffleChanged(_isShuffle));
        });
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        Task.Delay(500, token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;

            var newPlaylist = MusicListHelper.GetMusicList(_currentDirectory).Select(Track.FromFileInfo).ToList();

            Application.Invoke(() =>
            {
                Playlist = newPlaylist;
                NumberOfSongs = Playlist.Count;
                uiEventBus.Publish(new PlaylistUpdated(Playlist));
                uiEventBus.Publish(new RefreshPlaylistsRequested());
            });
        });
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        OnChanged(sender, e);
    }

    public void RegisterControls()
    {
        // Each sub-component has its own border.
        controlPanelView = new ControlPanelView(uiEventBus, 0, Pos.AnchorEnd(Globals.BUTTONS_FRAME_HEIGHT));
        Add(controlPanelView);

        progressBarView = new ProgressBarView(uiEventBus, player, 0, Pos.Top(controlPanelView) - Globals.PROGRESS_BAR_HEIGHT);
        Add(progressBarView);

        volumeSlider = new VolumeView(uiEventBus, 0, Pos.Top(progressBarView) - Globals.VOLUME_SLIDER_HEIGHT);
        Add(volumeSlider);

        musicListView = new MusicListView(uiEventBus, player, 0, 0, 0);
        musicListView.Width = Dim.Fill();
        musicListView.Height = Dim.Fill() - (Globals.BUTTONS_FRAME_HEIGHT + Globals.PROGRESS_BAR_HEIGHT + Globals.VOLUME_SLIDER_HEIGHT);
        Add(musicListView);

        watcher.Path = Globals.MuseDirectory;
        watcher.IncludeSubdirectories = true;
        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
        watcher.Filter = "*.*";
        watcher.Changed += OnChanged;
        watcher.Created += OnChanged;
        watcher.Deleted += OnChanged;
        watcher.Renamed += OnRenamed;
        watcher.EnableRaisingEvents = true;
    }

    public void RegisterStyles()
    {
        X = 0;
        Y = 1; // Below MenuBar
        Width = Dim.Fill();
        Height = Dim.Fill() - 1; // Leave 1 row for StatusBar
        BorderStyle = LineStyle.None; // Hide the border to avoid nested lines
    }

    private bool TrackSong()
    {
        var songInfoResult = player.GetSongInfo();
        if (!songInfoResult.Success)
        {
            return true;
        }

        var info = songInfoResult.Value;
        
        // Notify the bus only if actually playing/paused
        if (player.State != PlaybackState.Stopped)
        {
            uiEventBus.Publish(new TrackProgress(info.Name, info.CurrentTime, info.TotalTimeInSeconds));
        }

        if (info.CurrentTime >= info.TotalTimeInSeconds && info.TotalTimeInSeconds > 0 && 
            player.State == PlaybackState.Playing && !_isTransitioning)
        {
            if (_playMode == PlayMode.RepeatOne)
            {
                player.ChangeCurrentSongTime(0);
                player.Play();
                return true;
            }

            if (_playMode == PlayMode.None && !_isShuffle)
            {
                // Check if it's the last song
                var currentTrack = player.CurrentTrack;
                if (currentTrack != null && Playlist.Count > 0 && Playlist.Last().Path == currentTrack.Path)
                {
                    uiEventBus.Publish(new PauseRequested());
                    player.Stop();
                    return true;
                }
            }

            _isTransitioning = true;
            uiEventBus.Publish(new NextSongRequested());
        }

        return true;
    }

    private static string FormatTime(int current, int total)
    {
        int cm = current / 60;
        int cs = current % 60;
        int tm = total / 60;
        int ts = total % 60;

        return $" {cm}:{cs:00} / {tm}:{ts:00}";
    }

    public void ReloadPlaylist(string path, bool recursive = true)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return;
        }

        _currentDirectory = path;
        watcher.Path = Globals.MuseDirectory;

        Playlist = MusicListHelper.GetMusicList(path, recursive).Select(Track.FromFileInfo).ToList();
        NumberOfSongs = Playlist.Count;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            watcher.Dispose();
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
        }
        base.Dispose(disposing);
    }
}
