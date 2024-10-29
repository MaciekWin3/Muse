using Muse.Player;
using Muse.Utils;
using Muse.Windows;
using System.Diagnostics;
using System.Text;
using Terminal.Gui;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Muse;

public class App : Toplevel
{
    private readonly static string MUSIC_DIRECTORY = @"C:\Users\macie\Music\Miszmasz";

    private readonly IPlayer player;
    private MenuBar menuBar = null!;
    private StatusBar statusBar = null!;

    public App(IPlayer player)
    {
        this.player = player;
    }

    public void Run(string[] args)
    {
        Application.Init();
        Add(InitMenuBar());
        Add(InitStatusBar());
        var win = new MainWindow(player);
        Add(win);
        Application.Run(this);
        Application.Shutdown();
        player.Dispose();
    }

    private MenuBar InitMenuBar()
    {
        menuBar = new MenuBar
        {
            Menus =
            [
                new("_File", new MenuItem[]
                {
                    new("_Quit", "", () => Application.RequestStop())
                }),
                new("_Help", new MenuItem[]
                {
                    new("_About", "", () => ShowAsciiArt())
                }),
                new("_Download", new MenuItem[]
                {
                    new("_From YT", "", () => ShowDownloadDialog())
                })
            ]
        };

        return menuBar;
    }

    private StatusBar InitStatusBar()
    {
        statusBar = new StatusBar();
        statusBar.Add(new Shortcut()
        {
            Title = "Quit",
            Key = Application.QuitKey,
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
        MessageBox.Query(50, 12, "About", sb.ToString(), "Ok");
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
            ColorScheme = new(new Terminal.Gui.Attribute(Color.Green, Color.Black))
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
        downloadButton.Accept += async (s, e) =>
        {
            // Preparation 
            textLabelSuccess.Visible = false;
            spinnerView.Visible = true;
            spinnerView.AutoSpin = true;
            var url = urlTextField.Text;
            var songName = nameTextField.Text;

            // Download
            var result = await SaveVideoToDisk(url, songName);
            if (result.IsFailure)
            {
                MessageBox.ErrorQuery("Error", result.Error, "Ok");
            }

            // Cleanup
            Application.Refresh();
            spinnerView.Visible = false;
            spinnerView.AutoSpin = false;
            urlTextField.Text = "";
            textLabelSuccess.Visible = true;
        };

        var exitButton = new Button()
        {
            Title = "Exit",
            X = Pos.Center(),
            Y = Pos.Percent(90),
        };

        exitButton.Accept += (s, e) => dialog.Running = false;

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

    public async Task<Result> SaveVideoToDisk(string link, string name = null)
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