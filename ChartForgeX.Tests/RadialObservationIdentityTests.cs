using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RadialObservationIdentityTests {
    [Theory]
    [InlineData(ChartSeriesKind.Pie)]
    [InlineData(ChartSeriesKind.Donut)]
    public void DirectSlicesAndLegendsCaptureAuthoredIdsWhileOtherKeepsItsContributorIdentity(ChartSeriesKind kind) {
        var series = new ChartSeries(new ChartSeriesId("parts"), "Parts", kind, new[] {
            new ChartPoint(1, 8), new ChartPoint(2, 4), new ChartPoint(3, 2), new ChartPoint(4, 1)
        });
        foreach (var index in Enumerable.Range(0, 4))
            series.WithObservationId(index, new ChartObservationId("part-" + index));
        var chart = Chart.Create().WithSize(640, 400).WithXLabels("North", "South", "East", "West").AddSeries(series);
        chart.Options.MaximumPieSlices = 3;
        var prepared = chart.Prepare(new VisualRenderContext());
        var svg = prepared.ToSvg();
        var targets = XDocument.Parse(svg).Descendants().Where(node =>
            (string?)node.Attribute("data-cfx-role") is "radial-point" or "legend-entry").ToArray();
        foreach (var index in new[] { 0, 1 }) {
            var direct = targets.Where(node => (string?)node.Attribute("data-cfx-point") == index.ToString()).ToArray();
            Assert.Equal(2, direct.Length);
            Assert.All(direct, node => {
                Assert.Equal("parts", (string?)node.Attribute("data-cfx-series-key"));
                Assert.Equal("part-" + index, (string?)node.Attribute("data-cfx-observation-id"));
                Assert.Equal(index.ToString(), (string?)node.Attribute("data-cfx-source-points"));
            });
        }
        var aggregate = targets.Where(node => (string?)node.Attribute("data-cfx-point") == "-1").ToArray();
        Assert.Equal(2, aggregate.Length);
        Assert.All(aggregate, node => {
            Assert.Equal("radial-slice", (string?)node.Attribute("data-cfx-derived"));
            Assert.Equal("2,3", (string?)node.Attribute("data-cfx-source-points"));
            Assert.Null(node.Attribute("data-cfx-observation-id"));
        });
        series.WithObservationId(0, new ChartObservationId("changed"));
        chart.Options.MaximumPieSlices = 2;
        Assert.Equal(svg, prepared.ToSvg());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Pie)]
    [InlineData(ChartSeriesKind.Donut)]
    public void UnpaintedDirectSliceLegendStillCapturesItsAuthoredId(ChartSeriesKind kind) {
        var series = new ChartSeries(new ChartSeriesId("parts"), "Parts", kind,
            new[] { new ChartPoint(1, 0), new ChartPoint(2, 4) })
            .WithObservationId(0, new ChartObservationId("north"));
        var chart = Chart.Create().WithSize(640, 400).WithXLabels("North", "South").AddSeries(series);
        var nodes = XDocument.Parse(chart.Prepare(new VisualRenderContext()).ToSvg()).Descendants().ToArray();
        var legend = Assert.Single(nodes, node => (string?)node.Attribute("data-cfx-role") == "legend-entry"
            && (string?)node.Attribute("data-cfx-point") == "0");
        Assert.Equal("north", (string?)legend.Attribute("data-cfx-observation-id"));
        Assert.DoesNotContain(nodes, node => (string?)node.Attribute("data-cfx-role") == "radial-point"
            && (string?)node.Attribute("data-cfx-point") == "0");
    }
}
