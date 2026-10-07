using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2FrameBudgetTests {
    [Theory]
    [InlineData(ChartLegendPosition.Bottom)]
    [InlineData(ChartLegendPosition.Top)]
    [InlineData(ChartLegendPosition.Right)]
    public void RowBudgetIncludesOverflowAndRetainsAllOriginalLegendSemantics(ChartLegendPosition position) {
        var chart = ManySeries();
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(240, 200), 10),
            frame: new VisualFrame(legendPosition: position, legendMaximumRows: 2, legendMaximumHeightFraction: .5)));
        var document = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(2, ByRole(document, "legend-entry").Length);
        Assert.Contains(ByRole(document, "legend-label"), element => element.Value.Contains("5 more entries"));
        Assert.Equal(6, prepared.Regions.Count(region => region.Role == "legend" && region.Id != "legend-overflow"));
        Assert.Equal(5, ByRole(document, "legend-entry-omitted").Length);
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "frame.legend-overflow");
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void HeightBudgetTooSmallForReadableRowOmitsLegendWithoutMovingMarks() {
        var chart = ManySeries(); var layout = new VisualLayoutOptions(new VisualSize(320, 240), 10);
        var hidden = chart.Prepare(new VisualRenderContext(layout, frame: new VisualFrame(showLegend: false)));
        var capped = chart.Prepare(new VisualRenderContext(layout, frame: new VisualFrame(legendMaximumHeightFraction: .001)));
        Assert.Empty(ByRole(XDocument.Parse(capped.ToSvg()), "legend-entry"));
        Assert.Equal(6, capped.Regions.Count(region => region.Role == "legend"));
        Assert.All(capped.Regions.Where(region => region.Role == "legend"), region => Assert.Equal(0, region.Bounds.Width));
        var expected = hidden.Regions.Where(region => region.Role == "point").Select(region => region.Bounds).ToArray();
        Assert.Equal(expected, capped.Regions.Where(region => region.Role == "point").Select(region => region.Bounds).ToArray());
    }

    [Fact]
    public void AsymmetricPaddingControlsEachFrameEdgeAndScalarLayoutsRemainEquivalent() {
        var chart = Chart.Create().AddLine("Values", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        var edges = new ChartPadding(11, 17, 29, 37);
        var layout = new VisualLayoutOptions(new VisualSize(320, 240), edges);
        var prepared = chart.Prepare(new VisualRenderContext(layout, frame: new VisualFrame(showLegend: false, showSurface: true)));
        var surface = ByRole(XDocument.Parse(prepared.ToSvg()), "content-surface").Single();
        Assert.Equal(11, Number(surface, "x")); Assert.Equal(17, Number(surface, "y"));
        Assert.Equal(280, Number(surface, "width")); Assert.Equal(186, Number(surface, "height"));
        Assert.Equal(edges.Left, layout.PaddingEdges.Left); Assert.Equal(edges.Bottom, layout.PaddingEdges.Bottom);
        Assert.Equal(11, layout.Padding);
        var frame = new VisualFrame(showLegend: false);
        var scalar = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 240), 18), frame: frame));
        var uniform = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 240), ChartPadding.All(18)), frame: frame));
        Assert.Equal(scalar.ToSvg(), uniform.ToSvg()); Assert.Equal(scalar.ToPng(), uniform.ToPng());
    }

    [Fact]
    public void FrameCopiesAndGridPanelsKeepLegendBudgets() {
        var frame = new VisualFrame("Title", legendMaximumRows: 1, legendMaximumHeightFraction: .4);
        var headings = frame.WithHeadings("Other", "Subtitle");
        Assert.Equal(1, headings.LegendMaximumRows); Assert.Equal(.4, headings.LegendMaximumHeightFraction);
        var grid = ChartGrid.Create().WithColumns(1).Add(ManySeries());
        var prepared = grid.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(320, 300), new ChartPadding(10, 20, 30, 40)), frame: frame));
        Assert.Single(ByRole(XDocument.Parse(prepared.ToSvg()), "legend-entry"));
        Assert.Contains(prepared.Regions, region => region.Label != null && region.Label.Contains("6 more entries"));
    }

    [Fact]
    public void PresentationContractsRejectInvalidBudgetsAndConsumedViewports() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualFrame(legendMaximumRows: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualFrame(legendMaximumHeightFraction: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualFrame(legendMaximumHeightFraction: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualFrame(legendMaximumHeightFraction: 1.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualLayoutOptions(new VisualSize(100, 80), new ChartPadding(60, 0, 40, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualLayoutOptions(new VisualSize(100, 80), new ChartPadding(0, 50, 0, 30)));
    }

    private static Chart ManySeries() {
        var chart = Chart.Create();
        for (var index = 0; index < 6; index++) chart.AddLine("A long descriptive series label " + index, new[] { new ChartPoint(0, index), new ChartPoint(1, index + 1) });
        return chart;
    }
    private static XElement[] ByRole(XDocument document, string role) => document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
    private static double Number(XElement element, string name) => double.Parse(element.Attribute(name)!.Value, CultureInfo.InvariantCulture);
}
