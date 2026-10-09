using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Prepared radial facts identify their series before any host adds interactions.</summary>
public sealed class PreparedPolarIdentityTests {
    [Theory]
    [InlineData(ChartSeriesKind.Radar, "radar-point-source")]
    [InlineData(ChartSeriesKind.Polar, "polar-point-source")]
    public void EverySourcePointNamesItsOwningSeriesAndKeepsItsAuthoredPointIndex(ChartSeriesKind kind, string role) {
        var first = new[] { new ChartPoint(3, 72), new ChartPoint(1, 54), new ChartPoint(2, 63) };
        var second = new[] { new ChartPoint(2, 81), new ChartPoint(3, 92), new ChartPoint(1, 76) };
        var chart = Bare();
        if (kind == ChartSeriesKind.Radar) chart.AddRadarArea("Observed", first).AddRadarLine("Target", second);
        else chart.AddPolar("Observed", first).AddPolar("Target", second);
        chart.Series[0].WithInteractionKey("observed-source");
        chart.Series[1].WithInteractionKey("target-source");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var groups = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == role).ToArray();
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(6, groups.Length);
        for (var series = 0; series < 2; series++) {
            var points = series == 0 ? first : second;
            for (var point = 0; point < points.Length; point++) {
                var sourceId = $"series-{series}-point-{point}";
                var group = Assert.Single(groups, item => item.Id == sourceId);
                Assert.Equal(series.ToString(), group.Metadata["data-cfx-series"]);
                Assert.Equal(point.ToString(), group.Metadata["data-cfx-point"]);
                Assert.Equal(points[point].Y.ToString(System.Globalization.CultureInfo.InvariantCulture), group.Metadata["data-cfx-value"]);
                AssertSeriesAttribute(svg, sourceId, series);
            }
        }
    }

    [Fact]
    public void PolarAreaFactsDeclareTheirSingleSeriesIncludingAuthoredZero() {
        var chart = Bare().AddPolarArea("Distribution", new[] { new ChartPoint(1, 40), new ChartPoint(2, 10), new ChartPoint(3, 0) });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var groups = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "polar-area-point-source").ToArray();
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(3, groups.Length);
        Assert.All(groups, group => {
            Assert.Equal("0", group.Metadata["data-cfx-series"]);
            AssertSeriesAttribute(svg, group.Id!, 0);
        });
        Assert.Equal("0", Assert.Single(groups, group => group.Id == "series-0-point-2").Metadata["data-cfx-value"]);
    }

    private static void AssertSeriesAttribute(XDocument svg, string sourceId, int series) {
        var element = Assert.Single(svg.Descendants(), item => (string?)item.Attribute("data-cfx-source-id") == sourceId);
        Assert.Equal(series.ToString(), (string?)element.Attribute("data-cfx-series"));
    }

    private static Chart Bare() => Chart.Create().WithSize(400, 360).WithHeader(false).WithLegend(false).WithYAxisBounds(0, 100);
}
