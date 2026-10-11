using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PolarPointDescriptionTests {
    [Theory]
    [InlineData(ChartSeriesKind.Radar, false)]
    [InlineData(ChartSeriesKind.Polar, false)]
    [InlineData(ChartSeriesKind.Radar, true)]
    public void AuthoredDescriptionsKeepSeriesCategoryCapturedFormattingAndRawIdentity(ChartSeriesKind kind, bool mixed) {
        var calls = new Dictionary<double, int>();
        var chart = Chart.Create().WithSize(500, 400).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithDataLabels(false).WithYAxisBounds(0, 100).WithXLabels("North", "East", "South")
            .WithValueFormatter(value => {
                calls[value] = calls.GetValueOrDefault(value) + 1;
                return value.ToString("0.0", CultureInfo.GetCultureInfo("pl-PL")) + " units";
            });
        if (mixed) chart.AddRadialColumn("Observed", new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30) });
        var points = new[] { new ChartPoint(1, 12.5), new ChartPoint(2, 0), new ChartPoint(3, 30) };
        if (kind == ChartSeriesKind.Polar) chart.AddPolar("Target", points); else chart.AddRadarLine("Target", points);
        var series = mixed ? 1 : 0;
        chart.Series[series].WithPointLabel(2, "Authored caption").WithInteractionKey("target-source");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        var document = XDocument.Parse(svg);
        var expected = new[] { "Target, North: 12,5 units", "Target, East: 0,0 units", "Target, South: Authored caption" };
        var formatted = new[] { "12,5 units", "0,0 units", "Authored caption" };
        for (var point = 0; point < points.Length; point++) {
            var node = document.Descendants().Single(node => (string?)node.Attribute("data-cfx-source-id") == $"series-{series}-point-{point}");
            Assert.Equal(expected[point], (string?)node.Attribute("aria-label"));
            Assert.Equal(expected[point], (string?)node.Attribute("data-cfx-label"));
            Assert.Equal(formatted[point], (string?)node.Attribute("data-cfx-full-label"));
            Assert.Equal(points[point].Y.ToString("R", CultureInfo.InvariantCulture), (string?)node.Attribute("data-cfx-value"));
            Assert.Equal(points[point].X.ToString("R", CultureInfo.InvariantCulture), (string?)node.Attribute("data-cfx-category"));
            Assert.Equal(point.ToString(), (string?)node.Attribute("data-cfx-point"));
            Assert.Equal("false", (string?)node.Attribute("data-cfx-missing"));
        }
        Assert.Equal(1, calls[12.5]);
        var capturedCalls = calls.Values.Sum();
        chart.WithValueFormatter(_ => throw new InvalidOperationException("Prepared descriptions must not reformat."));
        chart.WithXLabels("Changed", "Changed", "Changed");
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal(capturedCalls, calls.Values.Sum());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingRadarGeometryKeepsDerivedIdentityWithoutAnAuthoredDescription(bool mixed) {
        var chart = Chart.Create().WithSize(500, 400).WithXLabels("North", "East", "South").WithYAxisBounds(0, 100);
        var observations = new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30) };
        if (mixed) chart.AddRadialColumn("Observed", observations); else chart.AddRadarArea("Observed", observations);
        chart.AddRadarLine("Target", new[] { new ChartPoint(1, 0), new ChartPoint(3, 80) });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var missing = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(node => node.Id == "series-1-missing-category-1");
        Assert.Equal("missing-category-zero", missing.Metadata["data-cfx-derived"]);
        Assert.Equal("0", missing.Metadata["data-cfx-source-count"]);
        Assert.Equal(string.Empty, missing.Metadata["data-cfx-source-points"]);
        Assert.Equal("-1", missing.Metadata["data-cfx-point"]);
        Assert.Equal("0", missing.Metadata["data-cfx-value"]);
        Assert.False(missing.Metadata.ContainsKey("aria-label"));
        Assert.False(missing.Metadata.ContainsKey("data-cfx-label"));
    }
}
