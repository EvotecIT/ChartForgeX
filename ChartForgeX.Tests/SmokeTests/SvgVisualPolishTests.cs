using System;
using System.Globalization;
using System.Reflection;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SvgSurfaceAndGuideStrokesStayPremiumAtAnyScale() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(420, 260)
            .WithTitle("Premium scale")
            .WithSubtitle("Crisp at small and large sizes")
            .AddSmoothLine("Values", Points(10, 30, 20), ChartColor.FromRgb(37, 99, 235));
        var prepared = PreparedFamily(chart);
        var svg = prepared.ToSvg();

        var elements = System.Xml.Linq.XDocument.Parse(svg).Descendants().ToArray();
        var guides = elements.Where(element => (string?)element.Attribute("data-cfx-role") is "axis-x" or "axis-y" or "grid-x" or "grid-y").ToArray();
        var nativeGuides = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role is "axis-x" or "axis-y" or "grid-x" or "grid-y").ToArray();
        Assert(guides.Length > 0 && guides.Length == nativeGuides.Length && guides.Zip(nativeGuides, (element, line) =>
            (string?)element.Attribute("data-cfx-role") == line.Role
            && double.Parse(element.Attribute("stroke-width")!.Value, CultureInfo.InvariantCulture) == line.StrokeWidth
            && line.StrokeWidth > 0 && line.Stroke!.Value.A > 0 && (string?)element.Attribute("stroke") == line.Stroke.Value.ToCss()).All(matches => matches),
            "Native axes and grid lines should carry explicit visible guide paint and logical stroke width.");
        Assert(elements.Any(element => (string?)element.Attribute("data-cfx-role") == "line"
            && double.TryParse((string?)element.Attribute("stroke-width"), NumberStyles.Float, CultureInfo.InvariantCulture, out var width) && width > 0),
            "Native line geometry should carry its resolved stroke instead of depending on page CSS.");
        Assert(SvgHasAttributes(svg, "data-cfx-role=\"frame-card\"") && SvgHasAttributes(svg, "data-cfx-role=\"content-surface\""),
            "Native SVG should retain the shared card and plot surfaces.");
        Assert(elements.Where(element => element.Name.LocalName == "text").All(element => element.Attribute("font-family") != null
            && element.Attribute("font-size") != null && element.Attribute("font-weight") != null),
            "Native text should carry its resolved typography independently of the embedding page.");

        var html = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(420, 260)
            .WithTitle("Premium HTML")
            .AddSmoothLine("Values", Points(10, 30, 20), ChartColor.FromRgb(37, 99, 235))
            .ToHtmlPage();
        Assert(html.Contains("linear-gradient(180deg", StringComparison.Ordinal), "Standalone HTML pages should use a polished responsive surface background.");
        Assert(html.Contains("min-height:100svh", StringComparison.Ordinal), "Standalone HTML pages should use modern viewport sizing for browser previews.");
        Assert(html.Contains("@media print", StringComparison.Ordinal), "Standalone HTML pages should include print-friendly chart framing.");
    }

    private static void StaticHtmlShellsSharePremiumPreviewPolish() {
        var chart = Chart.Create().WithTheme(ChartTheme.Light())
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
        AssertPremiumHtmlShell(grid.ToHtmlPage(), centered: true, "chart grid HTML page");

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
        var bulletChart = Chart.Create()
            .WithSize(920, 560)
            .WithTheme(ChartTheme.ReportDark())
            .AddBullet("DMARC enforcement", 88, 95, 0, 100, new[] { 60d, 80d }, ChartColor.FromRgb(52, 211, 153))
            .AddBullet("DNSSEC coverage", 74, 90, 0, 100, new[] { 55d, 78d }, ChartColor.FromRgb(96, 165, 250));
        var bulletPrepared = PreparedFamily(bulletChart);
        var bulletPadding = ChartForgeX.Rendering.VisualExportRequest.ForChart(bulletChart).Context.Layout.PaddingEdges;
        Assert(FamilyLabels(bulletPrepared, "bullet-row-label").All(label => label.Text.Lines.All(line => label.LineLeft(line) >= bulletPadding.Left)), "Bullet row labels should honor the common frame padding.");

        var narrowBulletChart = Chart.Create()
            .WithSize(180, 140)
            .WithLegend(false)
            .WithPadding(24, 24, 24, 24)
            .AddBullet("Compact control posture label", 74, 90, 0, 100, new[] { 45d, 70d }, ChartColor.FromRgb(96, 165, 250));
        narrowBulletChart.Series[0].WithDataLabels(false);
        var narrowBullet = narrowBulletChart.ToSvg("narrow-bullet");
        var valueX = double.Parse(GetStringAttribute(narrowBullet, "data-cfx-role=\"bullet-value\"", "x"), CultureInfo.InvariantCulture);
        var valueWidth = double.Parse(GetStringAttribute(narrowBullet, "data-cfx-role=\"bullet-value\"", "width"), CultureInfo.InvariantCulture);
        Assert(valueX >= 0 && valueX + valueWidth <= 180, "Narrow bullet charts should keep bars inside the chart viewport after internal padding.");
        Assert(narrowBulletChart.ToPng().Length > 64, "Narrow bullet chart bounds should render in PNG output.");

        var treeChart = Chart.Create()
            .WithSize(1040, 600)
            .WithTheme(ChartTheme.ReportLight())
            .AddTree("Control hierarchy", new[] { new ChartNode("Security posture", "Security posture"), new ChartNode("Mail authentication", "Mail authentication"), new ChartNode("Certificate lifecycle", "Certificate lifecycle"), new ChartNode("DNS hygiene", "DNS hygiene"), new ChartNode("SPF alignment", "SPF alignment"), new ChartNode("DKIM rotation", "DKIM rotation"), new ChartNode("Expiry monitoring", "Expiry monitoring"), new ChartNode("SAN inventory", "SAN inventory"), new ChartNode("DNSSEC rollout", "DNSSEC rollout"), new ChartNode("Stale record cleanup", "Stale record cleanup") }, new[] {
                new ChartTreeLink("Security posture", "Mail authentication", 3),
                new ChartTreeLink("Security posture", "Certificate lifecycle", 2),
                new ChartTreeLink("Security posture", "DNS hygiene", 2),
                new ChartTreeLink("Mail authentication", "SPF alignment"),
                new ChartTreeLink("Mail authentication", "DKIM rotation"),
                new ChartTreeLink("Certificate lifecycle", "Expiry monitoring"),
                new ChartTreeLink("Certificate lifecycle", "SAN inventory"),
                new ChartTreeLink("DNS hygiene", "DNSSEC rollout"),
                new ChartTreeLink("DNS hygiene", "Stale record cleanup")
            });
        var tree = PreparedFamily(treeChart);
        var treePadding = ChartForgeX.Rendering.VisualExportRequest.ForChart(treeChart).Context.Layout.PaddingEdges;
        var nodes = tree.Scene.Nodes.OfType<ChartForgeX.Rendering.VisualSceneRectangle>().Where(node => node.Role == "tree-node-mark").ToArray();
        Assert(nodes.Length == 10 && nodes.All(node => node.Bounds.Left >= treePadding.Left && node.Bounds.Right <= tree.Size.Width - treePadding.Right), "Every tree node should stay inside the common horizontal frame padding.");
        var links = tree.Scene.Nodes.OfType<ChartForgeX.Rendering.VisualScenePath>().Where(path => path.Role == "tree-link-path").ToArray();
        Assert(links.Length == 9 && links.All(link => link.Commands.Any(command => command.Kind == ChartPathCommandKind.CubicTo)), "Tree relationships should retain their weighted curved native paths.");
        Assert(FamilyLabels(tree, "tree-node-label").Length == nodes.Length, "Tree nodes should retain their fitted labels.");
        Assert(tree.ToPng().Length > 64, "Fitted tree layout should render through the native painter.");
    }

    private static void SharedRoutePolishReachesTopologyAndMapOutputs() {
        var topology = CreateSampleTopologyChart().ToSvg();
        Assert(topology.Contains("data-cfx-role=\"topology-edge-line-halo\"", StringComparison.Ordinal), "Topology routes should use the shared premium route halo layer.");
        Assert(topology.Contains("data-cfx-role=\"topology-edge-line-highlight\"", StringComparison.Ordinal), "Topology routes should use the shared premium route highlight layer.");
        Assert(TopologyEdgeLine(topology, "amer-emea").RenderedColor("stroke").A > 0, "Premium topology routes should retain a visible base stroke beneath the shared halo and highlight layers.");
        var cssColorTopology = CreateSampleTopologyChart().WithEdgeColor("amer-emea", "var(--directory-edge)").ToSvg();
        Assert(TopologyEdgeLine(cssColorTopology, "amer-emea").Attribute("stroke")!.Value.StartsWith("var(--directory-edge, ", StringComparison.Ordinal), "Topology SVG routes should preserve caller-supplied CSS edge colors with a concrete static fallback.");
        var cssColorLayeredTopology = CreateSampleTopologyChart().WithEdgeColor("amer-emea", "var(--directory-edge)").ToSvg(new TopologyRenderOptions().WithLuminousTopologyEdges());
        Assert(cssColorLayeredTopology.Contains("data-cfx-role=\"topology-edge-line-halo\"", StringComparison.Ordinal) && cssColorLayeredTopology.Contains("data-cfx-role=\"topology-edge-line-highlight\"", StringComparison.Ordinal) && TopologyEdgeLine(cssColorLayeredTopology, "amer-emea").Attribute("stroke")!.Value.StartsWith("var(--directory-edge, ", StringComparison.Ordinal), "Topology SVG routes should keep configured premium layers when caller edge colors use CSS syntax.");
        var plainEdgeTopology = CreateSampleTopologyChart().ToSvg(new TopologyRenderOptions().WithPlainTopologyEdges());
        Assert(!plainEdgeTopology.Contains("data-cfx-role=\"topology-edge-line-halo\"", StringComparison.Ordinal) && !plainEdgeTopology.Contains("data-cfx-role=\"topology-edge-line-highlight\"", StringComparison.Ordinal) && CountOccurrences(plainEdgeTopology, "data-cfx-role=\"topology-edge-line\"") == 2, "Topology edge polish should be configurable down to crisp single-stroke route lines.");
        var floatingLabelTopology = CreateSampleTopologyChart().ToSvg(new TopologyRenderOptions { VisualStyle = TopologyVisualStyle.MonitoringDashboard, IncludeEdgeLabelBackplates = false });
        var floatingText = System.Xml.Linq.XDocument.Parse(floatingLabelTopology).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-label-text").SelectMany(element => element.DescendantsAndSelf()).Where(element => element.Name.LocalName == "text").ToArray();
        Assert(floatingText.Length > 0 && floatingText.All(element => element.Attribute("stroke") != null && (string?)element.Attribute("paint-order") == "stroke"), "Topology floating edge labels should paint readable glyph outlines.");
        Assert(CreateSampleTopologyChart().ToPng().Length > 64, "Premium topology route polish should render in PNG output.");

        var mapChart = Chart.Create()
            .WithSize(520, 320)
            .WithMapViewport(ChartMapViewport.Europe())
            .WithDataLabels()
            .AddDottedMap("Revenue", new[] {
                new ChartMapPoint("Poland", 19.1451, 51.9194, 142, ChartColor.FromHex("#DC2626")),
                new ChartMapPoint("Germany", 10.4515, 51.1657, 214, ChartColor.FromHex("#22C55E")),
                new ChartMapPoint("Spain", -3.7038, 40.4168, 96, ChartColor.FromHex("#F59E0B"))
            });
        var map = mapChart.ToSvg("premium-map");
        Assert(map.Contains("data-cfx-role=\"dotted-map-label-leader-halo\"", StringComparison.Ordinal), "Dotted-map label leaders should share premium route halo styling.");
        Assert(CountOccurrences(map, "data-cfx-role=\"dotted-map-label-leader\"") == 3, "Dotted-map foreground leader metadata should stay stable while premium layers are added.");
        Assert(mapChart.ToPng().Length > 64, "Premium dotted-map leader polish should render in PNG output.");
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
        Assert(GetStringAttribute(svg, "data-cfx-role=\"metric-status-bar\"", "x") == "1.5", "Carded metric status bars should sit inside the card border instead of covering the rounded frame.");
        Assert(svg.Contains("-visualBackground", StringComparison.Ordinal), "Opaque carded visual blocks should keep their configured SVG page background behind rounded card corners.");
        var metricPng = ReadPngRgba(metric.ToPng(), out var metricPngWidth, out var metricPngHeight);
        Assert(AlphaAt(metricPng, metricPngWidth, 1, metricPngHeight - 2) == 255, "Opaque carded visual block PNG output should keep the outside of rounded card corners on the theme background.");

        var transparentMetric = MetricCard.Create()
            .WithMetric("Monthly Recurring Revenue", "$120,400")
            .WithStatus(VisualStatus.Positive)
            .WithTransparentBackground()
            .WithTheme(ChartTheme.ReportLight().WithCornerRadius(26, 12))
            .WithSize(420, 190);
        Assert(!transparentMetric.ToSvg("transparent-rounded-status-bar").Contains("-visualBackground)", StringComparison.Ordinal), "Transparent visual blocks should not paint a square SVG page background behind rounded card corners.");
        var transparentPng = ReadPngRgba(transparentMetric.ToPng(), out var transparentPngWidth, out var transparentPngHeight);
        Assert(AlphaAt(transparentPng, transparentPngWidth, 1, transparentPngHeight - 2) == 0, "Transparent visual block PNG output should leave the outside of rounded card corners transparent.");

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

        var transparentCardTheme = ChartTheme.ReportLight().WithSurfaceColors(ChartColor.Transparent, ChartColor.Transparent, ChartColor.Transparent, ChartColor.Transparent, ChartColor.Transparent);
        var transparentCardChart = Chart.Create()
            .WithSize(360, 220)
            .WithTheme(transparentCardTheme)
            .AddLine("Signal", Points(12, 24, 18), ChartColor.FromRgb(96, 165, 250));
        Assert(!transparentCardChart.ToSvg().Contains("data-cfx-role=\"card-inner-highlight\"", StringComparison.Ordinal), "Transparent card backgrounds should not receive a visible SVG inner highlight.");
        Assert(transparentCardChart.ToPng().Length > 64, "Transparent card backgrounds should still render PNG output.");

        var transparentCardMetric = MetricCard.Create()
            .WithMetric("MRR", "$120K")
            .WithTheme(transparentCardTheme)
            .WithSize(260, 140);
        Assert(!transparentCardMetric.ToSvg("transparent-card-metric").Contains("data-cfx-role=\"visual-card-highlight\"", StringComparison.Ordinal), "Transparent visual block cards should not receive a visible SVG inner highlight.");
        Assert(transparentCardMetric.ToPng().Length > 64, "Transparent visual block cards should still render PNG output.");

        var transparentGrid = VisualGrid.CreateMetricStrip("Transparent", new[] {
                MetricCard.Create().WithMetric("Patch Rate", "94%").WithStatus(VisualStatus.Positive).WithTheme(transparentCardTheme),
                MetricCard.Create().WithMetric("Warnings", "18").WithStatus(VisualStatus.Warning).WithTheme(transparentCardTheme)
            }, columns: 2)
            .WithTheme(transparentCardTheme);
        var transparentGridSvg = transparentGrid.ToSvg("transparent-grid");
        Assert(!transparentGridSvg.Contains("data-cfx-role=\"visual-grid-frame-highlight\"", StringComparison.Ordinal), "Transparent visual-grid sections should render only their outer frame, without an extra inner highlight line.");
        var transparentGridPixels = ReadPngRgba(transparentGrid.ToPng(), out var transparentGridWidth, out _);
        Assert(AlphaAt(transparentGridPixels, transparentGridWidth, 1, 1) == 0, "Transparent visual-grid sections should preserve transparent corners for overlay usage.");

        var translucentTheme = ChartTheme.ReportLight().WithShadowOpacity(0);
        translucentTheme.Background = ChartColor.FromRgba(15, 23, 42, 96);
        translucentTheme.CardBackground = ChartColor.Transparent;
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

    private static void FunnelZeroStageAvoidsFakeDropoffGuide() {
        var chart = Chart.Create()
            .WithSize(920, 560)
            .WithTheme(ChartTheme.ReportLight())
            .WithXLabels("Opened", "Deferred", "Closed")
            .AddFunnel("Review flow", Points(100, 0, 18)).WithDataLabels();
        var svg = chart.ToSvg("zero-stage-funnel");
        var prepared = PreparedFamily(chart);
        Assert(FamilyLabels(prepared, "funnel-label").Any(label => FamilyContent(label) == "Deferred: 0"), "Zero-value funnel stages should retain a visible inline label.");
        Assert(!svg.Contains("data-cfx-role=\"funnel-zero-label-backdrop\"", StringComparison.Ordinal), "Zero-value funnel labels should avoid floating callout panels.");
        Assert(!svg.Contains("prev stage was 0", StringComparison.Ordinal), "Funnel stages after a zero stage should not show fake previous-stage drop-off text.");
        Assert(FamilyGroups(prepared, "funnel-stage")[2].Metadata["data-cfx-dropoff-defined"] == "false", "Funnel drop-off must remain undefined when its previous stage is zero.");
        Assert(FamilyLabels(prepared, "funnel-ratio").Any(label => FamilyContent(label).Contains("No previous baseline")), "The undefined zero-stage baseline should have an honest visible caption.");
        Assert(chart.ToPng().Length > 64, "Zero-value funnel stage polish should render PNG output.");
    }

    private static byte AlphaAt(byte[] rgba, int width, int x, int y) => rgba[(y * width + x) * 4 + 3];

    private static void AssertPremiumHtmlShell(string html, bool centered, string label) {
        Assert(html.Contains("body{margin:0;min-height:100vh;min-height:100svh", StringComparison.Ordinal), label + " should use the shared viewport-safe body shell.");
        Assert(label == "chart grid HTML page" ? html.Contains("background:", StringComparison.Ordinal) && !html.Contains("linear-gradient(180deg", StringComparison.Ordinal)
            : html.Contains("linear-gradient(180deg", StringComparison.Ordinal), label + " should use its shared flat or gradient page surface policy.");
        Assert(html.Contains("-webkit-font-smoothing:antialiased", StringComparison.Ordinal) && html.Contains("text-rendering:geometricPrecision", StringComparison.Ordinal), label + " should request browser text polish.");
        Assert(html.Contains("@media print{body{min-height:auto", StringComparison.Ordinal) && html.Contains("background:transparent", StringComparison.Ordinal), label + " should include shared print framing.");
        if (label == "chart HTML page" || label == "visual block HTML page") {
            Assert(html.Contains("style=\"box-sizing:border-box;overflow:visible\"", StringComparison.Ordinal), label + " should not keep inline width or max-width rules that block standalone screen and print sizing.");
            Assert(html.Contains(" svg{width:100%;height:auto}", StringComparison.Ordinal), label + " should force the embedded SVG to page width in print mode.");
            Assert(CountOccurrences(html, "<meta charset=\"utf-8\"") == 1, label + " should emit one canonical document head.");
            Assert(CountOccurrences(html, "<style>body{margin:0") == 1, label + " should not duplicate the standalone stylesheet.");
        }

        if (label == "chart HTML page") Assert(html.Contains(".chartforgex-chart{width:min(100%,360px)", StringComparison.Ordinal), label + " should preserve centered browser previews with stylesheet sizing.");
        if (label == "visual block HTML page") Assert(html.Contains(".chartforgex-visual-block{width:min(100%,", StringComparison.Ordinal), label + " should preserve centered browser previews with stylesheet sizing.");
        if (centered) Assert(html.Contains("body{margin:0;min-height:100vh;min-height:100svh;display:grid;place-items:center", StringComparison.Ordinal) && html.Contains("padding:clamp(16px,4vmin,52px)", StringComparison.Ordinal), label + " should center preview content with responsive padding.");
        else Assert(!html.Contains("body{margin:0;min-height:100vh;min-height:100svh;display:grid;place-items:center", StringComparison.Ordinal), label + " should keep report body layout top-aligned instead of preview-centered.");
    }
}
