using Muse.UI.Bus;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI.Views;

public sealed class ProgressBarView : ProgressBar
{
    private readonly IUiEventBus uiBus;

    private const int ProgressBarHeight = 3;

    public ProgressBarView(IUiEventBus uiBus, Pos x, Pos y)
    {
        this.uiBus = uiBus;
        X = x;
        Y = y;

        Title = "Progress";
        Height = ProgressBarHeight;
        Width = Dim.Fill();
        Fraction = 0;
        BorderStyle = LineStyle.Rounded;
        ProgressBarStyle = ProgressBarStyle.Continuous;
    }
}
