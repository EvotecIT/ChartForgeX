using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using System.Globalization;
using System.Reflection;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ConsumerRenderingRegressionTests {
    [Fact]
    public void SequenceParticipantTextIsVerticallyCentered() {
        var result = MermaidRenderer.Render("sequenceDiagram\n participant App\n participant IdP as Identity provider\n App->>IdP: Authorization request");
        Assert.False(result.HasErrors);
        var nodes = XDocument.Parse(result.Artifact!.ToSvg()).Descendants();
        foreach (var group in nodes.Where(e => Role(e) == "sequence-participant")) {
            var box = group.Descendants().Single(e => Role(e) == "sequence-participant-box");
            var text = group.Descendants().First(e => e.Name.LocalName == "text");
            // The visible ink center allows font ascent/descent differences, but not a top-aligned label.
            var center = N(text, "y") - N(text, "font-size") * .3;
            Assert.InRange(center - (N(box, "y") + N(box, "height") / 2), -5, 5);
        }
        Assert.NotEmpty(result.Artifact!.ToPng());
    }
    [Fact]
    public void AltRendersOnlyAuthoredBranchCaptions() {
        var result = MermaidRenderer.Render("sequenceDiagram\n A->>B: Request\n alt valid token\n B-->>A: Success\n else expired token\n B-->>A: Retry\n end");
        var text = XDocument.Parse(result.Artifact!.ToSvg()).Descendants().Where(e => e.Name.LocalName == "text").Select(e => e.Value).ToArray();
        Assert.DoesNotContain(text, value => value.StartsWith("primary", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(text, value => value.Contains("valid token"));
        Assert.Contains(text, value => value.Contains("expired token"));
    }
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void CardStatusBarSpansTheLogicalCardAtEveryOutputScale(int scale) {
        var card = MetricCard.Create().WithSize(460, 200).WithCard(true).WithMetric("CPU", "38")
            .WithStatus(VisualStatus.Warning).WithPngOutputScale(scale);
        var image = card.RenderRgba();
        var svg = XDocument.Parse(card.ToSvg());
        var bar = svg.Descendants().Single(e => Role(e) == "metric-status-bar");
        var x = (int)((N(bar, "x") + N(bar, "width") / 2) * scale);
        var top = Pixel(image, x, 30 * scale);
        var bottom = Pixel(image, x, 170 * scale);
        Assert.True(top.SequenceEqual(bottom), "The status bar must paint the same color at both ends of the card.");
        Assert.False(bottom.SequenceEqual(Pixel(image, 20 * scale, 170 * scale)));
    }
    [Fact]
    public void ZeroPaddingListKeepsItsTextWithoutLeaderLines() {
        var list = ChartList.Create().WithSize(330, 140).WithPadding(0).WithCard(false)
            .AddCheckItem("Backups verified", true, "02:10").AddCheckItem("DR test", false, "due 30 Oct");
        var svg = XDocument.Parse(list.ToSvg());
        Assert.Contains(svg.Descendants(), e => e.Name.LocalName == "text" && e.Value == "Backups verified");
        Assert.DoesNotContain(svg.Descendants(), e => Role(e).Contains("leader"));
        Assert.NotEmpty(list.ToPng());
    }
    [Theory]
    [InlineData("progressive-gray", 32, 24)]
    [InlineData("progressive-rgb", 32, 24)]
    [InlineData("progressive-rgb-padded", 24, 24)]
    [InlineData("progressive-rgb-refinement", 32, 32)]
    public void ProgressiveJpegPreservesScanTablesEdgeBlocksAndRefinementPixels(string name, int width, int height) {
        byte[] Read(string suffix) {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ChartForgeX.Tests.Fixtures.Raster." + name + suffix)!;
            using var output = new MemoryStream(); stream.CopyTo(output); return output.ToArray();
        }
        var image = RasterImageDecoder.Decode(Read(".jpg"));
        var expected = Read(".rgb");
        Assert.Equal(width, image.Width); Assert.Equal(height, image.Height);
        var totalError = 0d;
        for (var i = 0; i < expected.Length; i++) totalError += Math.Abs(expected[i] - image.Pixels[(i / 3) * 4 + i % 3]);
        Assert.InRange(totalError / expected.Length, 0, 3);
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight)]
    [InlineData(TopologyLayoutDirection.RightToLeft)]
    [InlineData(TopologyLayoutDirection.TopToBottom)]
    [InlineData(TopologyLayoutDirection.BottomToTop)]
    public void LayeredAutomaticGroupsHaveSeparateShells(TopologyLayoutDirection direction) {
        var chart = TopologyChart.Create().WithViewport(680, 330).WithLayout(TopologyLayoutMode.Layered).WithLayoutDirection(direction)
            .AddAutoGroup("edge", "Edge").AddAutoGroup("core", "Core")
            .AddAutoNode("gw", "Gateway", TopologyNodeKind.Network, groupId: "edge")
            .AddAutoNode("api", "API", TopologyNodeKind.Service, groupId: "core")
            .AddAutoNode("db", "Database", TopologyNodeKind.Database, groupId: "core")
            .AddEdge("a", "gw", "api").AddEdge("b", "api", "db");
        var prepared = TopologyLayoutEngine.Prepare(chart);
        var a = prepared.Groups[0]; var b = prepared.Groups[1];
        var nodesById = prepared.Nodes.ToDictionary(n => n.Id);
        if (direction == TopologyLayoutDirection.LeftToRight) {
            Assert.True(nodesById["gw"].X < nodesById["api"].X);
            Assert.True(nodesById["api"].X < nodesById["db"].X);
        }
        Assert.True(a.X + a.Width <= b.X || b.X + b.Width <= a.X || a.Y + a.Height <= b.Y || b.Y + b.Height <= a.Y);
        Assert.All(chart.Groups, g => { Assert.Equal(0, g.X); Assert.Equal(0, g.Width); });
        foreach (var group in prepared.Groups) foreach (var node in prepared.Nodes.Where(n => n.GroupId == group.Id)) {
            Assert.InRange(node.X, group.X, group.X + group.Width - node.Width);
            Assert.InRange(node.Y, group.Y, group.Y + group.Height - node.Height);
        }
    }
    [Fact]
    public void CardHeadingUsesSpaceAboveItsGlyph() {
        var chart = TopologyChart.Create().WithViewport(300, 180).WithLegend(null)
            .AddNode("paw", "PAW-ADM-03", 40, 40, TopologyNodeKind.Network);
        var svg = XDocument.Parse(chart.ToSvg());
        Assert.Contains(svg.Descendants(), e => e.Name.LocalName == "text" && e.Value == "PAW-ADM-03");
    }
    [Theory]
    [InlineData("table")]
    [InlineData("list")]
    public void FactBodyTextAndRowsFollowThemeTypography(string kind) {
        XElement[] Texts(double size) {
            var theme = VisualTheme.Graphite().WithTypography(new VisualTypography(dataLabelSize: size));
            IVisualBlock block = kind == "table"
                ? ChartTable.Create().WithTheme(theme).WithSize(400, 300).WithColumns("Name").AddRow("First").AddRow("Second")
                : ChartList.Create().WithTheme(theme).WithSize(400, 300).AddItem("First").AddItem("Second");
            return XDocument.Parse(block.ToSvg()).Descendants().Where(e => e.Name.LocalName == "text" && (e.Value == "First" || e.Value == "Second")).ToArray();
        }
        var small = Texts(12); var large = Texts(24);
        Assert.Equal(2, small.Length); Assert.Equal(2, large.Length);
        Assert.Equal(N(small[0], "font-size") * 2, N(large[0], "font-size"), 3);
        Assert.Equal((N(small[1], "y") - N(small[0], "y")) * 2, N(large[1], "y") - N(large[0], "y"), 3);
    }
    [Fact]
    public void MetricBadgeClearsInlineHistoryAndUsesThemeInk() {
        var card = MetricCard.Create().WithTheme(VisualTheme.Graphite(), VisualThemeMode.Dark).WithSize(330, 150).WithPadding(0)
            .WithMetric("CPU", "38").WithSymbol("CPU").WithMiniSparkline(new[] { 12d, 24, 18, 38 });
        var nodes = XDocument.Parse(card.ToSvg()).Descendants().ToArray();
        var badge = nodes.Single(e => Role(e) == "metric-symbol-badge");
        var line = nodes.Single(e => Role(e) == "metric-mini-sparkline-line");
        var points = ((string)line.Attribute("points")!).Split(' ').Select(p => double.Parse(p.Split(',')[1], CultureInfo.InvariantCulture));
        Assert.True(points.Min() >= N(badge, "cy") + N(badge, "r") + 4);
        var symbol = nodes.Single(e => Role(e) == "metric-symbol");
        Assert.Equal(VisualTheme.Graphite().Resolve(VisualThemeMode.Dark).Foreground.ToCss(), (string?)symbol.Attribute("fill"));
        Assert.Contains(nodes, e => Role(e) == "metric-value" && e.Value == "38" && (string?)e.Attribute("display") != "none");
    }
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void MetricRoundBadgeStaysInsideCardPaddingAtEveryOutputScale(int scale) {
        var card = MetricCard.Create().WithTheme(VisualTheme.Graphite(), VisualThemeMode.Dark).WithSize(330, 150)
            .WithCard(true).WithPadding(0).WithMetric("CPU", "38").WithSymbol("CPU").WithPngOutputScale(scale);
        var nodes = XDocument.Parse(card.ToSvg()).Descendants().ToArray();
        var badge = nodes.Single(e => Role(e) == "metric-symbol-badge");
        Assert.True(N(badge, "cx") + N(badge, "r") <= 314);
        var image = card.ToPng();
        Assert.NotEmpty(image);
    }
    [Fact]
    public void ConsumerExampleIndexOmitsArtifactsFromEarlierWrites() {
        var output = Path.Combine(Path.GetTempPath(), "ChartForgeX-consumer-index-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        try {
            File.WriteAllText(Path.Combine(output, "stale.svg"), "<svg/>");
            ConsumerRenderingExamples.Write(output);
            var index = File.ReadAllText(Path.Combine(output, "index.html"));
            Assert.DoesNotContain("stale.svg", index, StringComparison.Ordinal);
            Assert.Contains("sequence.svg", index, StringComparison.Ordinal);
            Assert.Contains("sequence.png", index, StringComparison.Ordinal);
        } finally {
            Directory.Delete(output, recursive: true);
        }
    }
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void FitContentToViewportExpandsSmallTopologyAndKeepsOutputScale(int scale) {
        var chart = TopologyChart.Create().WithViewport(1300, 560).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("gateway", "Gateway", 30, 40, width: 90, height: 52).AddNode("identity", "Identity service", 180, 40, width: 90, height: 52)
            .AddEdge("request", "gateway", "identity", "HTTPS");
        var options = new TopologyRenderOptions { FitContentToViewport = true, IncludeTitle = false, IncludeLegend = false, PngSupersamplingScale = scale };
        var original = XDocument.Parse(chart.ToSvg(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false }));
        var fitted = XDocument.Parse(chart.ToSvg(options));
        double LargestNodeLabel(XDocument document) => document.Descendants()
            .Where(e => Role(e) == "topology-node-label").SelectMany(e => e.DescendantsAndSelf())
            .Where(e => e.Name.LocalName == "text" && e.Attribute("font-size") != null)
            .Select(e => N(e, "font-size")).DefaultIfEmpty(0).Max();

        Assert.True(LargestNodeLabel(fitted) > LargestNodeLabel(original));
        var image = RasterImageDecoder.Decode(chart.ToPng(options));
        Assert.Equal(1300, image.Width);
        Assert.Equal(560, image.Height);
    }

    [Fact]
    public void HubAndSpokeContentUsesAvailablePanelSizeWhenFittingIsEnabled() {
        var chart = TopologyChart.Create().WithViewport(1300, 560).WithLayout(TopologyLayoutMode.HubAndSpoke)
            .AddAutoNode("site", "AMS-HQ", TopologyNodeKind.Hub)
            .AddAutoNode("dc", "DC01", TopologyNodeKind.Server)
            .AddAutoNode("branch", "Branch office", TopologyNodeKind.Server)
            .AddAutoNode("wan", "WAN edge", TopologyNodeKind.Network)
            .AddEdge("site-dc", "site", "dc").AddEdge("site-branch", "site", "branch").AddEdge("site-wan", "site", "wan");
        var fixedSvg = XDocument.Parse(chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }));
        var fittedOptions = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false };
        var fittedSvg = XDocument.Parse(chart.ToSvg(fittedOptions));
        var arrangedNodes = TopologyLayoutEngine.Prepare(chart, options: fittedOptions).Nodes;
        for (var first = 0; first < arrangedNodes.Count; first++) for (var second = first + 1; second < arrangedNodes.Count; second++) {
            var a = arrangedNodes[first]; var b = arrangedNodes[second];
            Assert.True(a.X + a.Width <= b.X || b.X + b.Width <= a.X || a.Y + a.Height <= b.Y || b.Y + b.Height <= a.Y,
                $"Fitted matrix nodes '{a.Id}' and '{b.Id}' overlap.");
        }
        double LargestNodeFont(XDocument document) => document.Descendants()
            .Where(element => Role(element) == "topology-node-label").SelectMany(element => element.DescendantsAndSelf())
            .Where(element => element.Name.LocalName == "text" && element.Attribute("font-size") != null)
            .Select(element => N(element, "font-size")).DefaultIfEmpty(0).Max();

        Assert.True(LargestNodeFont(fittedSvg) > LargestNodeFont(fixedSvg));
        Assert.NotEmpty(chart.ToPng(fittedOptions));
    }

    [Fact]
    public void FitContentToViewportKeepsLongTopologyCardCaptionsWhole() {
        var chart = TopologyChart.Create().WithViewport(1200, 260).WithLayout(TopologyLayoutMode.GroupGrid)
            .AddAutoGroup("site-a", "Primary authentication and directory services")
            .AddAutoGroup("site-b", "Secondary recovery services")
            .AddAutoNode("dc1", "DC01", groupId: "site-a")
            .AddAutoNode("dns1", "DNS and catalog", groupId: "site-a")
            .AddAutoNode("dc2", "DC02", groupId: "site-b")
            .AddAutoNode("backup", "Recovery coordinator", groupId: "site-b");
        var options = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false };
        var svg = XDocument.Parse(chart.ToSvg(options));
        var labels = svg.Descendants().Where(element => Role(element) == "topology-node-label")
            .SelectMany(element => element.DescendantsAndSelf()).Where(element => element.Name.LocalName == "text")
            .Select(element => element.Value).ToArray();

        Assert.Contains("Recovery coordinator", labels);
        Assert.NotEmpty(chart.ToPng(options));
    }

    [Fact]
    public void MermaidClassDefColorSetsNodeLabelInk() {
        var result = MermaidRenderer.Render("flowchart LR\n A[Escalation]\n classDef oncall color:#ef4444,fill:#111827,stroke:#64748b\n class A oncall");
        Assert.False(result.HasErrors);
        var node = Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes.Single(item => item.Id == "A");
        Assert.Equal("#ef4444", node.LabelColor);
        var svg = XDocument.Parse(result.Artifact!.ToSvg());
        var label = svg.Descendants().Where(element => Role(element) == "topology-node-label")
            .SelectMany(element => element.DescendantsAndSelf()).First(element => element.Name.LocalName == "text");
        Assert.Equal("#EF4444", (string?)label.Attribute("fill"));
        var envelope = result.Artifact.ToInterchangeEnvelope();
        Assert.Equal("#ef4444", envelope.Nodes.Single(item => item.Label == "Escalation").LabelColor);
        var roundTrip = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson());
        Assert.Equal("#ef4444", roundTrip.Nodes.Single(item => item.Label == "Escalation").LabelColor);
        Assert.NotEmpty(result.Artifact.ToPng());
    }
    [Fact]
    public void WideMermaidFlowchartsScaleToTheirExplicitViewport() {
        const string source = "flowchart LR\n alert[On-call alert] --> triage{Known issue?}\n triage -->|yes| runbook[Use recovery runbook]\n triage -->|no| owner[Page service owner]\n runbook --> verify[Verify recovery]\n owner --> verify";
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        TopologyRenderOptions RenderOptions(bool fit) => new() { FitContentToViewport = fit, IncludeLegend = false, IncludeTitle = false };
        string Render(bool fit) {
            chart.WithViewport(2400, 1000, 32);
            return chart.ToSvg(RenderOptions(fit));
        }
        double LargestNodeFont(string svg) => XDocument.Parse(svg).Descendants()
            .Where(element => Role(element) == "topology-node-label").SelectMany(element => element.DescendantsAndSelf())
            .Where(element => element.Name.LocalName == "text" && element.Attribute("font-size") != null)
            .Select(element => N(element, "font-size")).DefaultIfEmpty(0).Max();

        var fixedSvg = Render(false);
        var fitSvg = Render(true);
        Assert.True(LargestNodeFont(fitSvg) > LargestNodeFont(fixedSvg));
        Assert.NotEmpty(chart.ToPng(RenderOptions(true)));
    }
    [Fact]
    public void GroupGridKeepsEveryGroupShellSeparateInWideShortViewports() {
        var chart = TopologyChart.Create().WithViewport(1000, 180).WithLayout(TopologyLayoutMode.GroupGrid)
            .AddAutoGroup("one", "One").AddAutoGroup("two", "Two").AddAutoGroup("three", "Three").AddAutoGroup("four", "Four")
            .AddAutoNode("a", "A", groupId: "one").AddAutoNode("b", "B", groupId: "two")
            .AddAutoNode("c", "C", groupId: "three").AddAutoNode("d", "D", groupId: "four");
        var groups = TopologyLayoutEngine.Prepare(chart).Groups;

        for (var first = 0; first < groups.Count; first++) for (var second = first + 1; second < groups.Count; second++) {
            var a = groups[first]; var b = groups[second];
            Assert.True(a.X + a.Width <= b.X || b.X + b.Width <= a.X || a.Y + a.Height <= b.Y || b.Y + b.Height <= a.Y,
                $"Group shells '{a.Id}' and '{b.Id}' overlap.");
        }
    }
    [Fact]
    public void InfoStatusUsesThemeInfoAndListMarkersUseOnePaletteColor() {
        var theme = VisualTheme.Graphite().WithPalette(ChartColor.FromRgb(210, 30, 45), ChartColor.FromRgb(20, 170, 90), ChartColor.FromRgb(40, 90, 220));
        var status = MetricCard.Create().WithTheme(theme).WithSize(260, 120).WithStatus(VisualStatus.Info).WithMetric("Replication", "Pending");
        var statusBar = XDocument.Parse(status.ToSvg()).Descendants().Single(e => Role(e) == "metric-status-bar");
        Assert.Equal(theme.Resolve(VisualThemeMode.Light).Info.ToCss(), (string?)statusBar.Attribute("fill"));

        var list = ChartList.Create().WithTheme(theme).WithSize(360, 160)
            .AddItem("First").AddItem("Second").AddItem("Third");
        var markers = XDocument.Parse(list.ToSvg()).Descendants().Where(e => Role(e) == "list-marker")
            .Select(e => (string?)e.Attribute("fill")).Where(color => color != null).Distinct().ToArray();
        Assert.Single(markers);
        Assert.Equal(theme.Resolve(VisualThemeMode.Light).Palette[0].ToCss(), markers[0]);
    }
    [Fact]
    public void HorizontalBarsHonorPointColorsAndCategoryCaptions() {
        var chart = Chart.Create().WithSize(520, 220).WithXLabels("Alpha", "Beta", "Gamma")
            .AddHorizontalBar("Count", new[] { new ChartPoint(1, 26), new ChartPoint(2, 24), new ChartPoint(3, 19) });
        chart.Series[0].WithPointColor(0, ChartColor.FromRgb(255, 0, 0)).WithPointColor(1, ChartColor.FromRgb(0, 180, 0));
        var nodes = XDocument.Parse(chart.ToSvg()).Descendants().ToArray();
        Assert.Contains(nodes, e => e.Name.LocalName == "text" && e.Value == "Alpha");
        Assert.Contains(nodes, e => e.Name.LocalName == "text" && e.Value == "Beta");
        var bars = nodes.Where(e => Role(e) == "horizontal-bar").ToArray();
        Assert.Equal(3, bars.Length);
        Assert.Contains(bars, e => ((string?)e.Attribute("fill"))?.Contains("#FF0000", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(bars, e => ((string?)e.Attribute("fill"))?.Contains("#00B400", StringComparison.OrdinalIgnoreCase) == true);
    }
    [Fact]
    public void BulletHasThreeDistinctRangeBands() {
        var chart = Chart.Create().WithSize(520, 200).AddBullet("Coverage", 72, 85);
        var ranges = XDocument.Parse(chart.ToSvg()).Descendants().Where(e => Role(e) == "bullet-range").ToArray();
        Assert.Equal(3, ranges.Length);
        Assert.Equal(3, ranges.Select(e => (string?)e.Attribute("fill")).Distinct().Count());
    }

    private const string AlertFlow = "flowchart TD\n alert([Alert raised]) --> triage{Known issue?}\n triage -->|yes| runbook[Run the runbook]\n triage -->|no| owner[Page the service owner]\n runbook --> verify[Verify recovery]\n owner --> fix[Ship a fix]\n fix --> verify\n verify --> close((Close))";
    [Theory]
    [InlineData("TD")]
    [InlineData("BT")]
    [InlineData("LR")]
    [InlineData("RL")]
    public void AutomaticDecisionRoutesStayOutsideTheirOwnEndpointSurfaces(string direction) {
        var result = MermaidRenderer.Render(AlertFlow.Replace("flowchart TD", "flowchart " + direction));
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var prepared = TopologyLayoutEngine.Prepare(chart);
        var nodes = prepared.Nodes.ToDictionary(n => n.Id);
        foreach (var edge in prepared.Edges) {
            var points = TopologyRenderPrimitives.EdgePoints(prepared, edge, nodes);
            foreach (var node in nodes.Values) {
                for (var i = 1; i < points.Count; i++) {
                    var a = points[i - 1]; var b = points[i];
                    var left = node.X + 1; var right = node.X + node.Width - 1;
                    var top = node.Y + 1; var bottom = node.Y + node.Height - 1;
                    var crosses = Math.Abs(a.X - b.X) < .01
                        ? a.X > left && a.X < right && Math.Max(a.Y, b.Y) > top && Math.Min(a.Y, b.Y) < bottom
                        : Math.Abs(a.Y - b.Y) < .01 && a.Y > top && a.Y < bottom && Math.Max(a.X, b.X) > left && Math.Min(a.X, b.X) < right;
                    if (crosses && node.Shape == TopologyNodeShape.Diamond) {
                        var samples = Math.Max(1, (int)Math.Ceiling(Math.Abs(b.X - a.X) + Math.Abs(b.Y - a.Y)));
                        crosses = Enumerable.Range(0, samples + 1).Any(sample => {
                            var t = (double)sample / samples;
                            var x = a.X + (b.X - a.X) * t; var y = a.Y + (b.Y - a.Y) * t;
                            return Math.Abs(x - node.X - node.Width / 2) / (node.Width / 2)
                                + Math.Abs(y - node.Y - node.Height / 2) / (node.Height / 2) < .98;
                        });
                    }
                    Assert.False(crosses, edge.SourceNodeId + "->" + edge.TargetNodeId + " crosses " + node.Id);
                }
            }
        }
    }
    [Fact]
    public void LongLeftToRightFlowEntersTheFinalRankFromTheLeft() {
        var result = MermaidRenderer.Render(AlertFlow.Replace("flowchart TD", "flowchart LR"));
        var chart = TopologyLayoutEngine.Prepare(Assert.IsType<TopologyChart>(result.Artifact!.Model));
        var nodes = chart.Nodes.ToDictionary(n => n.Id);
        var edge = chart.Edges.Single(e => e.SourceNodeId == "runbook" && e.TargetNodeId == "verify");
        var route = TopologyRenderPrimitives.EdgePoints(chart, edge, nodes);
        Assert.True(route[^1].X <= nodes["verify"].X);
        Assert.Equal(route[^1].Y, route[^2].Y, 3);
    }
    [Fact]
    public void TopologyEdgeTextIsCenteredInItsRouteBadge() {
        var result = MermaidRenderer.Render("stateDiagram-v2\n [*] --> Draft\n Draft --> Review: submit\n Review --> Draft: request changes\n Review --> Approved: approve");
        var groups = XDocument.Parse(result.Artifact!.ToSvg()).Descendants().Where(e => Role(e) == "topology-edge-label");
        foreach (var group in groups) {
            var text = group.Descendants().First(e => e.Name.LocalName == "text");
            Assert.InRange(N(text, "y") - N(text, "font-size") * .3 - N(group, "data-label-y"), -4, 4);
        }
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void TreemapDefaultCaptionsRespectExplicitVisibility(bool? visible, bool expected) {
        var chart = Chart.Create().WithSize(520, 300).AddTreemap("Work", new[] { new ChartHierarchyItem("a", "Alpha", value: 3), new ChartHierarchyItem("b", "Beta", value: 2) });
        if (visible.HasValue) chart.WithDataLabels(visible.Value);
        var nodes = XDocument.Parse(chart.ToSvg()).Descendants();
        Assert.Equal(expected, nodes.Any(e => Role(e) == "treemap-label"));
    }
    [Fact]
    public void WideCalendarWeekdayCaptionsFollowTheCenteredGrid() {
        var start = new DateTime(2026, 1, 1);
        var items = Enumerable.Range(0, 126).Select(i => new ChartCalendarHeatmapItem(start.AddDays(i), i % 5 + 1));
        var chart = Chart.Create().WithSize(1400, 220).AddCalendarHeatmap("Sign-ins", items);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var left = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(e => e.Role == "calendar-cell-shape").Min(e => e.Bounds.Left);
        var captions = prepared.Regions.Where(e => e.Role == "calendar-weekday").ToArray();
        Assert.NotEmpty(captions);
        Assert.All(captions, e => Assert.InRange(left - e.Bounds.Right, 0, 30));
    }

    [Fact]
    public void CloseBarValuesKeepTheirCaptionsCenteredOnTheirOwnMark() {
        var chart = Chart.Create().WithSize(520, 220).WithDataLabels(true)
            .AddBar("Count", new[] { new ChartPoint(1, 26), new ChartPoint(2, 24), new ChartPoint(3, 19) });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var marks = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(e => e.Role == "bar").OrderBy(e => e.Bounds.Left).ToArray();
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(e => e.Text.Lines.Count == 1 && e.Text.Lines[0].Text is "26" or "24" or "19").OrderBy(e => e.X).ToArray();
        Assert.Equal(3, marks.Length); Assert.Equal(3, captions.Length);
        for (var i = 0; i < marks.Length; i++) Assert.InRange(captions[i].LineLeft(captions[i].Text.Lines[0]) + captions[i].Text.Metrics.Width / 2 - marks[i].Bounds.X - marks[i].Bounds.Width / 2, -1, 1);
    }
    [Fact]
    public void RadarCeilingCaptionDoesNotCoverItsFirstVertex() {
        var chart = Chart.Create().WithSize(520, 320).WithXLabels("Mail auth", "DNSSEC", "TLS", "CT", "Policy", "Monitoring")
            .AddRadar("Current", new[] { 78d, 62, 74, 80, 70, 66 }.Select((v, i) => new ChartPoint(i + 1, v)).ToArray())
            .AddRadar("Target", Enumerable.Range(1, 6).Select(i => new ChartPoint(i, 80)).ToArray());
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneText>(), e => e.Role == "radar-ring-label" && e.Text.Lines.Count == 1 && e.Text.Lines[0].Text == "80");
        Assert.Equal(12, prepared.Regions.Count(e => e.Role == "radar-point"));
    }

    [Fact]
    public void GroupedLayeredEdgeBadgesStayOnTheirOwnRoutes() {
        var chart = TopologyChart.Create().WithViewport(680, 330).WithLayout(TopologyLayoutMode.Layered).WithLayoutDirection(TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("edge", "Edge").AddAutoGroup("core", "Core")
            .AddAutoNode("gw", "Gateway", TopologyNodeKind.Network, groupId: "edge")
            .AddAutoNode("api", "API", TopologyNodeKind.Service, groupId: "core")
            .AddAutoNode("db", "Database", TopologyNodeKind.Database, groupId: "core")
            .AddEdge("a", "gw", "api", "HTTPS").AddEdge("b", "api", "db", "SQL");
        var options = new TopologyRenderOptions { IncludeLegend = false };
        var prepared = TopologyLayoutEngine.Prepare(chart, options: options);
        var labels = TopologyRenderPrimitives.EdgeLabelLayouts(prepared, options);
        Assert.Equal(2, labels.Count);
        Assert.All(labels, label => Assert.InRange(Math.Abs(label.CenterX - label.AnchorX) + Math.Abs(label.CenterY - label.AnchorY), 0, 1));
    }
    private static string Role(XElement e) => (string?)e.Attribute("data-cfx-role") ?? "";
    private static double N(XElement e, string attribute) => double.Parse((string)e.Attribute(attribute)!, CultureInfo.InvariantCulture);
    private static byte[] Pixel(RgbaImage image, int x, int y) => image.Pixels.AsSpan((y * image.Width + x) * 4, 4).ToArray();
}
