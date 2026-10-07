using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SmallMultipleGridRendersStaticHtml() {
        var coverage = Chart.Create().WithTitle("Coverage").WithSize(320, 220).AddBar("Values", Points(80, 72, 91));
        var readiness = Chart.Create().WithTitle("Readiness").WithSize(320, 220).AddLine("Values", Points(62, 70, 84));
        var grid = ChartGrid.Create().WithTitle("Control scorecards").WithSubtitle("Small multiples for a static report")
            .WithTheme(ChartTheme.ReportLight()).WithColumns(2).WithGap(20).WithPadding(30).WithPanelSize(300, 200)
            .Add(coverage).Add(readiness).WithSharedYAxis();
        var html = grid.ToHtmlPage();
        Assert(html.Contains("<section class=\"chartforgex-grid\"", StringComparison.Ordinal), "Chart grids should render a stable report container.");
        Assert(CountOccurrences(html, "<svg ") == 1, "Static HTML should embed the complete prepared grid layout.");
        Assert(html.Contains("Control scorecards", StringComparison.Ordinal) && html.Contains("Coverage", StringComparison.Ordinal)
            && html.Contains("Readiness", StringComparison.Ordinal), "The composed comparison should retain report and panel headings.");
        Assert(!html.Contains("<script", StringComparison.OrdinalIgnoreCase), "Chart grids should remain JavaScript-free.");
        var svg = grid.ToSvg();
        var document = XDocument.Parse(svg);
        Assert(document.Root!.Attribute("width")!.Value == "680" && double.Parse(document.Root.Attribute("height")!.Value, System.Globalization.CultureInfo.InvariantCulture) > 260,
            "Natural grid exports should preserve fixed panel widths, measured headings and outer padding.");
        Assert(CountOccurrences(svg, "<svg ") == 1, "Grid panels should be translated scene geometry in one SVG viewport.");
        var prepared = grid.Prepare(VisualExportRequest.ForGrid(grid).Context);
        Assert(prepared.Regions.Count(region => region.Role == "panel") == 2, "The prepared grid should retain inspectable panel regions.");
        Assert(coverage.Options.YAxisMinimum == readiness.Options.YAxisMinimum && coverage.Options.YAxisMaximum == readiness.Options.YAxisMaximum,
            "Shared y-axis grids should apply equal bounds to compatible charts.");
        var png = grid.ToPng();
        Assert(ReadBigEndianInt32(png, 16) == 680 && ReadBigEndianInt32(png, 20) == (int)Math.Ceiling(prepared.Size.Height), "PNG should use the same logical grid viewport.");
        AssertRgbaParity(grid.ToRgbaImage(), png, "Prepared ChartGrid");
        var tintedTheme = ChartTheme.ReportLight(); tintedTheme.Background = ChartColor.FromHex("#F4F7FB");
        var tinted = ChartGrid.Create().WithTitle(grid.Title).WithSubtitle(grid.Subtitle).WithTheme(tintedTheme)
            .WithColumns(2).WithGap(20).WithPadding(30).WithPanelSize(300, 200).Add(coverage).Add(readiness);
        Assert(!png.SequenceEqual(tinted.ToPng()), "The shared grid export should honor explicit background colors.");

        var repeated = Chart.Create().WithTitle("Repeated").WithSize(320, 220).AddLine("Values", Points(10, 20, 30));
        var repeatedGrid = ChartGrid.Create().Add(repeated).Add(repeated);
        AssertNoDuplicateIds(repeatedGrid.ToHtmlPage(), "Repeated prepared grid panels");
        Assert(repeatedGrid.ToHtmlFragment() == repeatedGrid.ToHtmlFragment(), "Default HTML grid fragments should be deterministic.");
        AssertNoDuplicateIds(repeatedGrid.ToHtmlFragment("grid-a") + repeatedGrid.ToHtmlFragment("grid-b"), "Scoped HTML grids");
        AssertNoDuplicateIds(repeatedGrid.ToSvg("grid-a") + repeatedGrid.ToSvg("grid-b"), "Scoped SVG grids");
        AssertNoDuplicateIds(repeatedGrid.ToHtmlFragment("scope with spaces") + repeatedGrid.ToHtmlFragment("scope|with|spaces"), "External host scopes");
        Assert(grid.ToSvg() == grid.ToSvg(), "Prepared SVG grids should be deterministic.");
        Assert(grid.ToSvg("stable-grid") == grid.ToSvg("stable-grid"), "Host-scoped SVG grids should be deterministic.");
        grid.WithAutomaticPanelSize().WithAutomaticTheme();
        Assert(!grid.PanelSize.HasValue && grid.Theme == null, "Automatic grid controls should clear explicit export settings.");
    }
}
