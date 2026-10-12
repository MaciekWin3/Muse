using Muse.Player;
using Muse.UI.Bus;
using Muse.UI.Views;
using Muse.Utils;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace Muse.UI;

public sealed class MainWindowView : Window
{
    private readonly IPlayerService player;
    private readonly IMusicLibraryService musicLibrary;

    // Components
    private ControlPanelView controlPanelView = null!;
    private MusicListView musicListView = null!;
    private ProgressBarView progressBarView = null!;
    public VolumeView volumeSlider = null!;
    private EqualizerView equalizerView = null!;

    private readonly FileSystemWatcher watcher = new();
    public List<Track> Playlist { get; set; }

    private readonly IUiEventBus uiEventBus;
    private CancellationTokenSource? _debounceCts;
    private CancellationTokenSource? _preCacheCts;
    private readonly SemaphoreSlim _preCacheSemaphore = new(1, 1);
    private string _currentDirectory;

    private PlayMode _playMode = PlayMode.None;
    private bool _isShuffle = false;
    private readonly Random _random = new();
    private readonly YoutubeClient _youtubeClient = new();
    private bool _isTransitioning = false;
    private object? _trackTimeoutToken; // Captured reference for proper disposal

    public MainWindowView(IPlayerService player, IUiEventBus uiEventBus, IMusicLibraryService musicLibrary)
    {
        this.player = player;
        this.uiEventBus = uiEventBus;
        this.musicLibrary = musicLibrary;
        _currentDirectory = Globals.MuseDirectory;

        Playlist = musicLibrary.Scan(_currentDirectory).Select(Track.FromFileInfo).ToList();
        RegisterBusHandlers();
        RegisterControls();
        RegisterStyles();

        uiEventBus.Publish(new PlaylistUpdated(Playlist));

        // Use the lifecycle event to register timeouts safely once the UI tree is attached
        Initialized += (s, e) =>
        {
            if (App is not null)
            {
                _trackTimeoutToken = App.AddTimeout(TimeSpan.FromMilliseconds(200), () =>
                {
                    return TrackSong();
                });
            }
        };
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

            if (msg.Track.Source == TrackSource.YouTube && string.IsNullOrEmpty(msg.Track.StreamUrl))
            {
                uiEventBus.Publish(new SongLoading(msg.Track.Name));
            }
            var loadResult = await player.Load(msg.Track);
            if (!loadResult.Success)
            {
                uiEventBus.Publish(new PauseRequested());
                App?.Invoke(() =>
                {
                    if (App is not null)
                    {
                        MessageBox.ErrorQuery(App, "Error", $"Cannot load track: {loadResult.Error}", "OK");
                    }
                    uiEventBus.Publish(new NextSongRequested());
                });
                return;
            }
            uiEventBus.Publish(new PlayRequested());
            player.SetVolume(Globals.Volume);
            var result = player.Play();
            if (!result.Success)
            {
                App?.Invoke(() =>
                {
                    if (App is not null)
                    {
                        MessageBox.ErrorQuery(App, "Error", result.Error, "OK");
                    }
                });
            }
            else
            {
                StartPreCaching();
            }
        });

        uiEventBus.Subscribe<TogglePlayRequested>(_ =>
        {
            var songInfoResult = player.GetSongInfo();
            if (songInfoResult.IsFailure)
            {
                if (App is not null)
                {
                    MessageBox.ErrorQuery(App, "Error", "Please select a song", "OK");
                }
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
            if (msg.Volume > 0f)
            {
                Globals.LastAudibleVolume = msg.Volume;
            }
        });

        uiEventBus.Subscribe<MuteToggle>(msg =>
        {
            uiEventBus.Publish(new VolumeChanged(msg.IsMuted ? 0f : Globals.LastAudibleVolume));
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
                var newTracks = new List<Track>();

                var playlistId = PlaylistId.TryParse(msg.PlaylistUrl);
                if (playlistId is not null)
                {
                    var playlist = await _youtubeClient.Playlists.GetAsync(playlistId.Value);
                    await foreach (var v in _youtubeClient.Playlists.GetVideosAsync(playlist.Id))
                    {
                        if (newTracks.Count >= 100)
                        {
                            break;
                        }

                        newTracks.Add(new Track
                        {
                            Name = v.Title,
                            Path = v.Url,
                            Source = TrackSource.YouTube,
                            YouTubeId = v.Id,
                            DurationInSeconds = (int?)(v.Duration?.TotalSeconds)
                        });
                    }
                }
                else
                {
                    var videoId = VideoId.TryParse(msg.PlaylistUrl);
                    if (videoId is not null)
                    {
                        var v = await _youtubeClient.Videos.GetAsync(videoId.Value);
                        newTracks.Add(new Track
                        {
                            Name = v.Title,
                            Path = v.Url,
                            Source = TrackSource.YouTube,
                            YouTubeId = v.Id,
                            DurationInSeconds = (int?)(v.Duration?.TotalSeconds)
                        });
                    }
                    else
                    {
                        throw new ArgumentException("Provided URL is neither a valid YouTube playlist nor a video.");
                    }
                }

                if (newTracks.Count == 0)
                {
                    throw new Exception("No tracks found in the provided YouTube source.");
                }

                App?.Invoke(() =>
                {
                    Playlist = newTracks;
                    uiEventBus.Publish(new PlaylistUpdated(Playlist));
                    StartPreCaching();
                });
            }
            catch (Exception ex)
            {
                App?.Invoke(() =>
                {
                    if (App is not null)
                    {
                        MessageBox.ErrorQuery(App, "Error", $"Failed to load YouTube playlist: {ex.Message}", "OK");
                    }
                });
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

        _ = RefreshLibraryAfterDelayAsync(token);
    }

    private async Task RefreshLibraryAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(500, token);

            var newPlaylist = musicLibrary.Scan(_currentDirectory).Select(Track.FromFileInfo).ToList();

            App?.Invoke(() =>
            {
                Playlist = newPlaylist;
                uiEventBus.Publish(new PlaylistUpdated(Playlist));
                uiEventBus.Publish(new RefreshPlaylistsRequested());
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Library refresh failed: {ex.Message}");
        }
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        OnChanged(sender, e);
    }

    public void RegisterControls()
    {
        controlPanelView = new ControlPanelView(uiEventBus, 0, Pos.AnchorEnd(Globals.BUTTONS_FRAME_HEIGHT));
        Add(controlPanelView);

        progressBarView = new ProgressBarView(uiEventBus, player, 0, Pos.Top(controlPanelView) - Globals.PROGRESS_BAR_HEIGHT);
        Add(progressBarView);

        volumeSlider = new VolumeView(uiEventBus, 0, Pos.Top(progressBarView) - Globals.VOLUME_SLIDER_HEIGHT);
        Add(volumeSlider);

        musicListView = new MusicListView(uiEventBus, player, 0, 0, 0);
        musicListView.Width = Dim.Percent(70);
        musicListView.Height = Dim.Fill() - (Globals.BUTTONS_FRAME_HEIGHT + Globals.PROGRESS_BAR_HEIGHT + Globals.VOLUME_SLIDER_HEIGHT);
        Add(musicListView);

        equalizerView = new EqualizerView(uiEventBus, player, Pos.Right(musicListView), 0);
        equalizerView.Width = Dim.Fill();
        equalizerView.Height = musicListView.Height;
        Add(equalizerView);

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
        BorderStyle = LineStyle.None;
    }

    private bool TrackSong()
    {
        var songInfoResult = player.GetSongInfo();
        if (!songInfoResult.Success)
        {
            return true;
        }

        var info = songInfoResult.Value;

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
                var currentTrack = player.CurrentTrack;
                if (currentTrack is not null && Playlist.Count > 0 && Playlist.Last().Path == currentTrack.Path)
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

    public void ReloadPlaylist(string path, bool recursive = true)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return;
        }

        _currentDirectory = path;
        watcher.Path = Globals.MuseDirectory;

        Playlist = musicLibrary.Scan(path, recursive).Select(Track.FromFileInfo).ToList();
    }

    private void StartPreCaching()
    {
        _preCacheCts?.Cancel();
        _preCacheCts?.Dispose();
        _preCacheCts = new CancellationTokenSource();
        _ = PreCacheNextSongsTrackedAsync(_preCacheCts.Token);
    }

    private async Task PreCacheNextSongsTrackedAsync(CancellationToken token)
    {
        bool entered = false;
        try
        {
            await _preCacheSemaphore.WaitAsync(token);
            entered = true;
            await PreCacheNextSongs(token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Pre-cache failed: {ex.Message}");
        }
        finally
        {
            if (entered)
            {
                _preCacheSemaphore.Release();
            }
        }
    }

    private async Task PreCacheNextSongs(CancellationToken token)
    {
        if (Playlist is null || Playlist.Count == 0) return;

        var currentTrack = player.CurrentTrack;
        int currentIndex = -1;

        if (currentTrack is not null)
        {
            currentIndex = Playlist.FindIndex(t => t.Path == currentTrack.Path);
        }

        var tracksToCache = new List<Track>();

        if (_isShuffle)
        {
            for (int i = 0; i < 2; i++)
            {
                int randomIndex = _random.Next(Playlist.Count);
                if (randomIndex != currentIndex)
                {
                    tracksToCache.Add(Playlist[randomIndex]);
                }
            }
        }
        else
        {
            for (int i = 1; i <= 2; i++)
            {
                int nextIndex = currentIndex + i;
                if (nextIndex >= Playlist.Count)
                {
                    if (_playMode == PlayMode.Repeat)
                    {
                        nextIndex %= Playlist.Count;
                    }
                    else
                    {
                        break;
                    }
                }
                tracksToCache.Add(Playlist[nextIndex]);
            }
        }

        if (tracksToCache.Count > 0)
        {
            token.ThrowIfCancellationRequested();
            await player.PreCacheNextSongs(tracksToCache);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Drop timeout tracking tokens safely on cleanup
            if (_trackTimeoutToken is not null && App is not null)
            {
                App.RemoveTimeout(_trackTimeoutToken);
                _trackTimeoutToken = null;
            }

            watcher.Dispose();
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _preCacheCts?.Cancel();
            _preCacheCts?.Dispose();
            _preCacheSemaphore.Dispose();
        }
        base.Dispose(disposing);
    }
}