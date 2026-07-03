using Muse.Player;
using Muse.UI.Bus;
using Muse.Utils;
using System.Drawing;
using System.Text;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Muse.UI.Views;

public sealed class EqualizerView : FrameView
{
    private readonly IUiEventBus uiEventBus;
    private readonly IPlayerService playerService;
    private readonly GraphView graphView;
    private readonly DiscoBarSeries discoBarSeries;
    private SpectrumAnalyzer? spectrumAnalyzer;
    private object? timeoutToken; // Store token to cleanly cancel the timer

    public EqualizerView(IUiEventBus uiEventBus, IPlayerService playerService, Pos x, Pos y)
    {
        this.uiEventBus = uiEventBus;
        this.playerService = playerService;
        X = x;
        Y = y;
        Title = "Equalizer";
        BorderStyle = LineStyle.Rounded;

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

        // Wait until the view has been added to a layout tree so ApplicationContext is populated
        Initialized += (s, e) =>
        {
            if (App != null)
            {
                timeoutToken = App.AddTimeout(TimeSpan.FromMilliseconds(50), () =>
                {
                    if (playerService.State == PlaybackState.Playing)
                    {
                        EnsureSpectrumAnalyzer();
                        UpdateBars();
                    }
                    else
                    {
                        StopSpectrumAnalyzer();
                        ClearBars();
                    }
                    return true;
                });
            }
        };
    }

    private void EnsureSpectrumAnalyzer()
    {
        if (spectrumAnalyzer == null)
        {
            spectrumAnalyzer = new SpectrumAnalyzer();
        }
    }

    private void StopSpectrumAnalyzer()
    {
        if (spectrumAnalyzer != null)
        {
            spectrumAnalyzer.Dispose();
            spectrumAnalyzer = null;
        }
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
        if (spectrumAnalyzer == null) return;
        var data = spectrumAnalyzer.SpectrumData;
        if (data == null || data.Length == 0) return;

        if (discoBarSeries.Bars.Count != data.Length)
        {
            discoBarSeries.Bars = data.ToList();
        }
        else
        {
            for (int i = 0; i < data.Length; i++)
            {
                discoBarSeries.Bars[i] = (discoBarSeries.Bars[i] * 0.1f) + (data[i] * 0.9f);
            }
        }

        graphView.SetNeedsDraw();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Clean up the timeout token from the lifecycle loop
            if (timeoutToken != null && App != null)
            {
                App.RemoveTimeout(timeoutToken);
                timeoutToken = null;
            }

            if (spectrumAnalyzer != null)
            {
                spectrumAnalyzer.Dispose();
                spectrumAnalyzer = null;
            }
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
                Terminal.Gui.Drawing.Color segmentColor;
                if (y > viewport.Height * 0.8) segmentColor = Terminal.Gui.Drawing.Color.BrightRed;
                else if (y > viewport.Height * 0.6) segmentColor = Terminal.Gui.Drawing.Color.BrightYellow;
                else if (y > viewport.Height * 0.4) segmentColor = Terminal.Gui.Drawing.Color.BrightGreen;
                else if (y > viewport.Height * 0.2) segmentColor = Terminal.Gui.Drawing.Color.BrightCyan;
                else segmentColor = Terminal.Gui.Drawing.Color.BrightBlue;

                var attribute = new Terminal.Gui.Drawing.Attribute(segmentColor, Terminal.Gui.Drawing.Color.Black);

                // Instead of Application.Driver, we apply the local rendering attribute 
                // directly using GraphView's contextual override mechanisms or a direct SetAttribute block if available.
                graph.SetAttribute(attribute);

                // Draw the block rune
                graph.AddRune(x, viewport.Bottom - 1 - y, (Rune)'█');

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