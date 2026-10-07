using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects authored chart semantics across the detached prepared export boundary.</summary>
public sealed class V2CartesianSemanticTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AllAnnotationKindsRetainRawFactsAndRegionsAfterSourceMutation(bool annotationOnly) {
        var chart = Chart.Create().WithAxes(false).WithGrid(false)
            .AddHorizontalLine(2.5, "Target <5 & \"safe\"")
            .AddVerticalLine(1.5)
            .AddHorizontalBand(2.5, 3.5, "Full interval label")
            .AddVerticalBand(1.5, 2.5, "Vertical interval");
        if (!annotationOnly) chart.AddLine("Samples", new[] { new ChartPoint(1, 2), new ChartPoint(3, 4) });
        var source = chart.Annotations.ToArray();
        var prepared = chart.Prepare(Context());
        var svg = prepared.ToSvg();
        var png = prepared.ToPng();
        var groups = XDocument.Parse(svg).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "annotation").ToArray();
        Assert.Equal(source.Length, groups.Length);
        Assert.Equal(2, groups.Count(group => group.Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "annotation-band")));
        Assert.Equal(2, groups.Count(group => group.Descendants().Any(element => (string?)element.Attribute("data-cfx-role") == "annotation-line")));
        for (var index = 0; index < source.Length; index++) {
            var annotation = source[index];
            var group = groups[index];
            Assert.Equal(annotation.Kind.ToString(), (string?)group.Attribute("data-cfx-kind"));
            Assert.Equal(Number(annotation.Value), (string?)group.Attribute("data-cfx-value"));
            Assert.Equal(annotation.EndValue.HasValue ? Number(annotation.EndValue.Value) : "", (string?)group.Attribute("data-cfx-end-value"));
            Assert.Equal(annotation.Label, (string?)group.Attribute("data-cfx-label"));
            var region = Assert.Single(prepared.Regions, candidate => candidate.Id == "annotation-" + index);
            Assert.Equal("annotation", region.Role);
            Assert.Equal((string?)group.Attribute("aria-label"), region.Label);
            Assert.Contains(annotation.Kind.ToString(), region.Label!);
            Assert.Contains(Number(annotation.Value), region.Label!);
            if (annotation.EndValue.HasValue) Assert.Contains(Number(annotation.EndValue.Value), region.Label!);
            if (annotation.Label.Length > 0) Assert.Contains(annotation.Label, region.Label!);
        }
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.no-data");
        Assert.True(png.Length > 64);
        chart.Annotations.Clear();
        chart.Series.Clear();
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng());
        Assert.Equal(source.Length, prepared.Regions.Count(region => region.Role == "annotation"));
    }

    [Fact]
    public void PointCalloutAndCustomScatterRolesSurvivePreparationWithPointIdentity() {
        const string customRole = "operator-observation<&\"";
        var chart = Chart.Create().WithAxes(false).WithGrid(false)
            .AddScatter("Ordinary", new[] { new ChartPoint(0, 1) })
            .AddPointCallout("Full callout caption", 1, 2)
            .AddScatter("Authored", new[] { new ChartPoint(2, 3) });
        chart.Series[2].WithSemanticRole(customRole);
        var prepared = chart.Prepare(Context());
        var svg = prepared.ToSvg();
        var markers = XDocument.Parse(svg).Descendants().Where(element => element.Name.LocalName == "ellipse").ToArray();
        Assert.Equal(new[] { "marker", "point-callout", customRole }, markers.Select(element => (string?)element.Attribute("data-cfx-role")));
        for (var index = 0; index < markers.Length; index++) {
            var point = markers[index].Ancestors().Single(element => (string?)element.Attribute("data-cfx-role") == "point");
            Assert.Equal(index.ToString(CultureInfo.InvariantCulture), (string?)point.Attribute("data-cfx-series"));
            Assert.Equal("0", (string?)point.Attribute("data-cfx-source-point"));
            Assert.Equal(chart.Series[index].SemanticRole ?? "", (string?)point.Attribute("data-cfx-semantic-role"));
            Assert.Single(prepared.Regions, region => region.Id == "series-" + index + "-point-0");
        }
        var png = prepared.ToPng();
        chart.Series[1].SemanticRole = "changed-callout";
        chart.Series[2].SemanticRole = "changed-custom";
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.Bar)]
    public void NonScatterSeriesRetainAuthoredRolesWithoutReplacingStructuralMarkRoles(ChartSeriesKind kind) {
        var points = new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) };
        var chart = kind == ChartSeriesKind.Line ? Chart.Create().AddLine("Observed", points) : Chart.Create().AddBar("Observed", points);
        chart.WithAxes(false).WithGrid(false);
        chart.Series[0].WithSemanticRole("host-observation");
        var xml = XDocument.Parse(chart.Prepare(Context()).ToSvg());
        var series = Assert.Single(xml.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "series");
        Assert.Equal("host-observation", (string?)series.Attribute("data-cfx-semantic-role"));
        Assert.All(series.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "point"),
            point => Assert.Equal("host-observation", (string?)point.Attribute("data-cfx-semantic-role")));
        Assert.Contains(series.Descendants(), element => (string?)element.Attribute("data-cfx-role") == (kind == ChartSeriesKind.Line ? "line" : "bar"));
    }

    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(400, 260)),
        frame: new VisualFrame("", "", showLegend: false));

    private static string Number(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
