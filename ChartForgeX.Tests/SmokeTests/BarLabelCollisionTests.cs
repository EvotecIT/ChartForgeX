using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void DenseBarLabelsAvoidCollisions() {
        var values = Enumerable.Range(1, 14).Select(value => 40d + value).ToArray();
        var vertical = Chart.Create()
            .WithSize(320, 220)
            .WithDataLabels()
            .WithValueFormatter(value => "Remediation " + value.ToString("0", System.Globalization.CultureInfo.InvariantCulture))
            .AddBar("Values", Points(values));
        var verticalSvg = vertical.ToSvg();
        var verticalLabels = CountVisibleDataLabels(verticalSvg);
        Assert(verticalLabels > 0 && verticalLabels <= values.Length, "Dense vertical bar labels should render a readable subset instead of overlapping every label.");
        Assert(vertical.ToPng().Length > 64, "PNG vertical bar label collision avoidance should render valid output.");

        var horizontal = Chart.Create()
            .WithSize(320, 220)
            .WithDataLabels()
            .WithValueFormatter(value => "Coverage " + value.ToString("0", System.Globalization.CultureInfo.InvariantCulture))
            .AddHorizontalBar("Coverage", Points(values));
        var horizontalSvg = horizontal.ToSvg();
        var horizontalLabels = CountVisibleDataLabels(horizontalSvg);
        Assert(horizontalLabels > 0 && horizontalLabels <= values.Length, "Dense horizontal bar labels should render a readable subset instead of overlapping every label.");
        Assert(horizontal.ToPng().Length > 64, "PNG horizontal bar label collision avoidance should render valid output.");

        var intervals = Enumerable.Range(1, 12)
            .Select(value => new ChartInterval(value, 20 + value, 48 + value))
            .ToArray();
        var range = Chart.Create()
            .WithSize(320, 220)
            .WithDataLabels()
            .AddRangeBar("Ranges", intervals);
        var rangeSvg = range.ToSvg();
        var rangeLabels = CountVisibleDataLabels(rangeSvg);
        Assert(rangeLabels > 0 && rangeLabels <= intervals.Length, "Dense range-bar labels should render a readable subset instead of overlapping every interval label.");
        Assert(range.ToPng().Length > 64, "PNG range-bar label collision avoidance should render valid output.");
    }
}
