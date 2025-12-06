using Muse.Utils;
using Muse.YouTube;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI.Views;

public class MenuBarView : MenuBarv2
{
    private readonly IYoutubeDownloadService youtubeDownloadService;
    public MenuBarView(IYoutubeDownloadService youtubeDownloadService)
    {
        this.youtubeDownloadService = youtubeDownloadService;

        Menus =
[
            new("File", new MenuItemv2[]
                {
                    new("Open", "Open music folder", () => OpenFolder()),
                    new("Quit", "Quit application", () => Application.RequestStop()),
                }),
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

        this.youtubeDownloadService = youtubeDownloadService;
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

        var selectedPath = fileExplorerDialog.FilePaths?.FirstOrDefault()?.ToString();
        if (!string.IsNullOrWhiteSpace(selectedPath) && Directory.Exists(selectedPath))
        {
            var mw = FindMainWindow();
            if (mw is not null)
            {
                Globals.MuseDirectory = selectedPath;
                Application.Invoke(() => mw.ReloadPlaylist(selectedPath));
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

        var spinnerView = new SpinnerView
        {
            X = Pos.Right(urlTextField) + 2,
            Y = 2,
            Visible = false
        };

        var textLabelSuccess = new Label()
        {
            Title = "Downloaded successfully!",
            X = Pos.Center(),
            Y = Pos.Bottom(nameTextField) + 1,
            Visible = false,
        };

        var dialog = new Dialog()
        {
            Title = "Download",
            Width = Dim.Percent(50),
            Height = Dim.Percent(50),
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
            // Preparation 
            textLabelSuccess.Visible = false;
            spinnerView.Visible = true;
            spinnerView.AutoSpin = true;
            var url = urlTextField.Text;
            var songName = nameTextField.Text;

            var result = await youtubeDownloadService.DownloadAsync(url, songName);

            if (result.IsFailure)
            {
                MessageBox.ErrorQuery("Error", result.Error, "Ok");
            }
            else
            {
                urlTextField.Text = string.Empty;
                nameTextField.Text = string.Empty;
                textLabelSuccess.Visible = true;
                spinnerView.Visible = false;
                spinnerView.AutoSpin = false;
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
        dialog.Add(spinnerView);
        dialog.Add(textLabelSuccess);
        dialog.AddButton(downloadButton);
        dialog.AddButton(exitButton);

        Application.Run(dialog);
    }
}