using Muse.Player;
using Muse.Utils;
using Muse.Windows;
using System.Text;
using Terminal.Gui;
using VideoLibrary;

namespace Muse;

public class App : Toplevel
{
    private readonly static string MUSIC_DIRECTORY = @"C:\\Users\\macie\\Music\\Miszmasz\\";

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
        var button = new Button()
        {
            Title = "Download",
        };

        var urlTextField = new TextField()
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(),
        };

        var spinnerView = new SpinnerView
        {
            X = Pos.Bottom(urlTextField) + 1,
            Y = Pos.Center(),
            Visible = false
        };

        button.Accept += async (s, e) =>
        {
            spinnerView.Visible = true;
            spinnerView.AutoSpin = true;
            var url = urlTextField.Text;
            var result = await SaveVideoToDisk(url);
            if (result.IsFailure)
            {
                MessageBox.ErrorQuery("Error", result.Error, "Ok");
            }
            Application.Refresh();
            spinnerView.Visible = false;
            spinnerView.AutoSpin = false;
        };

        var dialog = new Dialog()
        {
            Title = "Download",
            Width = Dim.Percent(50),
            Height = Dim.Percent(50),
        };
        dialog.Add(urlTextField);
        dialog.Add(spinnerView);
        dialog.AddButton(button);
        Application.Run(dialog);
    }

    public async Task<Result> SaveVideoToDisk(string link)
    {
        var youTube = YouTube.Default;
        try
        {
            var video = await youTube.GetVideoAsync(link);
            File.WriteAllBytes(MUSIC_DIRECTORY + video.FullName, video.GetBytes());
        }
        catch (Exception e)
        {
            return Result.Fail(e.Message);
        }
        return Result.Ok();
    }
}