using Muse.Player;
using Muse.UI;
using Muse.UI.Bus;
using Muse.UI.Views;
using Muse.YouTube;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Input;
using Terminal.Gui.Views;

namespace Muse.App;

public class MuseApp : Toplevel
{
    private readonly IPlayerService player;
    private readonly IYoutubeDownloadService youtubeDownloadService;
    private readonly MainWindowView mainWindow;
    private readonly MenuBarView menuBarView;
    private readonly StatusBarView statusBarView;
    private readonly IUiEventBus uiEventBus;

    public MuseApp(IPlayerService player, IYoutubeDownloadService youtubeDownloadService,
        MainWindowView mainWindow, MenuBarView menuBarView, StatusBarView statusBarView,
        IUiEventBus uiEventBus)
    {
        this.player = player;
        this.youtubeDownloadService = youtubeDownloadService;
        this.menuBarView = menuBarView;
        this.mainWindow = mainWindow;
        this.statusBarView = statusBarView;
        this.uiEventBus = uiEventBus;
        Add(mainWindow, statusBarView, menuBarView);
        Initialized += (s, e) => Application.KeyDown += OnGlobalKeyDown;

        uiEventBus.Subscribe<ChangeThemeRequested>(msg =>
        {
            Application.Invoke(() =>
            {
                ThemeManager.Theme = msg.ThemeName;
                ConfigurationManager.Apply();
            });
        });
    }

    private void OnGlobalKeyDown(object? sender, Key key)
    {
        if (Application.Top is not MuseApp || Application.Top.MostFocused is TextField)
        {
            return;
        }

        if (key == Key.P)
        {
            uiEventBus.Publish(new TogglePlayRequested());
            key.Handled = true;
        }
        else if (key == Key.N)
        {
            uiEventBus.Publish(new NextSongRequested());
            key.Handled = true;
        }
        else if (key == Key.B)
        {
            uiEventBus.Publish(new PreviousSongRequested());
            key.Handled = true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Application.KeyDown -= OnGlobalKeyDown;
        }
        base.Dispose(disposing);
    }
}