using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects stable category meaning through reorder, reconstruction and changing series membership.</summary>
public sealed class StableSeriesPaletteTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void ExplicitIdsKeepMarkAndLegendPaintWhenOrderAndMembershipChange(VisualThemeMode mode) {
        var samples = Series("samples", 1); var reference = Series("reference", 2);
        var chart = Bare(mode).AddSeries(samples).AddSeries(reference);
        var before = Paints(chart); var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.NotEqual(before["samples"], before["reference"]);
        chart.MoveSeries(samples, 1); samples.WithName("Localized samples");
        Assert.Equal(before["samples"], Paints(chart)["samples"]);
        chart.AddSeries(Series("another", 3)).RemoveSeries(reference);
        Assert.Equal(before["samples"], Paints(chart)["samples"]);
        var rebuilt = Paints(Bare(mode).AddSeries(Series("reference", 2)).AddSeries(Series("samples", 1)));
        Assert.Equal(before["samples"], rebuilt["samples"]); Assert.Equal(before["reference"], rebuilt["reference"]);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void ExplicitPaintAndSemanticStateRetainPrecedenceAndFinitePalettesMayRepeat() {
        var color = ChartColor.FromHex("#123456");
        var theme = VisualTheme.Graphite().WithPalette(color);
        var chart = Bare(VisualThemeMode.Light).WithTheme(theme).AddSeries(Series("first", 1)).AddSeries(Series("second", 2));
        var repeated = Paints(chart);
        Assert.Equal(repeated["first"], repeated["second"]);
        chart.Series[0].StateRole = ChartSeriesState.Warning;
        Assert.NotEqual(repeated["first"], Paints(chart)["first"]);
        chart.Series[0].Color = ChartColor.FromHex("#987654");
        var authored = Paints(chart)["first"];
        chart.Series[0].StateRole = ChartSeriesState.Danger;
        Assert.Equal(authored, Paints(chart)["first"]);
        chart.Series[0].WithPointColor(0, color);
        Assert.Contains(color.ToCss(), Prepare(chart).ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void AnonymousHelpersKeepTheirOrdinalPaletteContract() {
        var theme = VisualTheme.Graphite();
        var chart = Bare(VisualThemeMode.Light).AddScatter("First", new[] { new ChartPoint(1, 1) })
            .AddScatter("Second", new[] { new ChartPoint(2, 2) });
        var colors = theme.Resolve(VisualThemeMode.Light);
        Assert.Equal(colors.Palette[0], ChartSeriesColours.Resolve(chart, 0, colors));
        Assert.Equal(colors.Palette[1], ChartSeriesColours.Resolve(chart, 1, colors));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Pie)]
    [InlineData(ChartSeriesKind.Donut)]
    [InlineData(ChartSeriesKind.PolarArea)]
    public void PartToWholeCategoriesKeepDistinctPaintWithinAnExplicitSeries(ChartSeriesKind kind) {
        var series = new ChartSeries(new ChartSeriesId("split"), "Allocation", kind,
            new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30) });
        var chart = Bare(VisualThemeMode.Light).AddSeries(series);
        var prepared = Prepare(chart);
        var fills = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Where(node =>
            node.Role is "pie-slice" or "donut-slice" or "polar-area-segment").Select(node => node.Fill).ToArray();
        Assert.Equal(3, fills.Length); Assert.Equal(3, fills.Distinct().Count());
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void DifferenceAreaKeepsDistinctPolarityPaintAfterReordering() {
        var difference = ChartSeries.CreateDifferenceArea(new ChartSeriesId("difference"), "Difference", new[] {
            new ChartDifferencePoint(1, 10, 20), new ChartDifferencePoint(2, 30, 20)
        });
        var chart = Bare(VisualThemeMode.Light).AddSeries(difference).AddSeries(Series("peer", 3));
        var before = PolarityPaints(Prepare(chart));
        Assert.Equal(2, before.Distinct().Count());
        chart.MoveSeries(difference, 1);
        Assert.Equal(before, PolarityPaints(Prepare(chart)));
    }

    private static ChartColor?[] PolarityPaints(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualScenePath>()
        .Where(node => node.Role is "difference-positive" or "difference-negative").Select(node => node.Fill).ToArray();

    private static Dictionary<string, string> Paints(Chart chart) {
        var document = XDocument.Parse(Prepare(chart).ToSvg());
        var result = new Dictionary<string, string>();
        foreach (var series in chart.Series) {
            var key = series.Id.Value;
            var group = document.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "series"
                && (string?)node.Attribute("data-cfx-series-key") == key);
            var mark = group.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "marker");
            var legend = document.Descendants().Single(node => (string?)node.Attribute("data-cfx-series-key") == key
                && ((string?)node.Attribute("data-cfx-role")) == "legend-entry");
            var swatch = legend.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "legend-swatch");
            Assert.Equal((string?)mark.Attribute("fill"), (string?)swatch.Attribute("fill"));
            result.Add(key, mark.Attribute("fill")!.Value);
        }
        return result;
    }

    private static ChartSeries Series(string id, double value) => ChartSeries.CreateScatter(new ChartSeriesId(id), id, new[] { new ChartPoint(value, value) });
    private static Chart Bare(VisualThemeMode mode) => Chart.Create().WithSize(600, 400).WithTheme(VisualTheme.Graphite(), mode)
        .WithHeader(false).WithLegend().WithDataLabels(false).WithAxes(false).WithGrid(false);
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
