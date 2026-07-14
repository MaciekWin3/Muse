using Muse.Player;
using Muse.UI;
using Muse.UI.Bus;
using Muse.UI.Views;
using Muse.YouTube;
using Terminal.Gui.Configuration;
using Terminal.Gui.Input;
using Terminal.Gui.Views;

namespace Muse.App;

public class MuseApp : Window
{
    private readonly IPlayerService player;
    private readonly IYoutubeDownloadService youtubeDownloadService;
    private readonly MainWindowView mainWindow;
    private readonly MenuBarView menuBarView;
    private readonly StatusBarView statusBarView;
    private readonly IUiEventBus uiEventBus;
    private AppMode currentMode = AppMode.Shortcuts;

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

        uiEventBus.Subscribe<ChangeThemeRequested>(msg =>
        {
            // Since we can't use static Application, invoke through the View's 
            // built-in Application Context if available, or run inline safely.
            void ApplyTheme()
            {
                ThemeManager.Theme = msg.ThemeName;
                // TuiConfigurationBuilder (the suggested replacement) is an undocumented,
                // still-evolving API in this Terminal.Gui prerelease. Stay on the static
                // ConfigurationManager facade until a migration guide is published.
#pragma warning disable CS0618
                ConfigurationManager.Apply();
#pragma warning restore CS0618
            }

            ApplyTheme();
        });

        uiEventBus.Subscribe<ChangeModeRequested>(msg =>
        {
            currentMode = msg.NewMode;
        });
    }

    // Instead of subscribing/unsubscribing to a dirty global hook,
    // override the View's built-in Preview or standard KeyDown method.
    protected override bool OnKeyDown(Key key)
    {
        // If a child TextField has focus, let it steal input natively without bypassing it
        if (MostFocused is TextField)
        {
            return base.OnKeyDown(key);
        }

        // Mode Toggling
        if (key == Key.Tab)
        {
            currentMode = currentMode == AppMode.Search ? AppMode.Shortcuts : AppMode.Search;
            uiEventBus.Publish(new ChangeModeRequested(currentMode));
            key.Handled = true;
            return true;
        }

        if (currentMode != AppMode.Shortcuts)
        {
            return base.OnKeyDown(key);
        }

        if (key == Key.P)
        {
            uiEventBus.Publish(new TogglePlayRequested());
            key.Handled = true;
            return true;
        }
        if (key == Key.N)
        {
            uiEventBus.Publish(new NextSongRequested());
            key.Handled = true;
            return true;
        }
        if (key == Key.B)
        {
            uiEventBus.Publish(new PreviousSongRequested());
            key.Handled = true;
            return true;
        }
        if (key == Key.D)
        {
            uiEventBus.Publish(new DeleteSongRequested());
            key.Handled = true;
            return true;
        }
        if (key == Key.R)
        {
            uiEventBus.Publish(new TogglePlayModeRequested());
            key.Handled = true;
            return true;
        }
        if (key == Key.S)
        {
            uiEventBus.Publish(new ShuffleToggleRequested());
            key.Handled = true;
            return true;
        }

        return base.OnKeyDown(key);
    }
}