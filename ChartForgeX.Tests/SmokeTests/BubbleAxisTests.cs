using System;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SecondaryAxisBubbleRangeIgnoresBubbleSizes() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithSecondaryYAxis("Risk score")
            .AddBubble("Risk clusters", new[] {
                new ChartBubble(1, 18, 800),
                new ChartBubble(2, 34, 1200)
            });
        chart.Series[0].UseSecondaryYAxis();

        var range = ChartRange.FromSecondaryYAxis(chart, ChartRange.FromChart(chart));
        Assert(range.MaxY >= 34 && range.MaxY < 100, "Secondary-axis bounds must reflect observed Y, independently of bubble sizes.");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var ticks = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "axis-secondary-y-label").ToArray();
        Assert(ticks.Length > 1, "Secondary-axis bubble charts must draw a readable value scale.");
        Assert(ticks.All(tick => double.Parse(tick.Text.Lines.Single().Text, System.Globalization.CultureInfo.InvariantCulture) < 100), "Secondary-axis tick labels must not include encoded bubble sizes.");
        Assert(chart.ToPng().Length > 64, "Secondary-axis bubble charts should render PNG output.");
    }
}
