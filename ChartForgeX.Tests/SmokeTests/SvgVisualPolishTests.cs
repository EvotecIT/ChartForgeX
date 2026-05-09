using System;
using System.Globalization;
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

    private static void SpecializedChartLayoutsKeepContentInsidePlotFrame() {
        var bullet = Chart.Create()
            .WithSize(920, 560)
            .WithTheme(ChartTheme.ReportDark())
            .AddBullet("DMARC enforcement", 88, 95, 0, 100, new[] { 60d, 80d }, ChartColor.FromRgb(52, 211, 153))
            .AddBullet("DNSSEC coverage", 74, 90, 0, 100, new[] { 55d, 78d }, ChartColor.FromRgb(96, 165, 250))
            .ToSvg();
        var bulletLabelX = double.Parse(GetStringAttribute(bullet, "data-cfx-role=\"bullet-row-label\"", "x"), CultureInfo.InvariantCulture);
        Assert(bulletLabelX >= 90, "Bullet row labels should honor internal content padding instead of touching the plot frame.");

        var tree = Chart.Create()
            .WithSize(1040, 600)
            .WithTheme(ChartTheme.ReportLight())
            .AddTree("Control hierarchy", new[] {
                new ChartTreeLink("Security posture", "Mail authentication", 3),
                new ChartTreeLink("Security posture", "Certificate lifecycle", 2),
                new ChartTreeLink("Security posture", "DNS hygiene", 2),
                new ChartTreeLink("Mail authentication", "SPF alignment"),
                new ChartTreeLink("Mail authentication", "DKIM rotation"),
                new ChartTreeLink("Certificate lifecycle", "Expiry monitoring"),
                new ChartTreeLink("Certificate lifecycle", "SAN inventory"),
                new ChartTreeLink("DNS hygiene", "DNSSEC rollout"),
                new ChartTreeLink("DNS hygiene", "Stale record cleanup")
            })
            .ToSvg();
        var leftNodeX = double.Parse(GetStringAttribute(tree, "data-cfx-label=\"Security posture\"", "x"), CultureInfo.InvariantCulture);
        var rightNodeX = double.Parse(GetStringAttribute(tree, "data-cfx-label=\"Stale record cleanup\"", "x"), CultureInfo.InvariantCulture);
        var rightNodeWidth = double.Parse(GetStringAttribute(tree, "data-cfx-label=\"Stale record cleanup\"", "width"), CultureInfo.InvariantCulture);
        Assert(leftNodeX >= 90, "Tree root nodes should keep left breathing room inside the plot frame.");
        Assert(rightNodeX + rightNodeWidth <= 990, "Tree leaf nodes should keep right breathing room inside the plot frame.");
    }

    private static void MetricStatusBarsRespectRoundedCards() {
        var metric = MetricCard.Create()
            .WithMetric("Monthly Recurring Revenue", "$120,400")
            .WithStatus(VisualStatus.Positive)
            .WithTheme(ChartTheme.ReportLight().WithCornerRadius(26, 12))
            .WithSize(420, 190);
        var svg = metric.ToSvg("rounded-status-bar");

        Assert(svg.Contains("data-cfx-role=\"metric-status-bar\"", StringComparison.Ordinal), "Metric status cards should still render the semantic accent bar.");
        Assert(svg.Contains("-visualCardClip", StringComparison.Ordinal), "Metric visual blocks should define a rounded card clipping path.");
        Assert(svg.Contains("clip-path=\"url(#", StringComparison.Ordinal), "Metric status bars should be clipped by the card radius instead of painting square corners.");
        Assert(!svg.Contains("-visualBackground)", StringComparison.Ordinal), "Carded visual blocks should not paint a square SVG background behind rounded card corners.");
        var metricPng = ReadPngRgba(metric.ToPng(), out var metricPngWidth, out var metricPngHeight);
        Assert(AlphaAt(metricPng, metricPngWidth, 1, metricPngHeight - 2) == 0, "Carded visual block PNG output should leave the outside of rounded card corners transparent.");

        var noCard = MetricCard.Create()
            .WithMetric("Monthly Recurring Revenue", "$120,400")
            .WithStatus(VisualStatus.Positive)
            .WithCard(false)
            .WithTheme(ChartTheme.ReportLight().WithCornerRadius(26, 12))
            .WithSize(420, 190);
        var noCardSvg = noCard.ToSvg("no-card-status-bar");
        var barStart = noCardSvg.IndexOf("data-cfx-role=\"metric-status-bar\"", StringComparison.Ordinal);
        Assert(barStart >= 0, "Metric status bars without a card should still render the semantic accent bar.");
        var barEnd = noCardSvg.IndexOf("/>", barStart, StringComparison.Ordinal);
        var noCardBar = noCardSvg.Substring(barStart, barEnd - barStart);
        Assert(!noCardBar.Contains("clip-path", StringComparison.Ordinal), "Metric status bars without a card should stay square instead of inheriting rounded card clipping.");
        Assert(noCard.ToPng().Length > 64, "No-card metric status bars should render PNG output without requiring a rounded card clip.");
    }

    private static void TransparentSurfacesKeepTheirAlphaContract() {
        var overlayChart = Chart.Create()
            .WithSize(360, 220)
            .WithTheme(ChartTheme.TransparentOverlayDark())
            .WithPlotBackground()
            .AddLine("Signal", Points(12, 24, 18), ChartColor.FromRgb(96, 165, 250));
        Assert(!overlayChart.ToSvg().Contains("data-cfx-role=\"plot-inner-highlight\"", StringComparison.Ordinal), "Transparent plot backgrounds should not receive a visible SVG inner highlight.");
        Assert(overlayChart.ToPng().Length > 64, "Transparent plot backgrounds should still render PNG output.");

        var translucentTheme = ChartTheme.ReportLight();
        translucentTheme.Background = ChartColor.FromRgba(15, 23, 42, 96);
        var chartGrid = ChartGrid.Create()
            .WithTheme(translucentTheme)
            .WithColumns(1)
            .WithPadding(32)
            .Add(Chart.Create().WithSize(160, 90).WithTransparentBackground().AddLine("Values", Points(1, 2, 3)));
        var chartGridPixels = ReadPngRgba(chartGrid.ToPng(), out var chartGridWidth, out _);
        Assert(AlphaAt(chartGridPixels, chartGridWidth, 16, 16) == 96, "PNG chart grids should not compound translucent background alpha when adding polish.");

        var visualGrid = VisualGrid.Create()
            .WithTheme(translucentTheme)
            .WithColumns(1)
            .WithPadding(32)
            .Add(ChartList.Create().WithTheme(ChartTheme.ReportLight()).WithSize(160, 90).AddItem("Ready"));
        var visualGridPixels = ReadPngRgba(visualGrid.ToPng(), out var visualGridWidth, out _);
        Assert(AlphaAt(visualGridPixels, visualGridWidth, 16, 16) == 96, "PNG visual grids should not compound translucent background alpha when adding polish.");
    }

    private static byte AlphaAt(byte[] rgba, int width, int x, int y) => rgba[(y * width + x) * 4 + 3];

    private static void AssertPremiumHtmlShell(string html, bool centered, string label) {
        Assert(html.Contains("body{margin:0;min-height:100vh;min-height:100svh", StringComparison.Ordinal), label + " should use the shared viewport-safe body shell.");
        Assert(html.Contains("linear-gradient(180deg", StringComparison.Ordinal), label + " should use the shared polished surface gradient.");
        Assert(html.Contains("-webkit-font-smoothing:antialiased", StringComparison.Ordinal) && html.Contains("text-rendering:geometricPrecision", StringComparison.Ordinal), label + " should request browser text polish.");
        Assert(html.Contains("@media print{body{min-height:auto", StringComparison.Ordinal) && html.Contains("background:transparent", StringComparison.Ordinal), label + " should include shared print framing.");
        if (label == "chart HTML page" || label == "visual block HTML page") {
            Assert(html.Contains("style=\"box-sizing:border-box;overflow:visible\"", StringComparison.Ordinal), label + " should not keep inline width or max-width rules that block standalone screen and print sizing.");
            Assert(html.Contains(" svg{width:100%;height:auto}", StringComparison.Ordinal), label + " should force the embedded SVG to page width in print mode.");
        }

        if (label == "chart HTML page") Assert(html.Contains(".chartforgex-chart{width:min(100%,360px)", StringComparison.Ordinal), label + " should preserve centered browser previews with stylesheet sizing.");
        if (label == "visual block HTML page") Assert(html.Contains(".chartforgex-visual-block{width:min(100%,", StringComparison.Ordinal), label + " should preserve centered browser previews with stylesheet sizing.");
        if (centered) Assert(html.Contains("body{margin:0;min-height:100vh;min-height:100svh;display:grid;place-items:center", StringComparison.Ordinal) && html.Contains("padding:clamp(16px,4vmin,52px)", StringComparison.Ordinal), label + " should center preview content with responsive padding.");
        else Assert(!html.Contains("body{margin:0;min-height:100vh;min-height:100svh;display:grid;place-items:center", StringComparison.Ordinal), label + " should keep report body layout top-aligned instead of preview-centered.");
    }
}
