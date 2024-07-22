using Muse.Player.Interfaces;
using Muse.Windows;
using System.Text;
using Terminal.Gui;

namespace Muse;

public class App : Toplevel
{
    private readonly IPlayer player;
    private MenuBar menuBar = null!;

    public App(IPlayer player)
    {
        this.player = player;
    }

    public void Run(string[] args)
    {
        Application.Init();
        Add(InitMenuBar());
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
                })
            ]
        };

        return menuBar;
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
}