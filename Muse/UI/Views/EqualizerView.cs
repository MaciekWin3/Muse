using Muse.Player;
using Muse.UI.Bus;
using Muse.Utils;
using Terminal.Gui.Views;
using Terminal.Gui.Drawing;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using System.Text;
using System.Drawing;

namespace Muse.UI.Views;

public sealed class EqualizerView : FrameView
{
    private readonly IUiEventBus uiEventBus;
    private readonly IPlayerService playerService;
    private readonly GraphView graphView;
    private readonly DiscoBarSeries discoBarSeries;
    private readonly SpectrumAnalyzer spectrumAnalyzer;

    public EqualizerView(IUiEventBus uiEventBus, IPlayerService playerService, Pos x, Pos y)
    {
        this.uiEventBus = uiEventBus;
        this.playerService = playerService;
        X = x;
        Y = y;
        Title = "Equalizer";
        BorderStyle = LineStyle.Rounded;

        spectrumAnalyzer = new SpectrumAnalyzer();

        graphView = new GraphView
        {
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        graphView.AxisX.Visible = false;
        graphView.AxisY.Visible = false;
        graphView.MarginLeft = 0;
        graphView.MarginBottom = 0;

        discoBarSeries = new DiscoBarSeries();
        graphView.Series.Add(discoBarSeries);

        Add(graphView);

        Application.AddTimeout(TimeSpan.FromMilliseconds(50), () =>
        {
            if (playerService.State == PlaybackState.Playing)
            {
                UpdateBars();
            }
            else
            {
                ClearBars();
            }
            return true;
        });
    }

    private void ClearBars()
    {
        if (discoBarSeries.Bars.Count > 0)
        {
            discoBarSeries.Bars.Clear();
            graphView.SetNeedsDraw();
        }
    }

    private void UpdateBars()
    {
        var data = spectrumAnalyzer.SpectrumData;
        if (data == null || data.Length == 0) return;

        // Maintain some continuity in the bars if they already exist
        if (discoBarSeries.Bars.Count != data.Length)
        {
            discoBarSeries.Bars = data.ToList();
        }
        else
        {
            for (int i = 0; i < data.Length; i++)
            {
                // Less smoothing (0.1/0.9) to show fast changes
                discoBarSeries.Bars[i] = (discoBarSeries.Bars[i] * 0.1f) + (data[i] * 0.9f);
            }
        }
        
        graphView.SetNeedsDraw();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            spectrumAnalyzer.Dispose();
        }
        base.Dispose(disposing);
    }
}

public class DiscoBarSeries : ISeries
{
    public List<float> Bars { get; set; } = new();

    public void DrawSeries(GraphView graph, Rectangle viewport, RectangleF graphSpace)
    {
        if (Bars == null || Bars.Count == 0) return;

        var barWidth = viewport.Width / Bars.Count;
        if (barWidth <= 0) barWidth = 1;

        for (int i = 0; i < Bars.Count; i++)
        {
            var x = viewport.Left + (i * barWidth);
            var height = (int)(Bars[i] * viewport.Height / 100);
            if (height > viewport.Height) height = viewport.Height;

            for (int y = 0; y < height; y++)
            {
                // Use a block rune for bars
                graph.AddRune(x, viewport.Bottom - 1 - y, (Rune)'█');
                
                // Add some width if possible to fill space
                for (int w = 1; w < barWidth - 1; w++)
                {
                    if (x + w < viewport.Right)
                    {
                        graph.AddRune(x + w, viewport.Bottom - 1 - y, (Rune)'█');
                    }
                }
            }
        }
    }
}
