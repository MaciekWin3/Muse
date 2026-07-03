using Muse.UI.Bus;
using Muse.Utils;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI.Views;

public sealed class VolumeView : ProgressBar
{
    private readonly IUiEventBus uiBus;
    private readonly Label volumeLabel;

    public VolumeView(IUiEventBus uiBus, Pos x, Pos y)
    {
        this.uiBus = uiBus;

        X = x;
        Y = y;
        Height = Globals.VOLUME_SLIDER_HEIGHT; // 4
        Width = Dim.Fill();

        // Keep the view frame title static so it never forces border style/layout redraws
        Title = "Volume";

        BorderStyle = LineStyle.Single;
        ProgressBarStyle = ProgressBarStyle.Blocks;

        Fraction = Globals.Volume;
        CanFocus = true;

        // Add a dedicated sub-label inside the frame to draw the changing percentage text smoothly
        volumeLabel = new Label()
        {
            X = Pos.Center(),
            Y = 0,
            Text = $"{(int)(Globals.Volume * 100)}%"
        };
        Add(volumeLabel);

        RegisterBusHandlers();
        RegisterInteractions();
    }

    protected override void OnActivated(ICommandContext? commandContext) { }
    protected override bool OnMouseEvent(Mouse mouse)
    {
        // Check explicitly for Button1 (Left Mouse Button) interactions or drag movements
        if (mouse.Flags.HasFlag(MouseFlags.LeftButtonClicked) ||
            mouse.Flags.HasFlag(MouseFlags.LeftButtonPressed))
        {
            if (Viewport.Width > 0)
            {
                // Correctly translate the X column coordinate to a progress percentage
                float newFraction = (float)mouse.Position.Value.X / Viewport.Width;
                UpdateVolume(Math.Clamp(newFraction, 0.0f, 1.0f));
            }

            mouse.Handled = true;
            return true;
        }

        return base.OnMouseEvent(mouse);
    }

    private void RegisterBusHandlers()
    {
        uiBus.Subscribe<VolumeChanged>(msg =>
        {
            App?.Invoke(() =>
            {
                Fraction = msg.Volume;
                volumeLabel.Text = $"{(int)(msg.Volume * 100)}%";
            });
        });
    }

    private void RegisterInteractions()
    {
        // Smooth Mouse interaction mapping
        MouseEvent += (sender, e) =>
        {
            if (e.View != this)
                return;

            if (e.Flags.HasFlag(MouseFlags.LeftButtonClicked))
            {
                var width = (float)Viewport.Width;
                var position = (float)e.Position.Value.X;
                var fraction = Math.Clamp(position / width, 0f, 1f);
                UpdateVolume(fraction);
            }
        };

        // Standard Keyboard navigation steps 
        KeyDown += (sender, e) =>
        {
            float step = 0.02f; // 2% granularity changes
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
        volumeLabel.Text = $"{(int)(volume * 100)}%";
        uiBus.Publish(new VolumeChanged(volume));
    }
}