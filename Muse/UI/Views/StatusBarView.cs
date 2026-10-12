using Muse.UI.Bus;
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

        AddShortcuts();
        RegisterBusHandlers();

        UpdateShortcutsVisibility(AppMode.Shortcuts);
    }

    private void AddShortcuts()
    {
        Add(new Shortcut()
        {
            Title = "Mute",
            Key = Key.Backspace,
            Action = () =>
            {
                var muteShortcut = SubViews.OfType<Shortcut>().FirstOrDefault(s => s.Title.Contains("Mute", StringComparison.OrdinalIgnoreCase));
                if (muteShortcut is not null)
                {
                    // If current title is "Mute", it's unmuted, so we want to mute (true).
                    // If it is anything else (like "Unmute"), we want to unmute (false).
                    bool shouldMute = muteShortcut.Title.Equals("Mute", StringComparison.OrdinalIgnoreCase);
                    uiEventBus.Publish(new MuteToggle(shouldMute));
                }
            }
        });

        Add(new Shortcut()
        {
            Title = "Play/Pause",
            Key = Key.P,
            Action = () => uiEventBus.Publish(new TogglePlayRequested())
        });

        Add(new Shortcut()
        {
            Title = "Prev",
            Key = Key.B,
            Action = () => uiEventBus.Publish(new PreviousSongRequested())
        });

        Add(new Shortcut()
        {
            Title = "Next",
            Key = Key.N,
            Action = () => uiEventBus.Publish(new NextSongRequested())
        });

        Add(new Shortcut()
        {
            Title = "Delete",
            Key = Key.D,
            Action = () => uiEventBus.Publish(new DeleteSongRequested())
        });

        Add(new Shortcut()
        {
            Title = "Switch Mode",
            Key = Key.Tab,
            Action = () =>
            {
                var currentTitle = SubViews.OfType<Shortcut>().FirstOrDefault(s => s.Title.StartsWith("Mode:"))?.Title ?? "";
                var nextMode = currentTitle.Contains("Search") ? AppMode.Shortcuts : AppMode.Search;
                uiEventBus.Publish(new ChangeModeRequested(nextMode));
            }
        });

        Add(new Shortcut()
        {
            Title = "Mode: Shortcuts",
        });
    }

    private void UpdateShortcutsVisibility(AppMode mode)
    {
        var playerShortcuts = new[] { "Mute", "Unmute", "Play/Pause", "Prev", "Next", "Delete" };
        foreach (var shortcut in SubViews.OfType<Shortcut>())
        {
            if (playerShortcuts.Any(s => shortcut.Title.StartsWith(s, StringComparison.OrdinalIgnoreCase)))
            {
                shortcut.Visible = true;
            }
        }
    }

    private void RegisterBusHandlers()
    {
        uiEventBus.Subscribe<ChangeModeRequested>(msg =>
        {
            App?.Invoke(() =>
            {
                var modeShortcut = SubViews.OfType<Shortcut>().FirstOrDefault(s => s.Title.StartsWith("Mode:"));
                if (modeShortcut is not null)
                {
                    modeShortcut.Title = $"Mode: {msg.NewMode}";
                }
                UpdateShortcutsVisibility(msg.NewMode);
            });
        });

        uiEventBus.Subscribe<VolumeChanged>(msg =>
        {
            App?.Invoke(() =>
            {
                var muteShortcut = SubViews.OfType<Shortcut>().FirstOrDefault(s => s.Title.Contains("Mute", StringComparison.OrdinalIgnoreCase));
                if (muteShortcut is not null)
                {
                    muteShortcut.Title = msg.Volume == 0f ? "Unmute" : "Mute";
                }
            });
        });

    }
}