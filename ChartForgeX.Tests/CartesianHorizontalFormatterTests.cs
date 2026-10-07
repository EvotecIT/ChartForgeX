using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianHorizontalFormatterTests {
    [Fact]
    public void ProjectedAxesCaptureTheirDistinctFormattersOnceBeforeDetachedExports() {
        var chart = Chart.Create().WithSize(700, 400).WithLegend(false)
            .AddHorizontalBar("Work", new[] { new ChartPoint(1, 20), new ChartPoint(2, 40) });
        var values = new Dictionary<double, int>(); var categories = new Dictionary<double, int>();
        chart.Options.XAxis.WithLabelFormatter(value => Capture(values, value, "Value "));
        chart.Options.YAxis.WithLabelFormatter(value => Capture(categories, value, "Department "));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var valueRegions = prepared.Regions.Where(region => region.Role == "axis-x-label").ToArray();
        var categoryRegions = prepared.Regions.Where(region => region.Role == "axis-y-label").ToArray();
        Assert.NotEmpty(valueRegions);
        Assert.All(valueRegions, region => Assert.StartsWith("Value ", region.Label!));
        Assert.Equal(new[] { "Department 1 (1)", "Department 2 (2)" }, categoryRegions.Select(region => region.Label));
        Assert.Equal(valueRegions.Length, values.Count); Assert.Equal(categoryRegions.Length, categories.Count);
        Assert.All(values.Values.Concat(categories.Values), count => Assert.Equal(1, count));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Contains(">Value ", svg); Assert.Contains(">Department ", svg);

        chart.Options.XAxis.WithLabelFormatter(_ => throw new InvalidOperationException("Changed value callback"));
        chart.Options.YAxis.WithLabelFormatter(_ => throw new InvalidOperationException("Changed category callback"));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.All(values.Values.Concat(categories.Values), count => Assert.Equal(1, count));
    }

    private static string Capture(Dictionary<double, int> calls, double value, string prefix) {
        calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
        return prefix + value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
