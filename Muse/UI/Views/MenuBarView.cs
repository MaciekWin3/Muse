using Muse.UI.Bus;
using Muse.Utils;
using Muse.YouTube;
using System.Collections.ObjectModel;
using System.Text;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace Muse.UI.Views;

public class MenuBarView : MenuBar
{
    private readonly IYoutubeDownloadService youtubeDownloadService;
    private readonly IUiEventBus uiEventBus;
    private readonly List<IDisposable> subscriptions = [];

    private PlayMode playMode = PlayMode.None;
    private bool isShuffle = false;

    public MenuBarView(IYoutubeDownloadService youtubeDownloadService, IUiEventBus uiEventBus)
    {
        this.youtubeDownloadService = youtubeDownloadService;
        this.uiEventBus = uiEventBus;

        RebuildMenu();

        Subscribe<RefreshPlaylistsRequested>(_ =>
        {
            App?.Invoke(() =>
            {
                RebuildMenu();
            });
        });

        Subscribe<PlayModeChanged>(msg =>
        {
            App?.Invoke(() =>
            {
                playMode = msg.NewMode;
                RebuildMenu();
            });
        });

        Subscribe<ShuffleChanged>(msg =>
        {
            App?.Invoke(() =>
            {
                isShuffle = msg.IsShuffle;
                RebuildMenu();
            });
        });
    }

