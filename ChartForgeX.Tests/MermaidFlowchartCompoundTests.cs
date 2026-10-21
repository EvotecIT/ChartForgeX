using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidFlowchartCompoundTests {
    // Exercises the official flowchart subgraph, local-direction and shell-endpoint syntax.
    // https://mermaid.js.org/syntax/flowchart.html#direction-in-subgraphs
    internal static string Source => File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "flowchart-nested-directions.mmd"));

    [Fact]
    public void ParserRetainsSubgraphParentsDirectionsAndForwardShellReferences() {
        var result = new MermaidParser().ParseFlowchart(Source);
        Assert.Empty(result.Diagnostics);
        var document = result.Document!;
        Assert.Equal(new[] { "start", "A", "B", "C", "D", "finish" }, document.Nodes.Select(node => node.Id));
        Assert.Equal(new[] { ("delivery", (string?)null, MermaidFlowchartDirection.LeftToRight),
            ("intake", "delivery", MermaidFlowchartDirection.TopToBottom), ("review", "delivery", MermaidFlowchartDirection.RightToLeft) },
            document.Subgraphs.Select(group => (group.Id, group.ParentSubgraphId, group.Direction)));
        Assert.Equal(new[] { "A", "B" }, document.Subgraphs.Single(group => group.Id == "intake").NodeIds);
        var topology = document.ToTopologyChart();
        Assert.Equal(document.Nodes.Select(node => node.Id), topology.Nodes.Select(node => node.Id));
        Assert.Equal(document.Edges.Select(edge => (edge.SourceId, edge.TargetId)), topology.Edges.Select(edge => (edge.SourceNodeId, edge.TargetNodeId)));
        Assert.All(topology.Groups, group => Assert.Contains("mermaid.source.line", group.Metadata.Keys));
        Assert.Equal(TopologyLayoutDirection.RightToLeft, topology.Groups.Single(group => group.Id == "review").LayoutDirection);
    }

    [Theory]
    [InlineData("TB", 0, 1)]
    [InlineData("TD", 0, 1)]
    [InlineData("BT", 0, -1)]
    [InlineData("LR", 1, 0)]
    [InlineData("RL", -1, 0)]
    public void NativeLocalRanksRespectDirectionAndMinimumSpans(string direction, int horizontal, int vertical) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart TB\nsubgraph work[Work]\ndirection " + direction + "\nA ----> B --> C\nend");
        Assert.Empty(parsed.Diagnostics);
        var prepared = parsed.Document!.ToTopologyChart().Prepare();
        var envelope = prepared.ToInterchangeEnvelope();
        var a = envelope.Nodes.Single(node => node.Id == "A"); var b = envelope.Nodes.Single(node => node.Id == "B");
        Assert.True(horizontal == 0 || (b.X!.Value - a.X!.Value) * horizontal > a.Width!.Value);
        Assert.True(vertical == 0 || (b.Y!.Value - a.Y!.Value) * vertical > a.Height!.Value);
        Assert.Equal(3, envelope.Edges[0].Topology!.MinimumRankSpan);
        var report = prepared.Analyze();
        Assert.Empty(report.Collisions);
        Assert.All(report.Nodes, node => AssertContains(Assert.Single(report.Groups).Bounds, node.Bounds));
        Assert.All(report.Nodes, node => Assert.False(Overlaps(node.Bounds, Assert.Single(report.Groups).HeaderBounds!.Value)));
    }

    [Fact]
    public void ExternalMemberLinksInheritTheParentDirectionWhileShellLinksPreserveLocalDirection() {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\nsubgraph work[Work]\ndirection TB\nA --> B\nend\nB --> outside");
        Assert.Empty(parsed.Diagnostics);
        var topology = parsed.Document!.ToTopologyChart();
        Assert.Equal(MermaidFlowchartDirection.TopToBottom, Assert.Single(parsed.Document!.Subgraphs).Direction);
        Assert.Equal(TopologyLayoutDirection.LeftToRight, Assert.Single(topology.Groups).LayoutDirection);
        var report = topology.Prepare().Analyze();
        var a = report.Nodes.Single(node => node.Id == "A"); var b = report.Nodes.Single(node => node.Id == "B");
        Assert.True(b.Bounds.Left > a.Bounds.Right);
    }

    [Fact]
    public void NestedNativeGeometryKeepsShellEndpointsAndCaptionsWithoutPhantomNodes() {
        var parsed = new MermaidParser().ParseFlowchart(Source);
        var chart = parsed.Document!.ToTopologyChart();
        var prepared = chart.Prepare();
        var report = prepared.Analyze();
        Assert.Empty(report.Collisions);
        var delivery = report.Groups.Single(group => group.Id == "delivery");
        var intake = report.Groups.Single(group => group.Id == "intake"); var review = report.Groups.Single(group => group.Id == "review");
        AssertContains(delivery.Bounds, intake.Bounds); AssertContains(delivery.Bounds, review.Bounds);
        Assert.True(review.Bounds.Left > intake.Bounds.Right);
        var a = report.Nodes.Single(node => node.Id == "A"); var b = report.Nodes.Single(node => node.Id == "B");
        var c = report.Nodes.Single(node => node.Id == "C"); var d = report.Nodes.Single(node => node.Id == "D");
        Assert.True(b.Bounds.Top > a.Bounds.Bottom); Assert.True(c.Bounds.Left > d.Bounds.Right);
        Assert.All(report.Edges, edge => { Assert.True(edge.SourceAttached, edge.Id); Assert.True(edge.TargetAttached, edge.Id); });
        Assert.Empty(report.RouteCrossings);
        Assert.All(report.EdgeLabels, label => {
            Assert.All(report.Nodes, node => Assert.False(Overlaps(label.Bounds, node.Bounds), label.EdgeId + " crosses " + node.Id));
            Assert.All(report.Groups, group => Assert.False(group.HeaderBounds.HasValue && Overlaps(label.Bounds, group.HeaderBounds.Value), label.EdgeId + " crosses " + group.Id));
        });
        var exported = VisualArtifactInterchangeEnvelope.FromJson(prepared.ToInterchangeEnvelope().ToJson());
        Assert.Equal(6, exported.Nodes.Count); Assert.Equal(3, exported.Groups.Count);
        Assert.Equal("delivery", exported.Groups.Single(group => group.Id == "intake").Topology!.ParentGroupId);
        Assert.Equal(TopologyLayoutDirection.RightToLeft, exported.Groups.Single(group => group.Id == "review").Topology!.LayoutDirection);
        Assert.Contains(exported.Edges, edge => edge.SourceId == "intake" && edge.TargetId == "review");
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(6, svg.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "topology-node"));
        Assert.Equal(3, svg.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "topology-group"));
        Assert.True(prepared.ToPng().Length > 100);
    }

    [Theory]
    [InlineData(false, 360)]
    [InlineData(true, 360)]
    [InlineData(false, 1100)]
    [InlineData(true, 1100)]
    public void NaturalCompoundExportsKeepOuterRoutesInsideTheNativeClip(bool dark, int width) {
        var source = "%%{init: {'fontFamily':'" + BrowserProofFonts.Family + "','theme':'" + (dark ? "dark" : "default") + "'}}%%\n" + Source;
        var result = MermaidRenderer.Render(source, new MermaidRenderOptions { Flowchart = new MermaidFlowchartRenderOptions { Width = width, Height = 760 } });
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        var svg = XDocument.Parse(artifact.ToSvg());
        var entry = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-edge" && (string?)element.Attribute("data-edge-id") == "mermaid-edge-0");
        var clipUrl = (string)entry.Ancestors().First(element => element.Attribute("clip-path") != null).Attribute("clip-path")!;
        var clipId = clipUrl.Substring(5, clipUrl.Length - 6);
        var clip = svg.Descendants().Single(element => element.Name.LocalName == "clipPath" && (string?)element.Attribute("id") == clipId).Elements().Single();
        var left = Number(clip, "x"); var top = Number(clip, "y");
        var right = left + Number(clip, "width"); var bottom = top + Number(clip, "height");
        var paths = svg.Descendants().Where(element => ((string?)element.Attribute("data-cfx-role"))?.StartsWith("topology-edge-line", StringComparison.Ordinal) == true
            || (string?)element.Attribute("data-cfx-role") == "topology-marker").ToArray();
        Assert.NotEmpty(paths);
        Assert.All(paths, path => {
            var halfStroke = double.TryParse((string?)path.Attribute("stroke-width"), NumberStyles.Float, CultureInfo.InvariantCulture, out var stroke) ? stroke / 2 : 0;
            Assert.All(Points(path), point => {
                Assert.InRange(point.X - halfStroke, left, right);
                Assert.InRange(point.X + halfStroke, left, right);
                Assert.InRange(point.Y - halfStroke, top, bottom);
                Assert.InRange(point.Y + halfStroke, top, bottom);
            });
        });
        var route = Points(entry.Elements().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-line"));
        var leg = route.Zip(route.Skip(1), (start, end) => (start, end)).Single(segment => segment.start.X == segment.end.X && Math.Abs(segment.start.Y - segment.end.Y) > 20);
        var raster = PngReader.Decode(artifact.ToPng());
        var x = (int)Math.Round(leg.start.X); var y = (int)Math.Round((leg.start.Y + leg.end.Y) / 2);
        var line = (y * raster.Width + x) * 4;
        var background = (y * raster.Width + x + 6) * 4;
        Assert.True(Enumerable.Range(0, 3).Sum(channel => Math.Abs(raster.Pixels[line + channel] - raster.Pixels[background + channel])) > 10,
            "The outer group-entry leg must remain visible in the native raster.");

        static double Number(XElement element, string name) => double.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);
        static ChartPoint[] Points(XElement element) {
            var numbers = Regex.Matches((string)element.Attribute("d")!, @"-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?")
                .Cast<Match>().Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
            return Enumerable.Range(0, numbers.Length / 2).Select(index => new ChartPoint(numbers[index * 2], numbers[index * 2 + 1])).ToArray();
        }
    }

    [Fact]
    public void CompoundLayoutRetainsParallelSelfEdgesAndSourcePaint() {
        const string source = "flowchart LR\nsubgraph work[Work]\ndirection TB\nA@{shape: rounded, label: Start}:::ready -->|one|B\nA -->|two|B\nA -->|retry|A\nend\nclassDef ready fill:#d6e7fa,stroke:#3d68a0\nlinkStyle 1 stroke:#35632a";
        var result = MermaidRenderer.Render(source);
        Assert.Empty(result.Diagnostics);
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(result.Artifact!.ToInterchangeJson());
        Assert.Equal(2, envelope.Nodes.Count); Assert.Equal(3, envelope.Edges.Count);
        Assert.Equal("#d6e7fa", envelope.Nodes.Single(node => node.Id == "A").BackgroundColor);
        Assert.Equal("#35632a", envelope.Edges[1].Color);
        Assert.Equal(new[] { "one", "two", "retry" }, envelope.Edges.Select(edge => edge.Label));
        Assert.All(envelope.Edges, edge => Assert.True(edge.ResolvedRoute.Count > 1));
    }

    [Fact]
    public void GroupViewsKeepDescendantFactsAndDetachOmittedAncestors() {
        var chart = new MermaidParser().ParseFlowchart(Source).Document!.ToTopologyChart();
        var parentView = new TopologyView(); parentView.GroupIds.Add("delivery");
        var parent = chart.Prepare(new TopologyRenderOptions { View = parentView }).ToInterchangeEnvelope();
        Assert.Equal(new[] { "A", "B", "C", "D" }, parent.Nodes.Select(node => node.Id));
        Assert.Equal(new[] { "delivery", "intake", "review" }, parent.Groups.Select(group => group.Id));
        Assert.Equal(3, parent.Edges.Count);
        Assert.Contains(parent.Edges, edge => edge.SourceId == "intake" && edge.TargetId == "review");
        var childView = new TopologyView(); childView.GroupIds.Add("intake");
        var child = chart.Prepare(new TopologyRenderOptions { View = childView }).ToInterchangeEnvelope();
        Assert.Equal(new[] { "A", "B" }, child.Nodes.Select(node => node.Id));
        Assert.Null(Assert.Single(child.Groups).Topology!.ParentGroupId);
        Assert.Single(child.Edges);
    }

    [Fact]
    public void NativeCompoundItemsKeepSeparateGroupAndNodeIdentitySpaces() {
        var chart = TopologyChart.Create().WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("nested", "Nested").AddAutoGroup("A", "Parent")
            .AddAutoNode("A", "Request").AddAutoNode("B", "Work", groupId: "nested")
            .AddEdge("go", "A", "B");
        chart.Groups[0].ParentGroupId = "A";
        chart.Groups[1].LayoutDirection = TopologyLayoutDirection.TopToBottom;
        var prepared = chart.Prepare();
        var envelope = prepared.ToInterchangeEnvelope();
        Assert.Equal(new[] { "node-A", "B" }, envelope.Nodes.Select(node => node.Id));
        Assert.Equal("A", envelope.Groups.Single(group => group.Id == "nested").Topology!.ParentGroupId);
        Assert.Equal(("node-A", "B"), (Assert.Single(envelope.Edges).SourceId, envelope.Edges[0].TargetId));
        var groupOrder = XDocument.Parse(prepared.ToSvg()).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-group")
            .Select(element => (string?)element.Attribute("data-group-id"));
        Assert.Equal(new[] { "A", "nested" }, groupOrder);
    }

    [Theory]
    [InlineData("cycle")]
    [InlineData("missing")]
    [InlineData("depth")]
    public void NativeCompoundHierarchyRejectsUnsafeParentGraphsBeforeRecursiveLayout(string invalid) {
        var chart = TopologyChart.Create().WithLayout(TopologyLayoutMode.Layered);
        var count = invalid == "depth" ? 129 : 2;
        for (var index = 0; index < count; index++) {
            chart.AddAutoGroup("g" + index, "Group " + index);
            if (index > 0) chart.Groups[index].ParentGroupId = "g" + (index - 1);
        }
        if (invalid == "cycle") chart.Groups[0].ParentGroupId = "g1";
        if (invalid == "missing") chart.Groups[0].ParentGroupId = "unknown";
        var error = Assert.Throws<TopologyValidationException>(() => chart.Prepare());
        Assert.Contains(error.Result.Errors, issue => issue.Code == (invalid == "cycle" ? "group-parent-cycle" : invalid == "missing" ? "missing-parent-group" : "group-parent-depth"));
    }

    [BrowserTheory]
    [InlineData(false, 360)]
    [InlineData(false, 1100)]
    [InlineData(true, 360)]
    [InlineData(true, 1100)]
    public async Task NestedCompoundCaptionsShareNativeAndBrowserEvidence(bool dark, int width) {
        var source = "%%{init: {'fontFamily':'" + BrowserProofFonts.Family + "','theme':'" + (dark ? "dark" : "default") + "'}}%%\n" + Source;
        var result = MermaidRenderer.Render(source, new MermaidRenderOptions { Flowchart = new MermaidFlowchartRenderOptions { Width = width, Height = 760 } });
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(), width, 760);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        var host = session.Page.Locator("[data-cfx-html-sizing='preserve-size']");
        await host.FocusAsync();
        Assert.True(await host.EvaluateAsync<bool>("element => document.activeElement === element"));
        var dimensions = await host.EvaluateAsync<double[]>("element => [element.clientWidth, element.scrollWidth, element.querySelector('svg').getBoundingClientRect().width]");
        if (width == 360) Assert.True(dimensions[1] > dimensions[0]);
        var counts = await session.Page.EvaluateAsync<int[]>("() => ['topology-node','topology-group','topology-edge-label'].map(role => document.querySelectorAll('[data-cfx-role=\"' + role + '\"]').length)");
        Assert.Equal(new[] { 6, 3, 5 }, counts);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = "compound-" + width.ToString(CultureInfo.InvariantCulture) + (dark ? "-dark" : "-light");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
        }
        var scroll = await host.EvaluateAsync<double>("element => { element.scrollLeft = element.scrollWidth; return element.scrollLeft; }");
        if (width == 360) Assert.True(scroll > 0);
        if (string.IsNullOrWhiteSpace(output)) return;
        File.WriteAllText(Path.Combine(output, name + ".svg"), svg);
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), png);
        File.WriteAllText(Path.Combine(output, name + ".json"), JsonSerializer.Serialize(new { width, height = 760, dark, hostWidth = dimensions[0], scrollWidth = dimensions[1], svgWidth = dimensions[2], scroll, nodes = 6, groups = 3, labels = 5, consoleErrors = 0 }));
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".scrolled.browser.png"), FullPage = true });
    }

    private static void AssertContains(ChartRect outer, ChartRect inner) {
        Assert.True(inner.Left >= outer.Left && inner.Top >= outer.Top && inner.Right <= outer.Right && inner.Bottom <= outer.Bottom);
    }
    private static bool Overlaps(ChartRect a, ChartRect b) => a.Left < b.Right && a.Right > b.Left && a.Top < b.Bottom && a.Bottom > b.Top;
}
