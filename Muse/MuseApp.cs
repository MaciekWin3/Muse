using Muse.Player;
using Muse.Utils;
using Muse.Windows;
using System.Diagnostics;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Muse;

public class MuseApp : Toplevel
{
    private readonly string MUSIC_DIRECTORY =
        Environment.GetEnvironmentVariable("MUSE_DIRECTORY")
            ?? throw new Exception("MUSE_DIRECTORY environment variable is not set.");

    private readonly IPlayerService player; // TODO: check if needed
    private MenuBarv2 menuBar = null!;
    private StatusBar statusBar = null!;
    private MainWindow mainWindow = null!;

    public MuseApp(IPlayerService player)
    {
        this.player = player;
        mainWindow = new MainWindow(player);
        menuBar = InitMenuBar();
        statusBar = InitStatusBar();
        Add(mainWindow, statusBar, menuBar);
    }

    public MenuBarv2 InitMenuBar()
    {
        menuBar = new MenuBarv2
        {
            Menus =
            [
                new("File", new MenuItemv2[]
                {
                    new("Open", "Open music folder", () => { }),
                    new("Quit", "Quit application", () => Application.RequestStop()),
                }),
                new("Help", new MenuItemv2[]
                {
                    new("About", "About Muse", () => ShowAsciiArt()),
                    new("Website", "Muse Website", () => WebsiteHelper.OpenUrl("https://github.com/MaciekWin3/Muse"))
                }),
                new("Download", new MenuItemv2[]
                {
                    new("From YT", "Download file from YT", () => ShowDownloadDialog())
                })
            ]
        };

        return menuBar;
    }

    public StatusBar InitStatusBar()
    {
        statusBar = new StatusBar
        {
            AlignmentModes = AlignmentModes.IgnoreFirstOrLast,
            CanFocus = false,
        };

        statusBar.Add(new Shortcut()
        {
            Title = "Quit",
            Key = Application.Keyboard.QuitKey,
        });

        // TODO: Verify do we want to keep mute option
        statusBar.Add(new Shortcut()
        {
            Title = "Mute",
            Key = Key.M,
            Action = () =>
            {
                if (mainWindow.volumeSlider.FocusedOption != 0)
                {
                    mainWindow.volumeSlider.SetOption(0);
                }
                else
                {
                    mainWindow.volumeSlider.SetOption(5);
                }
            }
        });

        statusBar.Add(new Shortcut()
        {
            Title = $"OS: {Environment.OSVersion}"
        });

        return statusBar;
    }

    private void ShowAsciiArt()
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
            // TODO
            //ColorScheme = new(new Terminal.Gui.Attribute(Color.Green, Color.Black))
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

            var result = await DownloadFromYoutube(url, songName);
            if (result.IsFailure)
            {
                MessageBox.ErrorQuery("Error", result.Error, "Ok");
            }
            else
            {
                // Cleanup
                // toDO
                //Application.Refresh();
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

    public async Task<Result> DownloadFromYoutube(string link, string name = null!)
    {
        var youtube = new YoutubeClient();
        try
        {
            var videoInfo = await youtube.Videos.GetAsync(link);
            if (!Directory.Exists(MUSIC_DIRECTORY))
            {
                Directory.CreateDirectory(MUSIC_DIRECTORY);
            }
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(link);
            var streamInfo = streamManifest.GetAudioOnlyStreams().Where(s => s.Container == Container.Mp4).GetWithHighestBitrate();
            var stream = await youtube.Videos.Streams.GetAsync(streamInfo);
            Debug.WriteLine(videoInfo.Author);
            await youtube.Videos.Streams.DownloadAsync(streamInfo, @$"{MUSIC_DIRECTORY}\{name ?? videoInfo.Title}.{streamInfo.Container}");
        }
        catch (Exception e)
        {
            return Result.Fail(e.Message);
        }
        return Result.Ok();
    }
}