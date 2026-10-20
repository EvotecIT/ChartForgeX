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
            .AddBar("Values", Points(values));
        for (var index = 0; index < values.Length; index++) vertical.Series[0].WithPointLabel(index,
            "Remediation " + values[index].ToString("0", System.Globalization.CultureInfo.InvariantCulture));
        var verticalSvg = vertical.ToSvg();
        var verticalLabels = CountVisibleDataLabels(verticalSvg);
        Assert(verticalLabels > 0 && verticalLabels <= values.Length, "Dense vertical bar labels should render a readable subset instead of overlapping every label.");
        Assert(vertical.ToPng().Length > 64, "PNG vertical bar label collision avoidance should render valid output.");
        ScatterBubbleColorRenderingTests.Capture(vertical.Prepare(ChartForgeX.Rendering.VisualExportRequest.ForChart(vertical).Context), "smoke-dense-authored-bars-vertical");

        var horizontal = Chart.Create()
            .WithSize(320, 220)
            .WithDataLabels()
            .AddHorizontalBar("Coverage", Points(values));
        for (var index = 0; index < values.Length; index++) horizontal.Series[0].WithPointLabel(index,
            "Coverage " + values[index].ToString("0", System.Globalization.CultureInfo.InvariantCulture));
        var horizontalSvg = horizontal.ToSvg();
        var horizontalLabels = CountVisibleDataLabels(horizontalSvg);
        Assert(horizontalLabels > 0 && horizontalLabels <= values.Length, "Dense horizontal bar labels should render a readable subset instead of overlapping every label.");
        Assert(horizontal.ToPng().Length > 64, "PNG horizontal bar label collision avoidance should render valid output.");
        ScatterBubbleColorRenderingTests.Capture(horizontal.Prepare(ChartForgeX.Rendering.VisualExportRequest.ForChart(horizontal).Context), "smoke-dense-authored-bars-horizontal");

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
