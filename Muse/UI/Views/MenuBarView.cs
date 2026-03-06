using Muse.UI.Bus;
using Muse.Utils;
using Muse.YouTube;
using System.Collections.ObjectModel;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI.Views;

public class MenuBarView : MenuBarv2
{
    private readonly IYoutubeDownloadService youtubeDownloadService;
    private readonly IUiEventBus uiEventBus;

    private PlayMode playMode = PlayMode.None;
    private bool isShuffle = false;

    public MenuBarView(IYoutubeDownloadService youtubeDownloadService, IUiEventBus uiEventBus)
    {
        this.youtubeDownloadService = youtubeDownloadService;
        this.uiEventBus = uiEventBus;

        RebuildMenu();

        uiEventBus.Subscribe<RefreshPlaylistsRequested>(_ =>
        {
            Application.Invoke(() =>
            {
                RebuildMenu();
            });
        });

        uiEventBus.Subscribe<PlayModeChanged>(msg =>
        {
            Application.Invoke(() =>
            {
                playMode = msg.NewMode;
                RebuildMenu();
            });
        });

        uiEventBus.Subscribe<ShuffleChanged>(msg =>
        {
            Application.Invoke(() =>
            {
                isShuffle = msg.IsShuffle;
                RebuildMenu();
            });
        });
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
            new("File", new MenuItemv2[]
            {
                new("Open", "Open music folder", () => OpenFolder()),
                new("Quit", "Quit application", () => Application.RequestStop()),
            }),
            new("Options", new MenuItemv2[]
            {
                new("Playlists", "Choose playlist", new Menuv2(GetPlaylistMenuItems())),
                new("Theme", "Change color theme", new Menuv2(GetThemeMenuItems())),
                new("Download", "Download from YouTube", () => ShowDownloadDialog()),
                new("Stream Playlist", "Stream YouTube playlist", () => ShowStreamPlaylistDialog())
            }),
            new("Playback", new MenuItemv2[]
            {
                new(repeatTitle, "Toggle repeat mode (R)", () => uiEventBus.Publish(new TogglePlayModeRequested())),
                new(shuffleTitle, "Toggle shuffle mode (S)", () => uiEventBus.Publish(new ShuffleToggleRequested()))
            }),
            new("Help", new MenuItemv2[]
            {
                new("About", "About Muse", () => ShowAsciiArt()),
                new("Shortcuts", "Show shortcuts", () => ShowShortcuts()),
                new("Website", "Muse Website", () => WebsiteHelper.OpenUrl("https://github.com/MaciekWin3/Muse"))
            })
        ];
    }

    private MenuItemv2[] GetThemeMenuItems()
    {
        return new MenuItemv2[]
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

    private MenuItemv2[] GetPlaylistMenuItems()
            {
                var items = new List<MenuItemv2>
                {
                    new("All Songs", "Show all songs", () =>
                    {
                        Application.Invoke(() => uiEventBus.Publish(new ReloadPlaylist(Globals.MuseDirectory)));
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
                            items.Add(new MenuItemv2(dirName, $"Show songs from {dirName}", () =>
                            {
                                Application.Invoke(() => uiEventBus.Publish(new ReloadPlaylist(dir)));
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.ErrorQuery("Error", $"Failed to list playlists: {ex.Message}", "Ok");
                    }
                }
        
                return items.ToArray();
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

        Application.Run(fileExplorerDialog);

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
                Application.Invoke(() => uiEventBus.Publish(new ReloadPlaylist(selectedPath)));
            }
        }
    }

    private MainWindowView? FindMainWindow()
    {
        if (Application.Top is null)
        {
            return null;
        }
        return FindIn([.. Application.Top.SubViews]);
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

    private static void ShowAsciiArt()
    {
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
        MessageBox.Query(50, 15, "About", sb.ToString(), "Ok");
    }

    private static void ShowShortcuts()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Global shortcuts:");
        sb.AppendLine("Quit: Esc");
        sb.AppendLine("Mute: Backspace");
        sb.AppendLine();
        sb.AppendLine("Player shortcuts:");
        sb.AppendLine("p  - Play/Pause");
        sb.AppendLine("n  - Next track");
        sb.AppendLine("b  - Previous track");
        sb.AppendLine("r  - Repeat mode");
        sb.AppendLine("s  - Shuffle mode");

        MessageBox.Query(50, 15, "Shortcuts", sb.ToString(), "Ok");
    }

    private void ShowDownloadDialog()
    {
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
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();
            playlists.AddRange(subDirs);
        }

        var playlistComboBox = new ComboBox()
        {
            X = 1,
            Y = Pos.Bottom(playlistLabel),
            Width = Dim.Fill()! - 5,
            Height = 4,
            Source = new ListWrapper<string>(new ObservableCollection<string>(playlists))
        };
        playlistComboBox.SelectedItem = 0;

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

        // Download file from YouTube
        downloadButton.Accepting += async (s, e) =>
        {
            // Download
            e.Handled = true;
            
            // Validation
            if (string.IsNullOrWhiteSpace(urlTextField.Text?.ToString()))
            {
                Application.Invoke(() => MessageBox.ErrorQuery("Error", "URL cannot be empty", "Ok"));
                return;
            }

            // Preparation 
            textLabelSuccess.Visible = false;
            progressLabel.Visible = true;
            progressLabel.Text = "Starting...";
            
            var url = urlTextField.Text?.ToString().Trim() ?? string.Empty;
            var songName = nameTextField.Text?.ToString().Trim() ?? string.Empty;
            
            string relativePath = "";
            if (playlistComboBox.SelectedItem >= 0 && playlistComboBox.SelectedItem < playlists.Count)
            {
                relativePath = playlistComboBox.SelectedItem == 0 ? "" : playlists[playlistComboBox.SelectedItem];
            }

            var progress = new Progress<double>(p =>
            {
                Application.Invoke(() =>
                {
                    int percentage = (int)(p * 100);
                    int bars = percentage / 10;
                    string barStr = new string('#', bars) + new string(' ', 10 - bars);
                    progressLabel.Text = $"[{barStr}] {percentage}%";
                });
            });

            downloadButton.Enabled = false;

            var result = await youtubeDownloadService.DownloadAsync(url, songName, relativePath, progress);

            downloadButton.Enabled = true;
            
            if (result.IsFailure)
            {
                progressLabel.Visible = false;
                Application.Invoke(() => MessageBox.ErrorQuery("Error", result.Error, "Ok"));
            }
            else
            {
                urlTextField.Text = string.Empty;
                nameTextField.Text = string.Empty;
                textLabelSuccess.Visible = true;
                progressLabel.Text = "[##########] 100%";
                
                // Refresh playlists if new folder created (though here we only select existing)
                // Trigger playlist reload if we downloaded to current folder
                Application.Invoke(() => uiEventBus.Publish(new RefreshPlaylistsRequested()));
            }

        };

        var exitButton = new Button()
        {
            Title = "Exit",
        };

        exitButton.Accepting += (s, e) => Application.RequestStop(dialog);

        dialog.Add(urlLabel);
        dialog.Add(urlTextField);
        dialog.Add(nameLabel);
        dialog.Add(nameTextField);
        dialog.Add(playlistLabel);
        dialog.Add(playlistComboBox);
        dialog.Add(progressLabel);
        dialog.Add(textLabelSuccess);
        dialog.AddButton(downloadButton);
        dialog.AddButton(exitButton);

        Application.Run(dialog);
    }

    private void ShowStreamPlaylistDialog()
    {
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
                Application.Invoke(() => MessageBox.ErrorQuery("Error", "URL cannot be empty", "Ok"));
                return;
            }

            uiEventBus.Publish(new LoadYoutubePlaylist(url));
            Application.RequestStop(dialog);
            e.Handled = true;
        };

        var exitButton = new Button()
        {
            Title = "Exit",
        };

        exitButton.Accepting += (s, e) => Application.RequestStop(dialog);

        dialog.Add(urlLabel);
        dialog.Add(urlTextField);
        dialog.AddButton(loadButton);
        dialog.AddButton(exitButton);

        Application.Run(dialog);
    }
}