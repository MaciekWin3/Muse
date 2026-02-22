using Muse.UI.Bus;
using Muse.Utils;
using Muse.YouTube;
using System.Collections.ObjectModel;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI.Views;

public class MenuBarView : MenuBarv2
{
    private readonly IYoutubeDownloadService youtubeDownloadService;
    private readonly IUiEventBus uiEventBus;
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
    }

    private void RebuildMenu()
    {
            Menus =
                [
                    new("File", new MenuItemv2[]
                    {
                        new("Open", "Open music folder", () => OpenFolder()),
                        new("Quit", "Quit application", () => Application.RequestStop()),
                    }),
                    new("Playlists", GetPlaylistMenuItems()),
                    new("Help", new MenuItemv2[]
                    {
                        new("About", "About Muse", () => ShowAsciiArt()),
                        new("Shortcuts", "Show shortcuts", () => ShowShortcuts()),
                        new("Website", "Muse Website", () => WebsiteHelper.OpenUrl("https://github.com/MaciekWin3/Muse"))
                    }),
                    new("Download", new MenuItemv2[]
                    {
                        new("From YT", "Download file from YT", () => ShowDownloadDialog())
                    })
                ];
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

        MessageBox.Query(50, 15, "Shortcuts", sb.ToString(), "Ok");
    }

    private void ShowDownloadDialog()
    {
        var urlLabel = new Label()
        {
            Title = "YouTube URL: ",
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
            Title = "Song name: ",
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
            Title = "Playlist: ",
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

        var progressBar = new ProgressBar()
        {
            X = 1,
            Y = Pos.Bottom(playlistComboBox) + 1,
            Width = Dim.Fill()! - 5,
            Height = 1,
            Visible = false
        };

        var textLabelSuccess = new Label()
        {
            Title = "Downloaded successfully!",
            X = Pos.Center(),
            Y = Pos.Bottom(progressBar) + 1,
            Visible = false,
        };

        var dialog = new Dialog()
        {
            Title = "Download",
            Width = Dim.Percent(50),
            Height = Dim.Percent(60),
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
            if (string.IsNullOrWhiteSpace(urlTextField.Text.ToString()))
            {
                MessageBox.ErrorQuery("Error", "URL cannot be empty", "Ok");
                return;
            }

            // Preparation 
            textLabelSuccess.Visible = false;
            progressBar.Visible = true;
            progressBar.Fraction = 0;
            
            var url = urlTextField.Text.ToString();
            var songName = nameTextField.Text.ToString();
            var relativePath = playlistComboBox.SelectedItem == 0 ? "" : playlists[playlistComboBox.SelectedItem];

            var progress = new Progress<double>(p =>
            {
                Application.Invoke(() =>
                {
                    progressBar.Fraction = (float)p;
                });
            });

            downloadButton.Enabled = false;

            var result = await youtubeDownloadService.DownloadAsync(url, songName, relativePath, progress);

            downloadButton.Enabled = true;
            progressBar.Visible = false;

            if (result.IsFailure)
            {
                MessageBox.ErrorQuery("Error", result.Error, "Ok");
            }
            else
            {
                urlTextField.Text = string.Empty;
                nameTextField.Text = string.Empty;
                textLabelSuccess.Visible = true;
                
                // Refresh playlists if new folder created (though here we only select existing)
                // Trigger playlist reload if we downloaded to current folder
                Application.Invoke(() => uiEventBus.Publish(new RefreshPlaylistsRequested()));
            }

        };

        var exitButton = new Button()
        {
            Title = "Exit",
            X = Pos.Center(),
            Y = Pos.Percent(90),
        };

        exitButton.Accepting += (s, e) => dialog.Running = false;

        dialog.Add(urlLabel);
        dialog.Add(urlTextField);
        dialog.Add(nameLabel);
        dialog.Add(nameTextField);
        dialog.Add(playlistLabel);
        dialog.Add(playlistComboBox);
        dialog.Add(progressBar);
        dialog.Add(textLabelSuccess);
        dialog.AddButton(downloadButton);
        dialog.AddButton(exitButton);

        Application.Run(dialog);
    }
}