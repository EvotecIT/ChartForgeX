using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects explicit absent-versus-zero meaning through standalone and mixed native radar geometry.</summary>
public sealed class RadarMissingCategoryTests {
    [Theory]
    [InlineData(false, ChartRadarMissingCategoryPolicy.Zero)]
    [InlineData(false, ChartRadarMissingCategoryPolicy.Gap)]
    [InlineData(false, ChartRadarMissingCategoryPolicy.Connect)]
    [InlineData(true, ChartRadarMissingCategoryPolicy.Zero)]
    [InlineData(true, ChartRadarMissingCategoryPolicy.Gap)]
    [InlineData(true, ChartRadarMissingCategoryPolicy.Connect)]
    public void PolicyControlsPathsFillsAndTargetsWithoutInventingAuthoredObservations(bool mixed, ChartRadarMissingCategoryPolicy policy) {
        var chart = Create(mixed, policy);
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
        var group = document.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "radar-series"
            && (string?)node.Attribute("data-cfx-series-key") == "sample");
        Assert.Equal(policy.ToString().ToLowerInvariant(), (string?)group.Attribute("data-cfx-missing-policy"));
        var outlines = group.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "radar-outline").ToArray();
        var areas = group.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "radar-area").ToArray();
        Assert.Equal(policy == ChartRadarMissingCategoryPolicy.Gap ? 2 : 1, outlines.Length);
        Assert.Equal(policy == ChartRadarMissingCategoryPolicy.Gap ? 0 : 1, areas.Length);
        Assert.All(outlines, outline => Assert.Equal(policy != ChartRadarMissingCategoryPolicy.Gap, outline.Attribute("d")!.Value.EndsWith("Z", StringComparison.Ordinal)));
        var facts = group.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "radar-point-source").ToArray();
        Assert.Equal(policy == ChartRadarMissingCategoryPolicy.Zero ? 6 : 4, facts.Length);
        var authoredZero = Assert.Single(facts, node => (string?)node.Attribute("data-cfx-point") == "0");
        Assert.Equal("0", (string?)authoredZero.Attribute("data-cfx-value"));
        Assert.Equal("false", (string?)authoredZero.Attribute("data-cfx-missing"));
        Assert.Equal("observed-zero", (string?)authoredZero.Attribute("data-cfx-observation-id"));
        if (policy != ChartRadarMissingCategoryPolicy.Zero) {
            Assert.DoesNotContain(facts, node => (string?)node.Attribute("data-cfx-missing") == "true");
            Assert.DoesNotContain(prepared.Regions, region => region.Id.StartsWith("series-1-missing-category-", StringComparison.Ordinal));
        }
        var legend = document.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "legend-entry"
            && (string?)node.Attribute("data-cfx-series-key") == "sample");
        Assert.Equal(policy != ChartRadarMissingCategoryPolicy.Gap,
            legend.Descendants().Any(node => (string?)node.Attribute("data-cfx-role") == "legend-area"));
        Assert.Equal(policy == ChartRadarMissingCategoryPolicy.Gap,
            prepared.Diagnostics.Any(diagnostic => diagnostic.Code == "radar.open-area"));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Series[1].ConfigureRadar(options => options.MissingCategoryPolicy = ChartRadarMissingCategoryPolicy.Zero);
        chart.Series[1].Points.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GapKeepsTheObservedRunAcrossTheCategorySeamAndRespectsReversal(bool mixed) {
        var chart = Create(mixed, ChartRadarMissingCategoryPolicy.Gap).ConfigureXAxis(axis => axis.Reversed = true);
        var prepared = Prepare(chart);
        var document = XDocument.Parse(prepared.ToSvg());
        var group = document.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "radar-series"
            && (string?)node.Attribute("data-cfx-series-key") == "sample");
        var paths = group.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "radar-outline").ToArray();
        Assert.Equal(2, paths.Length);
        Assert.Single(paths, node => node.Attribute("d")!.Value.Contains("C", StringComparison.Ordinal));
        Assert.Equal(4, prepared.Regions.Count(region => region.Role == "radar-point" && region.Id.StartsWith("series-1-point-", StringComparison.Ordinal)));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void MissingPolicyConfigurationIsDetachedValidatedAndKeepsZeroAsTheDefault() {
        var chart = Create(false, ChartRadarMissingCategoryPolicy.Zero);
        ChartRadarOptions? captured = null;
        chart.Series[1].ConfigureRadar(options => { captured = options; options.MissingCategoryPolicy = ChartRadarMissingCategoryPolicy.Gap; });
        captured!.MissingCategoryPolicy = ChartRadarMissingCategoryPolicy.Connect;
        Assert.Equal(ChartRadarMissingCategoryPolicy.Gap, chart.Series[1].Radar.MissingCategoryPolicy);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Series[1].ConfigureRadar(options => options.MissingCategoryPolicy = (ChartRadarMissingCategoryPolicy)99));
        Assert.Equal(ChartRadarMissingCategoryPolicy.Gap, chart.Series[1].Radar.MissingCategoryPolicy);
        Assert.Equal(ChartRadarMissingCategoryPolicy.Zero, new ChartRadarOptions().MissingCategoryPolicy);
    }

    private static Chart Create(bool mixed, ChartRadarMissingCategoryPolicy policy) {
        var all = Enumerable.Range(1, 6).Select(index => new ChartPoint(index, 80)).ToArray();
        var chart = Chart.Create().WithSize(600, 420).WithHeader(false).WithLegend().WithAxes(false).WithGrid(false)
            .WithDataLabels(false).WithYAxisBounds(0, 100);
        if (mixed) chart.AddRadialColumn("Categories", all);
        else chart.AddRadarLine("Categories", all);
        chart.Series[0].WithLegendEntry(false);
        if (!mixed) chart.Series[0].ConfigureMarkers(options => options.Enabled = false);
        var sample = new ChartSeries(new ChartSeriesId("sample"), "Observed", ChartSeriesKind.Radar, new[] {
            new ChartPoint(1, 0), new ChartPoint(2, 70), new ChartPoint(4, 60), new ChartPoint(6, 50)
        }).WithObservationId(0, new ChartObservationId("observed-zero"))
            .ConfigureRadar(options => { options.MissingCategoryPolicy = policy; options.CurveTension = .2; options.FillOpacity = .2; });
        return chart.AddSeries(sample);
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
