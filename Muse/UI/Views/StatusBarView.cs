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
                var muteShortcutSubView = SubViews.FirstOrDefault(s => s.Title.Contains("Mute", StringComparison.OrdinalIgnoreCase));
                if (muteShortcutSubView is not null)
                {
                    uiEventBus.Publish(new MuteToggle(muteShortcutSubView.Title == "Mute"));
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
            Title = "Repeat: None",
            Key = Key.R,
            Action = () => uiEventBus.Publish(new TogglePlayModeRequested())
        });

        Add(new Shortcut()
        {
            Title = "Shuffle: Off",
            Key = Key.S,
            Action = () => uiEventBus.Publish(new ShuffleToggleRequested())
        });

        Add(new Shortcut()
        {
            Title = "Switch Mode",
            Key = Key.Tab,
            Action = () => {
                var currentTitle = SubViews.OfType<Shortcut>().FirstOrDefault(s => s.Title.StartsWith("Mode:"))?.Title ?? "";
                var nextMode = currentTitle.Contains("Search") ? AppMode.Shortcuts : AppMode.Search;
                uiEventBus.Publish(new ChangeModeRequested(nextMode));
            }
        });

        Add(new Shortcut()
        {
            Title = "Mode: Shortcuts",
        });

        Add(new Shortcut()
        {
            Title = $"{Environment.OSVersion}"
        });
    }

    private void UpdateShortcutsVisibility(AppMode mode)
    {
        var playerShortcuts = new[] { "Mute", "Unmute", "Play/Pause", "Prev", "Next", "Delete", "Repeat", "Shuffle" };
        foreach (var shortcut in SubViews.OfType<Shortcut>())
        {
            if (playerShortcuts.Any(s => shortcut.Title.StartsWith(s, StringComparison.OrdinalIgnoreCase)))
            {
                shortcut.Visible = mode == AppMode.Shortcuts;
            }
        }
    }

    private void RegisterBusHandlers()
    {
        uiEventBus.Subscribe<ChangeModeRequested>(msg =>
        {
            Application.Invoke(() =>
            {
                var modeShortcut = SubViews.OfType<Shortcut>().FirstOrDefault(s => s.Title.StartsWith("Mode:"));
                if (modeShortcut != null)
                {
                    modeShortcut.Title = $"Mode: {msg.NewMode}";
                }
                UpdateShortcutsVisibility(msg.NewMode);
            });
        });

        uiEventBus.Subscribe<VolumeChanged>(msg =>
        {
            Application.Invoke(() =>
            {
                var muteShortcut = SubViews.OfType<Shortcut>().FirstOrDefault(s => s.Title.Contains("Mute", StringComparison.OrdinalIgnoreCase));
                if (muteShortcut != null)
                {
                    muteShortcut.Title = msg.Volume == 0f ? "Unmute" : "Mute";
                }
            });
        });

        uiEventBus.Subscribe<PlayModeChanged>(msg =>
        {
            Application.Invoke(() =>
            {
                var repeatShortcut = SubViews.OfType<Shortcut>().FirstOrDefault(s => s.Title.StartsWith("Repeat:"));
                if (repeatShortcut != null)
                {
                    repeatShortcut.Title = msg.NewMode switch
                    {
                        PlayMode.None => "Repeat: None",
                        PlayMode.Repeat => "Repeat: All",
                        PlayMode.RepeatOne => "Repeat: One",
                        _ => "Repeat"
                    };
                }
            });
        });

        uiEventBus.Subscribe<ShuffleChanged>(msg =>
        {
            Application.Invoke(() =>
            {
                var shuffleShortcut = SubViews.OfType<Shortcut>().FirstOrDefault(s => s.Title.StartsWith("Shuffle:"));
                if (shuffleShortcut != null)
                {
                    shuffleShortcut.Title = msg.IsShuffle ? "Shuffle: On" : "Shuffle: Off";
                }
            });
        });
    }
}
