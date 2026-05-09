using System;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SvgSurfaceAndGuideStrokesStayPremiumAtAnyScale() {
        var svg = Chart.Create()
            .WithSize(420, 260)
            .WithTitle("Premium scale")
            .WithSubtitle("Crisp at small and large sizes")
            .AddSmoothLine("Values", Points(10, 30, 20), ChartColor.FromRgb(37, 99, 235))
            .ToSvg();

        Assert(svg.Contains(".cfx-guide-stroke", StringComparison.Ordinal), "SVG stylesheet should define a guide-stroke class for crisp axes and grid lines.");
        Assert(svg.Contains(".cfx-premium-stroke", StringComparison.Ordinal), "SVG stylesheet should define a premium-stroke class for non-scaling series lighting.");
        Assert(svg.Contains("vector-effect:non-scaling-stroke", StringComparison.Ordinal), "SVG strokes should stay readable when charts are embedded very small or very large.");
        Assert(svg.Contains("class=\"cfx-guide-stroke\"", StringComparison.Ordinal), "SVG axis and grid lines should opt into the shared guide-stroke class.");
        Assert(svg.Contains("class=\"cfx-premium-stroke\"", StringComparison.Ordinal), "SVG line layers should opt into the shared premium-stroke class.");
        Assert(svg.Contains("-cardSurface\"", StringComparison.Ordinal) && svg.Contains("-plotSurface\"", StringComparison.Ordinal), "SVG surfaces should define reusable card and plot gradients.");
        Assert(svg.Contains("fill=\"url(#", StringComparison.Ordinal), "SVG card and plot surfaces should render through deterministic gradients.");
        Assert(svg.Contains("data-cfx-role=\"card-inner-highlight\"", StringComparison.Ordinal), "SVG card surfaces should render a subtle inner highlight.");
        Assert(svg.Contains("data-cfx-role=\"plot-inner-highlight\"", StringComparison.Ordinal), "SVG plot surfaces should render a subtle inner highlight.");
        Assert(svg.Contains("x1=\"76.5\"", StringComparison.Ordinal) || svg.Contains("y1=\"", StringComparison.Ordinal) && svg.Contains(".5\"", StringComparison.Ordinal), "SVG guide strokes should snap thin horizontal or vertical guides to half-pixel centers.");

        var html = Chart.Create()
            .WithSize(420, 260)
            .WithTitle("Premium HTML")
            .AddSmoothLine("Values", Points(10, 30, 20), ChartColor.FromRgb(37, 99, 235))
            .ToHtmlPage();
        Assert(html.Contains("linear-gradient(180deg", StringComparison.Ordinal), "Standalone HTML pages should use a polished responsive surface background.");
        Assert(html.Contains("min-height:100svh", StringComparison.Ordinal), "Standalone HTML pages should use modern viewport sizing for browser previews.");
        Assert(html.Contains("@media print", StringComparison.Ordinal), "Standalone HTML pages should include print-friendly chart framing.");
    }

    private static void StaticHtmlShellsSharePremiumPreviewPolish() {
        var chart = Chart.Create()
            .WithSize(360, 220)
            .WithTitle("Shell chart")
            .AddSmoothLine("Values", Points(12, 22, 18), ChartColor.FromRgb(37, 99, 235));
        AssertPremiumHtmlShell(chart.ToHtmlPage(), centered: true, "chart HTML page");

        var grid = ChartGrid.Create()
            .WithTitle("Shell grid")
            .WithTheme(ChartTheme.ReportLight())
            .WithColumns(2)
            .Add(chart)
            .Add(Chart.Create().WithSize(360, 220).WithTitle("Bars").AddBar("Values", Points(4, 7, 5)));
        AssertPremiumHtmlShell(grid.ToHtmlPage(), centered: false, "chart grid HTML page");

        var table = ChartTable.Create()
            .WithTitle("Shell visual block")
            .WithTheme(ChartTheme.ReportLight())
            .AddColumn("Name")
            .AddColumn("Value")
            .AddRow("Coverage", "98%");
        AssertPremiumHtmlShell(table.ToHtmlPage(), centered: true, "visual block HTML page");

        var visualGrid = VisualGrid.Create()
            .WithTitle("Shell visual grid")
            .WithTheme(ChartTheme.ReportDark())
            .WithColumns(2)
            .Add(chart)
            .Add(table);
        AssertPremiumHtmlShell(visualGrid.ToHtmlPage(), centered: false, "visual grid HTML page");
    }

    private static void AssertPremiumHtmlShell(string html, bool centered, string label) {
        Assert(html.Contains("body{margin:0;min-height:100vh;min-height:100svh", StringComparison.Ordinal), label + " should use the shared viewport-safe body shell.");
        Assert(html.Contains("linear-gradient(180deg", StringComparison.Ordinal), label + " should use the shared polished surface gradient.");
        Assert(html.Contains("-webkit-font-smoothing:antialiased", StringComparison.Ordinal) && html.Contains("text-rendering:geometricPrecision", StringComparison.Ordinal), label + " should request browser text polish.");
        Assert(html.Contains("@media print{body{min-height:auto", StringComparison.Ordinal) && html.Contains("background:transparent", StringComparison.Ordinal), label + " should include shared print framing.");
        if (centered) Assert(html.Contains("body{margin:0;min-height:100vh;min-height:100svh;display:grid;place-items:center", StringComparison.Ordinal) && html.Contains("padding:clamp(16px,4vmin,52px)", StringComparison.Ordinal), label + " should center preview content with responsive padding.");
        else Assert(!html.Contains("body{margin:0;min-height:100vh;min-height:100svh;display:grid;place-items:center", StringComparison.Ordinal), label + " should keep report body layout top-aligned instead of preview-centered.");
    }
}
