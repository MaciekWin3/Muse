using Muse.UI.Bus;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI.Views;

public sealed class VolumeView : Slider
{
    private const int VolumeSliderHeight = 4;

    private readonly IUiEventBus uiBus;

    public VolumeView(IUiEventBus uiBus, Pos y)
    {
        this.uiBus = uiBus;

        Options = Enumerable.Range(0, 21)
            .Select(i => i * 5)
            .Select(v => new SliderOption<object>
            {
                Data = v,
                Legend = v.ToString()
            })
            .ToList();

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
             var value = e.Options.FirstOrDefault().Key;
             uiBus.Publish(new VolumeChanged(value));
         };

        SetOption(10);
    }
}
