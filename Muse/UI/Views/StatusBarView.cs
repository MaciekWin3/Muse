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
                uiEventBus.Publish(new VolumeChanged(0));
            }
        });

        Add(new Shortcut()
        {
            Title = $"OS: {Environment.OSVersion}"
        });
    }
}
