using Muse.UI.Bus;
using Muse.Utils;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI.Views;

public sealed class VolumeView : ProgressBar
{
    private readonly IUiEventBus uiBus;
    private float _previousVolume = 0.5f;

    public VolumeView(IUiEventBus uiBus, Pos x, Pos y)
    {
        this.uiBus = uiBus;

        X = x;
        Y = y;
        Height = Globals.VOLUME_SLIDER_HEIGHT; // 4
        Width = Dim.Fill();

        Title = $"Volume ({(int)(Globals.Volume * 100)}%)";
        
        // Differentiate from Song Progress (which is Rounded/Continuous)
        BorderStyle = LineStyle.Single;
        ProgressBarStyle = ProgressBarStyle.Blocks;
        
        Fraction = Globals.Volume;
        CanFocus = true;

        RegisterBusHandlers();
        RegisterInteractions();
    }

    private void RegisterBusHandlers()
    {
        uiBus.Subscribe<VolumeChanged>(msg =>
        {
            Application.Invoke(() =>
            {
                Fraction = msg.Volume;
                Title = $"Volume ({(int)(msg.Volume * 100)}%)";
            });
        });

        uiBus.Subscribe<MuteToggle>(msg =>
        {
            if (msg.IsMuted)
            {
                _previousVolume = Fraction;
                UpdateVolume(0);
            }
            else
            {
                UpdateVolume(_previousVolume);
            }
        });
    }

    private void RegisterInteractions()
    {
        // Mouse interaction
        Application.Mouse.MouseEvent += (sender, e) =>
        {
            if (e.View != this)
                return;

            if (e.Flags.HasFlag(MouseFlags.Button1Clicked) || e.Flags.HasFlag(MouseFlags.Button1Pressed))
            {
                var width = (float)Viewport.Width;
                var position = (float)e.Position.X;
                var fraction = Math.Clamp(position / width, 0f, 1f);
                UpdateVolume(fraction);
            }
        };

        // Keyboard interaction
        KeyDown += (sender, e) =>
        {
            float step = 0.02f; // 2% step
            if (e == Key.CursorLeft)
            {
                UpdateVolume(Math.Clamp(Fraction - step, 0f, 1f));
                e.Handled = true;
            }
            else if (e == Key.CursorRight)
            {
                UpdateVolume(Math.Clamp(Fraction + step, 0f, 1f));
                e.Handled = true;
            }
            else if (e == Key.Home)
            {
                UpdateVolume(0f);
                e.Handled = true;
            }
            else if (e == Key.End)
            {
                UpdateVolume(1f);
                e.Handled = true;
            }
        };
    }

    private void UpdateVolume(float volume)
    {
        Fraction = volume;
        Title = $"Volume ({(int)(volume * 100)}%)";
        uiBus.Publish(new VolumeChanged(volume));
    }
}