    private void Subscribe<T>(Action<T> handler) => subscriptions.Add(uiEventBus.Subscribe(handler));

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }
            subscriptions.Clear();
        }

        base.Dispose(disposing);
    }

    private void RebuildMenu()
    {
        var repeatTitle = playMode switch
        {
            PlayMode.None => "Repeat: None",
            PlayMode.Repeat => "Repeat: All",
            PlayMode.RepeatOne => "Repeat: One",
            _ => "Repeat"
        };

        var shuffleTitle = isShuffle ? "Shuffle: On" : "Shuffle: Off";

        Menus =
        [
            new MenuBarItem("File", new MenuItem[]
            {
                new("Open", "Open music folder", () => OpenFolder()),
                new("Quit", "Quit application", () => App?.RequestStop()),
            }),
            new MenuBarItem("Options", new MenuItem[]
            {
                new("Playlists", "Manage playlists", null) { SubMenu = new Menu(GetPlaylistMenuItems()) },
                new("Theme", "Change application theme", null) { SubMenu = new Menu(GetThemeMenuItems()) },
                new("Download", "Download from YouTube", () => ShowDownloadDialog()),
                new("Stream Playlist", "Stream YouTube playlist", () => ShowStreamPlaylistDialog())
            }),
            new MenuBarItem("Playback", new MenuItem[]
            {
                new(repeatTitle, "Toggle repeat mode (R)", () => uiEventBus.Publish(new TogglePlayModeRequested())),
                new(shuffleTitle, "Toggle shuffle mode (S)", () => uiEventBus.Publish(new ShuffleToggleRequested()))
            }),
            new MenuBarItem("Help", new MenuItem[]
            {
                new("About", "About Muse", () => ShowAsciiArt()),
                new("Shortcuts", "Show shortcuts", () => ShowShortcuts()),
                new("Website", "Muse Website", () => WebsiteHelper.OpenUrl("https://github.com/MaciekWin3/Muse"))
            })
        ];
    }

    private MenuItem[] GetThemeMenuItems()
    {
        return new MenuItem[]
        {
            new("Default", "Default theme", () => uiEventBus.Publish(new ChangeThemeRequested("Default"))),
            new("Dark", "Dark theme", () => uiEventBus.Publish(new ChangeThemeRequested("Dark"))),
            new("Light", "Light theme", () => uiEventBus.Publish(new ChangeThemeRequested("Light"))),
            new("TurboPascal 5", "TurboPascal 5 theme", () => uiEventBus.Publish(new ChangeThemeRequested("TurboPascal 5"))),
            new("Anders", "Anders theme", () => uiEventBus.Publish(new ChangeThemeRequested("Anders"))),
            new("Amber Phosphor", "Amber Phosphor theme", () => uiEventBus.Publish(new ChangeThemeRequested("Amber Phosphor"))),
            new("Green Phosphor", "Green Phosphor theme", () => uiEventBus.Publish(new ChangeThemeRequested("Green Phosphor"))),
            new("8-Bit", "8-Bit theme", () => uiEventBus.Publish(new ChangeThemeRequested("8-Bit")))
        };
    }

    private MenuItem[] GetPlaylistMenuItems()
    {
        var items = new List<MenuItem>
        {
            new("All Songs", "Show all songs", () =>
            {
                App?.Invoke(() => uiEventBus.Publish(new ReloadPlaylist(Globals.MuseDirectory, true)));
            })
        };

        if (Directory.Exists(Globals.MuseDirectory))
        {
            try
            {
                var subDirs = Directory.GetDirectories(Globals.MuseDirectory);
                foreach (var dir in subDirs)
                {
                    var dirName = Path.GetFileName(dir);
                    items.Add(new MenuItem(dirName, $"Show songs from {dirName}", () =>
                    {
                        App?.Invoke(() => uiEventBus.Publish(new ReloadPlaylist(dir, false)));
                    }));
                }
            }
            catch (Exception ex)
            {
                if (App is not null)
                {
                    MessageBox.ErrorQuery(App, "Error", $"Failed to list playlists: {ex.Message}", "Ok");
                }
            }
        }

        return [.. items];
    }

    private void OpenFolder()
    {
        var fileExplorerDialog = new OpenDialog
        {
            Title = "Open",
            Path = Globals.MuseDirectory,
            AllowsMultipleSelection = false,
            OpenMode = OpenMode.Directory
        };

        if (App is null) return;

        App.Run(fileExplorerDialog);

        var selectedPath = fileExplorerDialog.FilePaths?
            .FirstOrDefault()?
            .ToString();

        if (!string.IsNullOrWhiteSpace(selectedPath) && Directory.Exists(selectedPath))
        {
            var mw = FindMainWindow();
            if (mw is not null)
            {
                Globals.MuseDirectory = selectedPath;
                RebuildMenu();
                App.Invoke(() => uiEventBus.Publish(new ReloadPlaylist(selectedPath, true)));
            }
        }
    }

    private MainWindowView? FindMainWindow()
    {
        if (App?.TopRunnableView is null)
        {
            return null;
        }
        return FindIn([.. App.TopRunnableView.SubViews]);
    }

    private MainWindowView? FindIn(IList<View> views)
    {
        foreach (var v in views)
        {
            if (v is MainWindowView mw)
            {
                return mw;
            }
            var result = FindIn([.. v.SubViews]);
            if (result is not null)
            {
                return result;
            }
        }
        return null;
    }

    private void ShowAsciiArt()
    {
        if (App is null) return;

        var sb = new StringBuilder();
        sb.AppendLine("Muse - Terminal MP3 player");
        sb.AppendLine();
        sb.AppendLine(@"  __  __ _    _  _____ ______ ");
        sb.AppendLine(@" |  \/  | |  | |/ ____|  ____|");
        sb.AppendLine(@" | \  / | |  | | (___ | |__   ");
        sb.AppendLine(@" | |\/| | |  | |\___ \|  __|  ");
        sb.AppendLine(@" | |  | | |__| |____) | |____ ");
        sb.AppendLine(@" |_|  |_|\____/|_____/|______|");
        sb.AppendLine();
        MessageBox.Query(App, 50, 15, "About", sb.ToString(), "Ok");
    }

    private void ShowShortcuts()
    {
        if (App is null) return;

        var sb = new StringBuilder();
        sb.AppendLine("Global shortcuts:");
        sb.AppendLine("Quit: Esc");
        sb.AppendLine("Mute: Backspace");
        sb.AppendLine();
        sb.AppendLine("Player shortcuts:");
        sb.AppendLine("p    - Play/Pause");
        sb.AppendLine("n    - Next track");
        sb.AppendLine("b    - Previous track");
        sb.AppendLine("d    - Delete track");
        sb.AppendLine("r    - Repeat mode");
        sb.AppendLine("s    - Shuffle mode");
        sb.AppendLine("Tab  - Switch Shortcuts/Search mode");

        MessageBox.Query(App, 50, 15, "Shortcuts", sb.ToString(), "Ok");
    }

    private void ShowDownloadDialog()
    {
        if (App is null) return;

        var urlLabel = new Label()
        {
            Text = "YouTube URL: ",
            X = 1,
            Y = 1,
        };

        var urlTextField = new TextField()
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill()! - 5,
        };

        var nameLabel = new Label()
        {
            Text = "Song name: ",
            X = 1,
            Y = Pos.Bottom(urlTextField),
        };

        var nameTextField = new TextField()
        {
            X = 1,
            Y = Pos.Bottom(nameLabel),
            Width = Dim.Fill()! - 5,
        };

        var playlistLabel = new Label()
        {
            Text = "Playlist: ",
            X = 1,
            Y = Pos.Bottom(nameTextField),
        };

        var playlists = new List<string> { "All Songs" };
        if (Directory.Exists(Globals.MuseDirectory))
        {
            var subDirs = Directory.GetDirectories(Globals.MuseDirectory)
                .Select(Path.GetFileName)
                .Where(x => x is not null)
                .Select(x => x!)
                .ToList();
            playlists.AddRange(subDirs);
        }

        var playlistComboBox = new DropDownList()
        {
            X = 1,
            Y = Pos.Bottom(playlistLabel),
            Width = Dim.Fill()! - 5,
            Height = 4,
            Source = new ListWrapper<string>(new ObservableCollection<string>(playlists))
        };

        var progressLabel = new Label()
        {
            Text = "Ready",
            X = Pos.Center(),
            Y = Pos.Bottom(playlistComboBox) + 1,
            Width = Dim.Fill()! - 5,
            Visible = false
        };

        var textLabelSuccess = new Label()
        {
            Text = "Downloaded successfully!",
            X = Pos.Center(),
            Y = Pos.Bottom(progressLabel) + 1,
            Visible = false,
        };

        var dialog = new Dialog()
        {
            Title = "Download",
            Width = Dim.Percent(50),
            Height = Dim.Percent(70),
        };

        var downloadButton = new Button()
        {
            Title = "Download",
        };

        downloadButton.Accepting += async (s, e) =>
        {
            e.Handled = true;

            if (string.IsNullOrWhiteSpace(urlTextField.Text?.ToString()))
            {
                App?.Invoke(() => MessageBox.ErrorQuery(App, "Error", "URL cannot be empty", "Ok"));
                return;
            }

            textLabelSuccess.Visible = false;
            progressLabel.Visible = true;
            progressLabel.Text = "Starting...";

            var url = urlTextField.Text?.ToString().Trim() ?? string.Empty;
            var songName = nameTextField.Text?.ToString().Trim() ?? string.Empty;

            string relativePath = "";
            if (!string.IsNullOrEmpty(playlistComboBox.SelectedText))
            {
                relativePath = playlistComboBox.SelectedText.Length == 0 ? "" : playlists.First(s => s == playlistComboBox.SelectedText);
            }

            downloadButton.Enabled = false;
            using var cancellationSource = new CancellationTokenSource();

            Result result;

            var videoId = VideoId.TryParse(url);
            var playlistId = PlaylistId.TryParse(url);
            bool isExplicitPlaylist = url.Contains("playlist?list=", StringComparison.OrdinalIgnoreCase);

            if (playlistId is not null && (videoId is null || isExplicitPlaylist))
            {
                var progress = new Progress<int>(p =>
                {
                    App?.Invoke(() =>
                    {
                        string songText = p == 1 ? "song" : "songs";
                        progressLabel.Text = $"Downloaded {p} {songText}...";
                    });
                });
                result = await youtubeDownloadService.DownloadPlaylistAsync(url, relativePath, progress, cancellationSource.Token);
            }
            else
            {
                var progress = new Progress<double>(p =>
                {
                    App?.Invoke(() =>
                    {
                        int percentage = (int)(p * 100);
                        int bars = percentage / 10;
                        string barStr = new string('#', bars) + new string(' ', 10 - bars);
                        progressLabel.Text = $"[{barStr}] {percentage}%";
                    });
                });
                result = await youtubeDownloadService.DownloadAsync(url, songName, relativePath, progress, cancellationSource.Token);
            }

            downloadButton.Enabled = true;

            if (result.IsFailure)
            {
                progressLabel.Visible = false;
                App?.Invoke(() => MessageBox.ErrorQuery(App, "Error", result.Error, "Ok"));
            }
            else
            {
                urlTextField.Text = string.Empty;
                nameTextField.Text = string.Empty;
                textLabelSuccess.Visible = true;
                progressLabel.Text = "[##########] 100%";

                App?.Invoke(() => uiEventBus.Publish(new RefreshPlaylistsRequested()));
            }
        };

        var exitButton = new Button()
        {
            Title = "Exit",
        };

        exitButton.Accepting += (s, e) => App?.RequestStop(dialog);

        dialog.Add(urlLabel, urlTextField, nameLabel, nameTextField, playlistLabel, playlistComboBox, progressLabel, textLabelSuccess);
        dialog.AddButton(downloadButton);
        dialog.AddButton(exitButton);

        App.Run(dialog);
    }

    private void ShowStreamPlaylistDialog()
    {
        if (App is null) return;

        var urlLabel = new Label()
        {
            Text = "YouTube Playlist URL: ",
            X = 1,
            Y = 1,
        };

        var urlTextField = new TextField()
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill()! - 5,
        };

        var dialog = new Dialog()
        {
            Title = "Stream Playlist",
            Width = Dim.Percent(50),
            Height = Dim.Percent(30),
        };

        var loadButton = new Button()
        {
            Title = "Load",
        };

        loadButton.Accepting += (s, e) =>
        {
            var url = urlTextField.Text?.ToString().Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                App?.Invoke(() => MessageBox.ErrorQuery(App, "Error", "URL cannot be empty", "Ok"));
                return;
            }

            uiEventBus.Publish(new LoadYoutubePlaylist(url));
            App?.RequestStop(dialog);
            e.Handled = true;
        };

        var exitButton = new Button()
        {
            Title = "Exit",
        };

        exitButton.Accepting += (s, e) => App?.RequestStop(dialog);

        dialog.Add(urlLabel, urlTextField);
        dialog.AddButton(loadButton);
        dialog.AddButton(exitButton);

        App.Run(dialog);
    }
}