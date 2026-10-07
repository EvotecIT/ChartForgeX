using System.Globalization;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void DenseComparisonLabelsAvoidCollisions() {
        string Formatter(double value) => "Window " + value.ToString("0", CultureInfo.InvariantCulture);
        var bands = Enumerable.Range(1, 10).Select(value => new ChartRangeBand(value, 30 + value, 52 + value)).ToArray();

        var rangeBand = Chart.Create()
            .WithSize(320, 220)
            .WithDataLabels()
            .WithValueFormatter(Formatter)
            .AddRangeBand("Forecast", bands);
        AssertUsefulSubset(rangeBand.ToSvg(), bands.Length, "Dense range-band labels should avoid collisions.");
        Assert(rangeBand.ToPng().Length > 64, "PNG range-band label collision avoidance should render valid output.");

        var rangeArea = Chart.Create()
            .WithSize(320, 220)
            .WithDataLabels()
            .WithValueFormatter(Formatter)
            .AddRangeArea("Prediction", bands);
        AssertUsefulSubset(rangeArea.ToSvg(), bands.Length, "Dense range-area labels should avoid collisions.");
        Assert(rangeArea.ToPng().Length > 64, "PNG range-area label collision avoidance should render valid output.");

        var dumbbells = Enumerable.Range(1, 10).Select(value => new ChartDumbbell(value, 30 + value, 52 + value)).ToArray();
        var dumbbell = Chart.Create()
            .WithSize(320, 220)
            .WithDataLabels()
            .WithValueFormatter(Formatter)
            .AddDumbbell("Before/after", dumbbells);
        AssertUsefulSubset(dumbbell.ToSvg(), dumbbells.Length, "Dense dumbbell labels should avoid collisions.");
        Assert(dumbbell.ToPng().Length > 64, "PNG dumbbell label collision avoidance should render valid output.");

        var bubbles = Enumerable.Range(1, 10).Select(value => new ChartBubble(value, 40 + value * 0.2, 20 + value)).ToArray();
        var bubble = Chart.Create()
            .WithSize(320, 220)
            .WithDataLabels()
            .WithValueFormatter(Formatter)
            .AddBubble("Reach", bubbles);
        AssertUsefulSubset(bubble.ToSvg(), bubbles.Length, "Dense bubble labels should avoid collisions.");
        Assert(bubble.ToPng().Length > 64, "PNG bubble label collision avoidance should render valid output.");

        var errorBars = Enumerable.Range(1, 10).Select(value => new ChartErrorBar(value, 42 + value * 0.2, 34 + value * 0.2, 50 + value * 0.2)).ToArray();
        var error = Chart.Create()
            .WithSize(320, 220)
            .WithDataLabels()
            .WithValueFormatter(Formatter)
            .AddErrorBar("Confidence", errorBars);
        AssertUsefulSubset(error.ToSvg(), errorBars.Length, "Dense error-bar labels should avoid collisions.");
        Assert(error.ToPng().Length > 64, "PNG error-bar label collision avoidance should render valid output.");

        var boxes = Enumerable.Range(1, 10)
            .Select(value => new ChartBoxPlot(value, 18 + value, 24 + value, 31 + value, 38 + value, 48 + value))
            .ToArray();
        var box = Chart.Create()
            .WithSize(320, 220)
            .WithDataLabels()
            .WithValueFormatter(Formatter)
            .AddBoxPlot("Latency", boxes);
        AssertUsefulSubset(box.ToSvg(), boxes.Length, "Dense box-plot labels should avoid collisions.");
        Assert(box.ToPng().Length > 64, "PNG box-plot label collision avoidance should render valid output.");
    }

    private static void AssertUsefulSubset(string svg, int itemCount, string message) {
        var labelCount = CountVisibleDataLabels(svg);
        Assert(labelCount > 0 && labelCount <= itemCount, message);
        var overlap = Rendering.ChartLabelScene.Inspect(svg, Typography.FontSpec.SystemSans());
        Assert(overlap.LabelLabel == 0, message + " Measured label boxes must remain disjoint.");
    }
}
