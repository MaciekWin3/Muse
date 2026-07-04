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
    private readonly GraphView graphView;
    private readonly DiscoBarSeries discoBarSeries;
    private SpectrumAnalyzer? spectrumAnalyzer;
    private object? timeoutToken;

    public EqualizerView(IUiEventBus uiEventBus, IPlayerService playerService, Pos x, Pos y)
    {
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
            if (App is not null)
            {
                timeoutToken = App.AddTimeout(TimeSpan.FromMilliseconds(50), () =>
                {
                    if (playerService.State is PlaybackState.Playing)
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
        spectrumAnalyzer ??= new SpectrumAnalyzer();
    }

    private void StopSpectrumAnalyzer()
    {
        spectrumAnalyzer?.Dispose();
        spectrumAnalyzer = null;
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
        if (spectrumAnalyzer is null)
        {
            return;
        }

        // Pattern matching check for null or empty array
        if (spectrumAnalyzer.SpectrumData is not { Length: > 0 } data)
        {
            return;
        }

        if (discoBarSeries.Bars.Count != data.Length)
        {
            discoBarSeries.Bars = [.. data];
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
            if (timeoutToken is not null && App is not null)
            {
                App.RemoveTimeout(timeoutToken);
                timeoutToken = null;
            }

            spectrumAnalyzer?.Dispose();
            spectrumAnalyzer = null;
        }

        base.Dispose(disposing);
    }
}

public class DiscoBarSeries : ISeries
{
    public List<float> Bars { get; set; } = [];

    public void DrawSeries(GraphView graph, Rectangle viewport, RectangleF graphSpace)
    {
        // Pattern matching guard clause
        if (Bars is not { Count: > 0 })
        {
            return;
        }

        var barWidth = viewport.Width / Bars.Count;
        if (barWidth <= 0)
        {
            barWidth = 1;
        }

        for (int i = 0; i < Bars.Count; i++)
        {
            var x = viewport.Left + (i * barWidth);
            var height = (int)(Bars[i] * viewport.Height / 100);

            if (height > viewport.Height)
            {
                height = viewport.Height;
            }

            for (int y = 0; y < height; y++)
            {
                // Refactored to a relational pattern matching switch expression
                Terminal.Gui.Drawing.Color segmentColor = y switch
                {
                    _ when y > viewport.Height * 0.8 => Terminal.Gui.Drawing.Color.BrightRed,
                    _ when y > viewport.Height * 0.6 => Terminal.Gui.Drawing.Color.BrightYellow,
                    _ when y > viewport.Height * 0.4 => Terminal.Gui.Drawing.Color.BrightGreen,
                    _ when y > viewport.Height * 0.2 => Terminal.Gui.Drawing.Color.BrightCyan,
                    _ => Terminal.Gui.Drawing.Color.BrightBlue
                };

                var attribute = new Terminal.Gui.Drawing.Attribute(segmentColor, Terminal.Gui.Drawing.Color.Black);
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