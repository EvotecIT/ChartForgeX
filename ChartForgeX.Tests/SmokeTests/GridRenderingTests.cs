using System;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SmallMultipleGridRendersStaticHtml() {
        var coverage = Chart.Create().WithTitle("Coverage").WithSize(320, 220).AddBar("Values", Points(80, 72, 91));
        var readiness = Chart.Create().WithTitle("Readiness").WithSize(320, 220).AddLine("Values", Points(62, 70, 84));
        var grid = ChartGrid.Create()
            .WithTitle("Control scorecards")
            .WithSubtitle("Small multiples for a static report")
            .WithTheme(ChartTheme.ReportLight())
            .WithColumns(2)
            .WithGap(20)
            .WithPadding(30)
            .WithPanelSize(300, 200)
            .Add(coverage)
            .Add(readiness)
            .WithSharedYAxis();

        var html = grid.ToHtmlPage();
        Assert(html.Contains("<section class=\"chartforgex-grid\"", StringComparison.Ordinal), "Chart grids should render a stable report container.");
        Assert(html.Contains("--cfx-grid-columns:2", StringComparison.Ordinal), "Chart grids should expose the requested column count.");
        Assert(html.Contains("--cfx-grid-gap:20px", StringComparison.Ordinal), "Chart grids should expose the requested gap.");
        Assert(html.Contains("--cfx-grid-padding:30px", StringComparison.Ordinal), "Chart grids should expose the requested padding.");
        Assert(html.Contains("--cfx-grid-panel-width:300px", StringComparison.Ordinal), "Chart grids should expose fixed panel widths.");
        Assert(html.Contains("--cfx-grid-panel-height:200px", StringComparison.Ordinal), "Chart grids should expose fixed panel heights.");
        Assert(html.Contains("linear-gradient(180deg", StringComparison.Ordinal) && html.Contains(".chartforgex-grid-panel svg{width:auto;height:auto;max-width:100%;max-height:100%;display:block;overflow:visible}", StringComparison.Ordinal), "Chart grid HTML pages should use polished surfaces without clipping chart shadows.");
        Assert(CountOccurrences(html, "<svg ") == 2, "Chart grids should render each chart as inline SVG.");
        Assert(html.Contains(">Control scorecards</h1>", StringComparison.Ordinal), "Chart grids should render report titles.");
        Assert(!html.Contains("<script", StringComparison.OrdinalIgnoreCase), "Chart grids should remain JavaScript-free.");
        var repeated = Chart.Create().WithTitle("Repeated").WithSize(320, 220).AddLine("Values", Points(10, 20, 30));
        var repeatedHtml = ChartGrid.Create().Add(repeated).Add(repeated).ToHtmlPage();
        var repeatedTitleIds = ExtractAttributeValues(repeatedHtml, "<title id=\"");
        Assert(repeatedTitleIds.Length == 2 && repeatedTitleIds.Distinct(StringComparer.Ordinal).Count() == 2, "Inline HTML grids should give repeated charts unique SVG title IDs.");
        AssertNoDuplicateIds(repeatedHtml, "Inline HTML grids");
        var repeatedGrid = ChartGrid.Create().Add(repeated);
        var combinedGridFragments = repeatedGrid.ToHtmlFragment() + repeatedGrid.ToHtmlFragment();
        var combinedGridTitleIds = ExtractAttributeValues(combinedGridFragments, "<title id=\"");
        Assert(combinedGridTitleIds.Length == 2 && combinedGridTitleIds.Distinct(StringComparer.Ordinal).Count() == 2, "Concatenated HTML grid fragments should scope child SVG title IDs per grid render.");
        AssertNoDuplicateIds(combinedGridFragments, "Concatenated HTML grid fragments");
        Assert(coverage.Options.YAxisMinimum == readiness.Options.YAxisMinimum && coverage.Options.YAxisMaximum == readiness.Options.YAxisMaximum, "Shared y-axis grids should apply equal y-axis bounds to compatible charts.");
        Assert(coverage.ToPng().Length > 64 && readiness.ToPng().Length > 64, "Shared y-axis bounds should apply to PNG rendering too.");
        var early = Chart.Create().WithSize(300, 200).AddLine("Early", new[] { new ChartPoint(2, 10), new ChartPoint(3, 20) });
        var late = Chart.Create().WithSize(300, 200).AddLine("Late", new[] { new ChartPoint(6, 18), new ChartPoint(8, 28) });
        ChartGrid.Create().Add(early).Add(late).WithSharedAxes();
        Assert(early.Options.XAxisMinimum == late.Options.XAxisMinimum && early.Options.XAxisMaximum == late.Options.XAxisMaximum, "Shared x-axis grids should apply equal x-axis bounds to compatible charts.");
        Assert(early.Options.YAxisMinimum == late.Options.YAxisMinimum && early.Options.YAxisMaximum == late.Options.YAxisMaximum, "Shared-axis grids should apply equal y-axis bounds to compatible charts.");
        Assert(early.ToPng().Length > 64 && late.ToSvg().Contains("<svg", StringComparison.Ordinal), "Shared x-axis bounds should apply to SVG and PNG rendering.");

        var svg = grid.ToSvg();
        Assert(svg.StartsWith("<svg", StringComparison.Ordinal), "Chart grids should export standalone SVG.");
        Assert(!svg.Contains("data:image/svg+xml;base64,", StringComparison.Ordinal), "Chart grid SVG should keep child charts inline instead of base64-encoding them.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"grid-panel\"") == 2, "Chart grid SVG should expose inline child chart panels for downstream inspection.");
        Assert(svg.Contains("-gridSurface", StringComparison.Ordinal) && svg.Contains("vector-effect:non-scaling-stroke", StringComparison.Ordinal), "Chart grid SVG should use premium scalable surface and stroke primitives.");
        Assert(CountOccurrences(svg, "<svg ") == 3, "Chart grid SVG should contain the root SVG plus one inline SVG per child chart.");
        var combinedGridSvgs = grid.ToSvg() + grid.ToSvg();
        var gridSvgTitleIds = ExtractAttributeValues(combinedGridSvgs, "<title id=\"");
        Assert(gridSvgTitleIds.Length == 6 && gridSvgTitleIds.Distinct(StringComparer.Ordinal).Count() == 6, "Concatenated SVG grid exports should scope root and child accessibility IDs per render.");
        AssertNoDuplicateIds(combinedGridSvgs, "Concatenated SVG grid exports");
        AssertNoDuplicateIds(grid.ToSvg("grid-a") + grid.ToSvg("grid-b"), "Scoped raw SVG grid exports");
        Assert(grid.ToSvg("stable-grid") == grid.ToSvg("stable-grid"), "Explicit SVG grid ID scopes should keep raw SVG grid output deterministic.");
        var boundaryGridA = ChartGrid.Create().WithTitle("b|c").Add(repeated);
        var boundaryGridB = ChartGrid.Create().WithTitle("c").Add(repeated);
        AssertNoDuplicateIds(boundaryGridA.ToSvg("a") + boundaryGridB.ToSvg("a|b"), "Boundary-distinct scoped SVG grid exports");
        Assert(svg.Contains("width=\"291\" height=\"200\"", StringComparison.Ordinal), "Fixed panel grid exports should contain charts without distorting their aspect ratio.");
        var png = grid.ToPng();
        Assert(ReadBigEndianInt32(png, 16) == 680, "Chart grid PNG should use fixed panel width and custom padding.");
        Assert(ReadBigEndianInt32(png, 20) == 336, "Chart grid PNG should use fixed panel height and custom padding.");
        var tintedTheme = ChartTheme.ReportLight();
        tintedTheme.Background = ChartColor.FromHex("#F4F7FB");
        Assert(!png.SequenceEqual(ChartGrid.Create().WithTitle("Control scorecards").WithSubtitle("Small multiples for a static report").WithTheme(tintedTheme).WithColumns(2).WithGap(20).WithPadding(30).WithPanelSize(300, 200).Add(coverage).Add(readiness).ToPng()), "Chart grid PNG should honor polished export surface colors.");

        var stretched = ChartGrid.Create().WithPanelSize(300, 200).WithPanelFit(ChartGridPanelFit.Stretch).Add(coverage);
        Assert(stretched.ToSvg().Contains("width=\"300\" height=\"200\"", StringComparison.Ordinal), "Stretch panel grids should use the full fixed panel size.");

        var compact = ChartGrid.Create()
            .WithTitle("Extremely long small multiple grid title that should not overflow exported report bounds")
            .WithPadding(16)
            .WithPanelSize(220, 140)
            .Add(Chart.Create().WithTitle("Tiny").WithSize(220, 140).AddLine("Values", Points(10, 20, 30)));
        Assert(compact.ToSvg().Contains("...</text>", StringComparison.Ordinal), "Composed SVG grid headers should shorten long titles.");
        Assert(compact.ToPng().Length > 64, "Composed PNG grid headers should render even when long titles require fitting.");

        grid.WithAutomaticPanelSize().WithAutomaticTheme();
        Assert(!grid.PanelSize.HasValue && grid.Theme == null, "Automatic grid panel and theme settings should clear explicit export controls.");
        stretched.Theme = ChartTheme.ReportDark();
        stretched.Theme = null;
        Assert(stretched.Theme == null, "Nullable grid theme assignments should clear explicit grid themes.");
    }
}
