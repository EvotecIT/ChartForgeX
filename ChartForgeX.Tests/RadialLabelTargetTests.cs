using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RadialLabelTargetTests {
    [Theory]
    [InlineData(ChartSeriesKind.Pie, false)]
    [InlineData(ChartSeriesKind.Donut, true)]
    [InlineData(ChartSeriesKind.RadialBar, true)]
    [InlineData(ChartSeriesKind.RadialColumn, false)]
    [InlineData(ChartSeriesKind.Polar, false)]
    [InlineData(ChartSeriesKind.Radar, true)]
    [InlineData(ChartSeriesKind.PolarArea, true)]
    public void StaticCaptionsDeclareTheirNativeSourceWithoutDuplicatingObservationFacts(ChartSeriesKind kind, bool outside) {
        var svg = XDocument.Parse(RadialLabelTargetBrowserTests.Create(kind, outside).ToSvg());
        var aliases = svg.Descendants().Where(element => element.Attribute("data-cfx-label-for") != null && element.Descendants().Any(child => child.Name.LocalName == "text")).ToArray();
        Assert.NotEmpty(aliases);
        Assert.Equal(3, svg.Descendants().Count(element => element.Attribute("data-cfx-point") != null));
        foreach (var label in aliases) {
            var source = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-source-id") == (string?)label.Attribute("data-cfx-label-for"));
            var observation = source.AncestorsAndSelf().First(element => element.Attribute("data-cfx-point") != null);
            Assert.EndsWith((string)label.Attribute("data-cfx-full-label")!, (string)observation.Attribute("data-cfx-full-label")!);
            Assert.Equal("true", (string?)label.Attribute("data-cfx-label-decoration"));
            Assert.Null(label.Attribute("data-cfx-point"));
            Assert.Null(label.Attribute("data-cfx-series"));
        }
    }

    [Theory]
    [InlineData(ChartSeriesKind.Pie)]
    [InlineData(ChartSeriesKind.RadialBar)]
    public void ExternalSvgNamespacesPreserveLabelSourceIdentityWithinEachDocument(ChartSeriesKind kind) {
        var chart = RadialLabelTargetBrowserTests.Create(kind, true);
        var left = XDocument.Parse(chart.ToSvg("caption-left"));
        var right = XDocument.Parse(chart.ToSvg("caption-right"));
        var leftIds = left.Descendants().Attributes("id").Select(attribute => attribute.Value).ToArray();
        var rightIds = right.Descendants().Attributes("id").Select(attribute => attribute.Value).ToArray();
        Assert.NotEmpty(leftIds); Assert.NotEmpty(rightIds);
        Assert.All(leftIds, id => Assert.StartsWith("caption-left-", id, StringComparison.Ordinal));
        Assert.All(rightIds, id => Assert.StartsWith("caption-right-", id, StringComparison.Ordinal));
        Assert.Empty(leftIds.Intersect(rightIds, StringComparer.Ordinal));
        foreach (var svg in new[] { left, right }) {
            var labels = svg.Descendants().Where(element => element.Attribute("data-cfx-label-for") != null).ToArray();
            Assert.NotEmpty(labels);
            foreach (var label in labels) {
                var source = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-source-id") == (string?)label.Attribute("data-cfx-label-for"));
                Assert.NotNull(source.AncestorsAndSelf().First(element => element.Attribute("data-cfx-point") != null));
            }
        }
        Assert.Equal(left.Descendants().Attributes("data-cfx-label-for").Select(attribute => attribute.Value),
            right.Descendants().Attributes("data-cfx-label-for").Select(attribute => attribute.Value));
    }

    [Theory]
    [InlineData(ChartDataLabelPlacement.Above)]
    [InlineData(ChartDataLabelPlacement.Below)]
    public void VerticalPieCaptionsDeclareTheSameNativeSourceAsTheirLeader(ChartDataLabelPlacement placement) {
        var chart = RadialLabelTargetBrowserTests.Create(ChartSeriesKind.Pie, true).WithDataLabelPlacement(placement).WithSize(800, 460);
        var svg = XDocument.Parse(chart.ToSvg());
        var labels = svg.Descendants().Where(element => element.Attribute("data-cfx-label-for") != null && element.Descendants().Any(child => child.Name.LocalName == "text")).ToArray();
        Assert.NotEmpty(labels);
        foreach (var label in labels) Assert.Contains(svg.Descendants(), element =>
            (string?)element.Attribute("data-cfx-source-id") == (string?)label.Attribute("data-cfx-label-for") + "-connector");
    }

    [Fact]
    public void OtherCaptionPointsToTheAuthoredAggregateRatherThanInventingARawObservation() {
        var chart = RadialLabelTargetBrowserTests.Create(ChartSeriesKind.Pie, true);
        chart.Options.MaximumPieSlices = 2;
        var svg = XDocument.Parse(chart.ToSvg());
        var label = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-label-for") == "series-0-point-other");
        var mark = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-source-id") == "series-0-point-other");
        var observation = mark.Ancestors().First(element => element.Attribute("data-cfx-point") != null);
        Assert.Equal("-1", (string?)observation.Attribute("data-cfx-point"));
        Assert.Equal("1,2", (string?)observation.Attribute("data-cfx-source-points"));
        Assert.Equal((string?)observation.Attribute("data-cfx-full-label"), (string?)label.Attribute("data-cfx-full-label"));
        Assert.Equal(2, svg.Descendants().Count(element => element.Attribute("data-cfx-point") != null));
    }

    [Fact]
    public void MissingRadarCaptionAndStackTotalDoNotAliasARealSourceObservation() {
        var radar = RadialLabelTargetBrowserTests.Create(ChartSeriesKind.Radar, true);
        radar.Series[0].Points.RemoveAt(1);
        radar.AddRadar("Complete", new[] { new ChartPoint(1, 400), new ChartPoint(2, 500), new ChartPoint(3, 600) });
        var svg = XDocument.Parse(radar.ToSvg());
        var missing = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-source-id") == "series-0-missing-category-1-label-source");
        Assert.Null(missing.Attribute("data-cfx-label-for"));

        var numeric = RadialLabelTargetBrowserTests.Create(ChartSeriesKind.RadialColumn, true);
        numeric.AddRadialColumn("Additional", new[] { new ChartPoint(1, 100), new ChartPoint(2, 200), new ChartPoint(3, 300) });
        foreach (var series in numeric.Series) series.WithStackGroup("total");
        numeric.Options.ShowStackTotals = true;
        var totals = XDocument.Parse(numeric.ToSvg()).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "stack-total-source").ToArray();
        Assert.NotEmpty(totals);
        Assert.All(totals, total => Assert.Null(total.Attribute("data-cfx-label-for")));
    }
}
