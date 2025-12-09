using Muse.UI.Bus;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI.Views;

public sealed class VolumeView : Slider
{
    private const int VolumeSliderHeight = 4;

    // Settings
    private const int VolumeStep = 5;
    private const int MaxVolume = 100;
    private const int StepsCount = (MaxVolume / VolumeStep) + 1;
    private const int DefaultVolumePercent = 50;
    private static readonly int DefaultVolumeChoice = DefaultVolumePercent / VolumeStep;

    private int previousChoice = DefaultVolumeChoice;

    private readonly IUiEventBus uiBus;
    private IEnumerable<SliderOption<object>> VolumeOptions =>
        Enumerable.Range(0, StepsCount)
            .Select(step => step * VolumeStep)
            .Select(v => new SliderOption<object>
            {
                Data = v,
                Legend = v.ToString()
            });

    public VolumeView(IUiEventBus uiBus, Pos x, Pos y)
    {
        this.uiBus = uiBus;

        Options = [.. VolumeOptions];
        X = x;
        Y = y;
        Title = "Volume";
        Height = VolumeSliderHeight;
        Width = Dim.Fill();
        Type = SliderType.Single;
        UseMinimumSize = false;
        BorderStyle = LineStyle.Rounded;
        ShowEndSpacing = false;

        OptionsChanged += (sender, e) =>
        {
            int stepIndex = e.Options.FirstOrDefault().Key;

            if (stepIndex != 0)
            {
                previousChoice = stepIndex;
            }

            float volume = CalculateVolume(stepIndex);
            uiBus.Publish(new VolumeChanged(volume));
        };

        SetOption(DefaultVolumeChoice);
        RegisterBusHandlers();
    }

    private void RegisterBusHandlers()
    {
        uiBus.Subscribe<MuteToggle>(msg =>
        {
            if (msg.IsMuted)
            {
                SetOption(0);
            }
            else
            {
                SetOption(previousChoice);
            }
        });
    }

    private float CalculateVolume(int volumeOption)
        => (volumeOption * VolumeStep) / 100f;
}
