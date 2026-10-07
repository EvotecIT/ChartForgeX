using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2ChartGridTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void GridRetainsSpanPackingSharedAxesAndScopedChildGeometryInFixedViewport(VisualThemeMode mode) {
        var first = Line("First", 1, 4); var second = Line("Second", 2, 8); var third = Line("Third", 3, 6);
        var grid = ChartGrid.Create().WithTitle("Comparison").WithColumns(2).WithGap(12).WithPanelFit(VisualPanelFit.Stretch)
            .Add(first).Add(second).Add(third, columnSpan: 2).WithSharedAxes();
        var prepared = grid.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 480)), themeMode: mode,
            frame: new VisualFrame(showLegend: false, showSurface: true)));
        Assert.Equal(first.Options.YAxis.Minimum, second.Options.YAxis.Minimum);
        Assert.Equal(first.Options.YAxis.Maximum, third.Options.YAxis.Maximum);
        var panels = prepared.Regions.Where(region => region.Role == "panel").ToArray();
        Assert.Equal(3, panels.Length); Assert.True(panels[2].Bounds.Width > panels[0].Bounds.Width * 2);
        Assert.True(panels[2].Bounds.Top > panels[0].Bounds.Bottom);
        Assert.Contains(prepared.Regions, region => region.Id == "panel-0/series-0-point-0");
        Assert.Contains(prepared.Regions, region => region.Id == "panel-1/series-0-point-0");
        var document = XDocument.Parse(prepared.ToSvg("comparison"));
        var ids = document.Descendants().Attributes("id").Select(attribute => attribute.Value).ToArray();
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.All(ids, id => Assert.StartsWith("comparison-", id, StringComparison.Ordinal));
        var image = prepared.ToRgba(new VisualRenderOptions(scale: 2, supersampling: 1));
        Assert.Equal(1280, image.Width); Assert.Equal(960, image.Height);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        first.Series.Clear(); second.Title = "Changed"; grid.WithColumns(1).WithGap(100);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void PaginatedGridRetainsAccessibilityVariablesAndEmptyColumnsIndependently() {
        var variables = new SvgColorVariables().Add("--grid-surface", ChartColor.White, SvgColorRole.Surface);
        var grid = ChartGrid.Create().WithColumns(2).WithSvgColorVariables(variables).Add(Line("One", 1, 2))
            .Add(Line("Two", 3, 4)).Add(Line("Three", 5, 6)).WithSharedAxes();
        grid.Accessibility.WithTextAlternative("Three metrics", "Shared value range", "pl-PL");
        var pages = grid.Paginate(2);
        Assert.Equal(2, pages.Count);
        Assert.All(pages, page => {
            Assert.True(page.Grid.PreserveEmptyColumns);
            Assert.Equal("Three metrics", page.Grid.Accessibility.Name);
            Assert.Equal("pl-PL", page.Grid.Accessibility.Language);
            Assert.Single(page.Grid.SvgColorVariables!.Variables);
            var prepared = page.Grid.Prepare(new VisualRenderContext());
            Assert.Equal("Three metrics", prepared.Accessibility.Name);
            Assert.NotEmpty(prepared.ToPng());
        });
        variables.Add("--later", ChartColor.Black);
        pages[0].Grid.Accessibility.Name = "Edited page";
        Assert.Single(pages[1].Grid.SvgColorVariables!.Variables);
        Assert.Equal("Three metrics", pages[1].Grid.Accessibility.Name);
    }

    [Fact]
    public void InsufficientPanelGapSpaceProducesDiagnosticWithoutInvalidChildGeometry() {
        var grid = ChartGrid.Create().WithColumns(2).WithGap(1000).Add(Line("One", 1, 2)).Add(Line("Two", 3, 4));
        var prepared = grid.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(120, 100), padding: 8)));
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "grid.insufficient-space");
        Assert.DoesNotContain("NaN", prepared.ToSvg()); Assert.Equal(120, prepared.ToRgba().Width);
        Assert.Throws<InvalidOperationException>(() => ChartGrid.Create().Prepare(new VisualRenderContext()));
    }

    private static Chart Line(string title, double first, double second) => Chart.Create().WithTitle(title).AddLine("Values",
        new[] { new ChartPoint(0, first), new ChartPoint(1, second) });
}
