using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CoordinateInferenceMetadataTests {
    [Fact]
    public void CartesianObservationsDeclareCoordinatesOnTheirExistingSeriesScope() {
        foreach (var family in new[] { "line", "scatter", "area", "range" }) {
            var chart = Chart.Create().WithLegend(true);
            var points = new[] { new ChartPoint(3, 5), new ChartPoint(5, 7) };
            switch (family) {
                case "line": chart.AddLine("Reading", points); break;
                case "scatter": chart.AddScatter("Reading", points); break;
                case "area": chart.AddArea("Reading", points); break;
                default: chart.AddRangeBand("Reading", new[] { new ChartRangeBand(3, 4, 6), new ChartRangeBand(5, 5, 7) }); break;
            }
            var doc = Export(chart);
            var scope = Assert.Single(doc.Descendants(), e => e.Attribute("data-cfx-coordinate-system") != null);
            Assert.Equal("series", (string?)scope.Attribute("data-cfx-role"));
            Assert.Equal("cartesian", (string?)scope.Attribute("data-cfx-coordinate-system"));
            var observations = scope.Descendants().Where(e => e.Attribute("data-cfx-point") != null).ToArray();
            Assert.Equal(new[] { "0", "1" }, observations.Select(e => (string?)e.Attribute("data-cfx-point")));
            Assert.All(observations, e => Assert.Same(scope, e.Ancestors().First(a => a.Attribute("data-cfx-coordinate-system") != null)));
            var legends = doc.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "legend-entry").ToArray();
            Assert.NotEmpty(legends);
            Assert.All(legends,
                e => Assert.DoesNotContain(e.AncestorsAndSelf(), a => a.Attribute("data-cfx-coordinate-system") != null));
        }
    }

    [Fact]
    public void NativeOnlyProducersKeepTheirOwnCoordinateFactsAndPolarScope() {
        var charts = new[] {
            Chart.Create().WithXLabels("First").AddHeatmapRow("Requests", new[] { 37d }),
            Chart.Create().AddProgressBars("Progress", new[] { new ChartProgressItem("Ready", 37) }),
            Chart.Create().AddTimelineRange("Work", 2, 8),
            Chart.Create().AddTreemap("Teams", new[] { new ChartHierarchyItem("support", "Support", value: 37) }),
            Chart.Create().AddWordCloud("Topics", new[] { new ChartWordCloudItem("Visible", 37) }),
            Chart.Create().WithMapLabels(false).AddRegionMap("Coverage", new ChartMapDefinition("region", "Region", 100, 100,
                new[] { new ChartMapRegion("A", "First", "M20 20H40V40H20Z") }), new[] { new ChartRegionMapItem("A", 37) })
        };
        foreach (var chart in charts) {
            var doc = Export(chart);
            Assert.DoesNotContain(doc.Descendants(), e => e.Attribute("data-cfx-coordinate-system") != null);
            Assert.Contains(doc.Descendants(), e => e.Attribute("data-cfx-value") != null || e.Attribute("data-cfx-y") != null || e.Attribute("data-cfx-start") != null);
        }
        var polar = Export(Chart.Create().WithXLabels("First", "Second").AddPie("Share", ChartPoints.FromValues(7, 3)));
        var scope = Assert.Single(polar.Descendants(), e => e.Attribute("data-cfx-coordinate-system") != null);
        Assert.Equal("polar", (string?)scope.Attribute("data-cfx-coordinate-system"));
        Assert.Equal(new[] { "7", "3" }, scope.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "radial-point").Select(e => (string?)e.Attribute("data-cfx-value")));
    }

    private static XDocument Export(Chart chart) => XDocument.Parse(chart.Prepare(new VisualRenderContext()).ToSvg("coordinate-proof"));
}
