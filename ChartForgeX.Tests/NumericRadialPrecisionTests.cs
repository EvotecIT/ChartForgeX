using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NumericRadialPrecisionTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SmallSignedValueAxesKeepDistinctCaptionsAndOriginalPointFacts(bool bars) {
        var chart = NumericRadialSeriesTests.Add(Chart.Create(), bars, "Observed",
            new ChartPoint(1, -.0001), new ChartPoint(2, -.0004)).WithYAxisBounds(-.0005, 0);
        var scene = NumericRadialSeriesTests.Compile(chart);
        var captions = scene.Regions.Where(region => region.Role == "radial-value-label")
            .Select(region => region.Label!).ToArray();
        Assert.True(captions.Length >= 3);
        Assert.Equal(captions.Length, captions.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("-0.0004", captions);
        Assert.DoesNotContain("-0", captions);
        var point = scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Id == "series-0-point-0");
        Assert.Equal(-.0001, double.Parse(point.Metadata["data-cfx-y"], CultureInfo.InvariantCulture));
        Assert.Contains("-0.0001", point.Metadata["data-cfx-full-label"]);

        var svg = VisualSceneSvgRenderer.Render(scene);
        var pixels = VisualSceneRasterRenderer.Render(scene).Pixels;
        chart.Options.YAxis.WithLabelFormatter(_ => throw new InvalidOperationException("Late formatter"));
        chart.Series[0].Points.Clear();
        Assert.Equal(svg, VisualSceneSvgRenderer.Render(scene));
        Assert.Equal(pixels, VisualSceneRasterRenderer.Render(scene).Pixels);
    }
}
