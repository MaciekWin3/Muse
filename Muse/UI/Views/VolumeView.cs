using Muse.UI.Bus;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI.Views;

public sealed class VolumeView : Slider
{
    private const int VolumeSliderHeight = 4;

    private readonly IUiEventBus uiBus;

    private IEnumerable<SliderOption<object>> VolumeOptions { get; set; } = Enumerable.Range(0, 21)
            .Select(i => i * 5)
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
             var volumeOption = e.Options.FirstOrDefault().Key;
             var volume = CalculateVolume(volumeOption);
             uiBus.Publish(new VolumeChanged(volume));
         };

        SetOption(10);
    }

    private float CalculateVolume(int volumeOption) => (1f / Options.Count) * volumeOption;
}
