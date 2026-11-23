using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class IntegerStackedAxisTickTests {
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    public void StackedCountsUseWholeTicksAcrossTheAccumulatedValueRange(int maximum, bool horizontal) {
        var first = maximum / 2;
        var completed = new[] { new ChartPoint(1, first), new ChartPoint(2, first) };
        var pending = new[] { new ChartPoint(1, maximum - first), new ChartPoint(2, maximum - first) };
        var chart = Chart.Create().Size(640, 300).ShowHeader(false).Legend(false).DataLabels(false).Bars(b => b.Stacked());
        if (horizontal) chart.AddHorizontalBar("Completed", completed).AddHorizontalBar("Pending", pending).XAxis(x => x.IntegerTicks());
        else chart.AddBar("Completed", completed).AddBar("Pending", pending).YAxis(y => y.IntegerTicks());
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var role = horizontal ? "axis-x-label" : "axis-y-label";
        var ticks = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == role)
            .Select(node => node.Text.Lines.Single().Text).ToArray();
        Assert.Equal(Enumerable.Range(0, maximum + 1).Select(value => value.ToString(CultureInfo.InvariantCulture)), ticks);
    }
}
