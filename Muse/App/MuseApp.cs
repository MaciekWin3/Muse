using Muse.Player;
using Muse.UI;
using Muse.UI.Views;
using Muse.YouTube;
using Terminal.Gui.Views;

namespace Muse.App;

public class MuseApp : Toplevel
{
    private readonly IPlayerService player;
    private readonly IYoutubeDownloadService youtubeDownloadService;
    private readonly MainWindow mainWindow;
    private readonly MenuBarView menuBarView;
    private readonly StatusBarView statusBarView;

    public MuseApp(IPlayerService player, IYoutubeDownloadService youtubeDownloadService,
        MainWindow mainWindow, MenuBarView menuBarView, StatusBarView statusBarView)
    {
        this.player = player;
        this.youtubeDownloadService = youtubeDownloadService;
        this.menuBarView = menuBarView;
        this.mainWindow = mainWindow;
        this.statusBarView = statusBarView;
        Add(mainWindow, statusBarView, menuBarView);
    }
}