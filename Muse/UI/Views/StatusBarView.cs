using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI.Views;

public class StatusBarView : StatusBar
{
    private readonly MainWindow mainWindow;
    public StatusBarView(MainWindow mainWindow)
    {
        this.mainWindow = mainWindow;

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

        Add(new Shortcut()
        {
            Title = $"OS: {Environment.OSVersion}"
        });
    }
}
