using Muse.UI.Bus;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI.Views;

public class StatusBarView : StatusBar
{
    private readonly IUiEventBus uiEventBus;
    public StatusBarView(IUiEventBus uiEventBus)
    {
        this.uiEventBus = uiEventBus;

        AlignmentModes = AlignmentModes.IgnoreFirstOrLast;
        CanFocus = false;

        Add(new Shortcut()
        {
            Title = "Quit",
            Key = Application.Keyboard.QuitKey,
        });

        Add(new Shortcut()
        {
            Title = "Mute",
            Key = Key.Backspace,
            Action = () =>
            {
                var muteShortcutSubView = SubViews.FirstOrDefault(s => s.Title.Contains("Mute", StringComparison.OrdinalIgnoreCase));
                if (muteShortcutSubView is not null)
                {
                    uiEventBus.Publish(new MuteToggle(muteShortcutSubView.Title == "Mute"));
                }
            }
        });

        Add(new Shortcut()
        {
            Title = $"OS: {Environment.OSVersion}"
        });

        RegisterBusHandlers();
    }

    private void RegisterBusHandlers()
    {
        uiEventBus.Subscribe<VolumeChanged>(msg =>
        {
            Application.Invoke(() =>
            {
                var muteShortcut = SubViews.FirstOrDefault(s => s.Title.Contains("Mute", StringComparison.OrdinalIgnoreCase));
                muteShortcut?.Title = msg.Volume == 0f ? "Unmute" : "Mute";
            });
        });
    }
}
