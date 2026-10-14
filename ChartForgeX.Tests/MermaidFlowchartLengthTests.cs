using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartLengthTests {
    [Theory]
    [InlineData("---", 1)]
    [InlineData("----", 2)]
    [InlineData("-----", 3)]
    [InlineData("-->", 1)]
    [InlineData("--->", 2)]
    [InlineData("---->", 3)]
    [InlineData("===", 1)]
    [InlineData("====", 2)]
    [InlineData("=====", 3)]
    [InlineData("==>", 1)]
    [InlineData("===>", 2)]
    [InlineData("====>", 3)]
    [InlineData("-.-", 1)]
    [InlineData("-..-", 2)]
    [InlineData("-...-", 3)]
    [InlineData("-.->", 1)]
    [InlineData("-..->", 2)]
    [InlineData("-...->", 3)]
    [InlineData("<---", 2)]
    [InlineData("<----", 3)]
    [InlineData("<--->", 2)]
    [InlineData("<---->", 3)]
    [InlineData("o---o", 2)]
    [InlineData("o----o", 3)]
    [InlineData("x---x", 2)]
    [InlineData("x----x", 3)]
    [InlineData("-- a-b --->", 2)]
    [InlineData("-- a-b ---->", 3)]
    [InlineData("-. a-b ..->", 2)]
    [InlineData("-->|a---b|", 1)]
    [InlineData("--->|a---b|", 2)]
    [InlineData("== three...dots ===>", 2)]
    [InlineData("o-- a-b ---o", 2)]
    [InlineData("-- a-b o---o", 2)]
    [InlineData("== a-b x====x", 3)]
    [InlineData("-. a-b o-..-o", 2)]
    [InlineData("-- a-b <--->", 2)]
    [InlineData("-- \"x ----> y\" --->", 2)]
    public void SourceOperatorsUseNativeMinimumRankSpans(string op, int expected) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\nA " + op + " B");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(expected, Assert.Single(parsed.Document!.ToTopologyChart().Edges).MinimumRankSpan);
    }

    [Fact]
    public void VersionedFixtureSharesNativeAndReferenceRankFacts() {
        var fixture = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "flowchart-link-lengths.mmd");
        var parsed = new MermaidParser().ParseFlowchart(File.ReadAllText(fixture));
        Assert.Empty(parsed.Diagnostics);
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.ChangeExtension(fixture, ".expected.json")));
        var chart = parsed.Document!.ToTopologyChart();
        Assert.Equal(expected.RootElement.GetProperty("nodes").EnumerateArray().Select(node => node.GetString()), chart.Nodes.Select(node => node.Id));
        Assert.Equal(expected.RootElement.GetProperty("edges").EnumerateArray().Select(edge => (edge[0].GetString()!, edge[1].GetString()!)),
            chart.Edges.Select(edge => (edge.SourceNodeId, edge.TargetNodeId)));
        Assert.Equal(expected.RootElement.GetProperty("edgeLabels").EnumerateArray().Select(label => label.GetString()), chart.Edges.Select(edge => edge.Label ?? ""));
        Assert.Equal(expected.RootElement.GetProperty("minimumRankSpans").EnumerateArray().Select(span => span.GetInt32()), chart.Edges.Select(edge => edge.MinimumRankSpan));
    }

    [Theory]
    [InlineData(VisualLinkDirection.Forward)]
    [InlineData(VisualLinkDirection.Backward)]
    [InlineData(VisualLinkDirection.Bidirectional)]
    [InlineData(VisualLinkDirection.None)]
    public void NativeRankHintsFollowTheRelationshipDirection(VisualLinkDirection direction) {
        var chart = TopologyChart.Create().WithViewport(600, 300).WithLegend(null)
            .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
            .AddAutoNode("A", "A").AddAutoNode("B", "B")
            .AddEdge("link", "A", "B", direction: direction)
            .WithEdgeLayoutHints("link", minimumRankSpan: 3);
        var prepared = TopologyLayoutEngine.Prepare(chart, options: new TopologyRenderOptions { IncludeLegend = false });
        var first = prepared.Nodes.Single(node => node.Id == (direction == VisualLinkDirection.Backward ? "B" : "A"));
        var second = prepared.Nodes.Single(node => node.Id == (direction == VisualLinkDirection.Backward ? "A" : "B"));
        Assert.True(Rank(second) >= Rank(first) + 3);
        Assert.True(second.X > first.X + first.Width);
        Assert.Equal(("A", "B"), (Assert.Single(prepared.Edges).SourceNodeId, prepared.Edges[0].TargetNodeId));
    }

    [Theory]
    [InlineData("flowchart", "LR", 1, 0)]
    [InlineData("flowchart", "RL", -1, 0)]
    [InlineData("flowchart", "TB", 0, 1)]
    [InlineData("flowchart", "BT", 0, -1)]
    [InlineData("swimlane-beta", "LR", 1, 0)]
    [InlineData("swimlane-beta", "RL", -1, 0)]
    [InlineData("swimlane-beta", "TB", 0, 1)]
    [InlineData("swimlane-beta", "BT", 0, -1)]
    public void ChainedSpansKeepTheirCumulativeSpacingInEveryLayoutDirection(string family, string direction, int horizontal, int vertical) {
        var source = family + " " + direction + "\nsubgraph Delivery\nA ----> B\nB -- a-b ---> C\nend";
        var parsed = new MermaidParser().ParseFlowchart(source);
        Assert.Empty(parsed.Diagnostics);
        var chart = TopologyLayoutEngine.Prepare(parsed.Document!.ToTopologyChart());
        var a = chart.Nodes.Single(node => node.Id == "A");
        var b = chart.Nodes.Single(node => node.Id == "B");
        var c = chart.Nodes.Single(node => node.Id == "C");
        Assert.Equal(3, Rank(b) - Rank(a));
        Assert.Equal(2, Rank(c) - Rank(b));
        Assert.True(horizontal == 0 || (b.X - a.X) * horizontal > a.Width);
        Assert.True(vertical == 0 || (b.Y - a.Y) * vertical > a.Height);
        Assert.True(horizontal == 0 || (c.X - b.X) * horizontal > b.Width);
        Assert.True(vertical == 0 || (c.Y - b.Y) * vertical > b.Height);
        Assert.All(chart.Nodes, node => Assert.Equal(Assert.Single(parsed.Document!.Subgraphs).Id, node.GroupId));
        Assert.All(chart.Nodes, node => {
            Assert.InRange(node.X, 0, chart.Viewport.Width - node.Width);
            Assert.InRange(node.Y, 0, chart.Viewport.Height - node.Height);
        });
        var artifact = MermaidRenderer.Render(source).Artifact!;
        var svg = XDocument.Parse(artifact.ToSvg()).Root!;
        var png = artifact.ToPng();
        Assert.Equal(Math.Ceiling(double.Parse(svg.Attribute("width")!.Value, CultureInfo.InvariantCulture)), ReadPngDimension(png, 16));
        Assert.Equal(Math.Ceiling(double.Parse(svg.Attribute("height")!.Value, CultureInfo.InvariantCulture)), ReadPngDimension(png, 20));
    }

    [Fact]
    public void CyclesStayTogetherWhileTheirOutgoingLinkReservesRanks() {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\nA ----> B\nB --> A\nB ----> C");
        Assert.Empty(parsed.Diagnostics);
        var chart = TopologyLayoutEngine.Prepare(parsed.Document!.ToTopologyChart());
        Assert.Equal(Rank(chart.Nodes.Single(node => node.Id == "A")), Rank(chart.Nodes.Single(node => node.Id == "B")));
        Assert.True(Rank(chart.Nodes.Single(node => node.Id == "C")) >= Rank(chart.Nodes.Single(node => node.Id == "B")) + 3);
    }

    [Fact]
    public void UseCaseStereotypesKeepTheirOwnSpacingPolicy() {
        var parsed = new MermaidParser().ParseFlowchart("usecase-beta\nA ----> B\nB ..> : include C");
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { 1, 2 }, parsed.Document!.ToTopologyChart().Edges.Select(edge => edge.MinimumRankSpan));
    }

    [Fact]
    public void MarkdownAndInterchangePreserveTheAuthoredLinkSpacing() {
        const string source = "flowchart LR\nA ----> B\nB -- a-b ---> C";
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var artifact = Assert.Single(fenced.Artifacts);
        Assert.Equal(new[] { 3, 2 }, Assert.IsType<TopologyChart>(artifact.Model).Edges.Select(edge => edge.MinimumRankSpan));
        var envelope = VisualArtifactInterchangeEnvelope.FromUtf8Json(artifact.ToInterchangeEnvelope().ToUtf8Json());
        Assert.Equal(new[] { 3, 2 }, envelope.Edges.Select(edge => edge.Topology!.MinimumRankSpan));
        Assert.Equal(rendered.Artifact!.ToSvg(), artifact.ToSvg());
        Assert.Equal(rendered.Artifact!.ToPng(), artifact.ToPng());
    }

    [BrowserTheory]
    [InlineData(false, 320, true)]
    [InlineData(false, 960, false)]
    [InlineData(true, 320, true)]
    [InlineData(true, 960, false)]
    public async Task AuthoredSpacingRendersInNativeAndResponsiveStaticHosts(bool dark, int width, bool preserveSize) {
        var result = MermaidRenderer.Render((dark ? "%%{init: {'theme':'dark'}}%%\n" : "")
            + "flowchart LR\nA[Receive] ----> B[Validate]\nB -. accepted ..-> C[Store]");
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!;
        var svg = artifact.ToSvg();
        var png = artifact.ToPng();
        Assert.Contains("data-edge-minimum-rank-span=\"3\"", svg);
        var root = XDocument.Parse(svg).Root!;
        Assert.Equal(Math.Ceiling(double.Parse(root.Attribute("width")!.Value, CultureInfo.InvariantCulture)), ReadPngDimension(png, 16));
        Assert.Equal(Math.Ceiling(double.Parse(root.Attribute("height")!.Value, CultureInfo.InvariantCulture)), ReadPngDimension(png, 20));
        var htmlOptions = new VisualArtifactRenderOptions {
            HtmlSizing = preserveSize ? VisualArtifactHtmlSizing.PreserveSize : VisualArtifactHtmlSizing.FitToWidth
        };
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(htmlOptions), width, 620);
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        if (preserveSize) {
            var region = session.Page.Locator("[data-cfx-html-sizing='preserve-size']");
            Assert.True(await region.EvaluateAsync<bool>("element => element.scrollWidth > element.clientWidth"));
            Assert.True(await session.Page.EvaluateAsync<bool>("() => [...document.querySelectorAll('svg text')].every(element => parseFloat(getComputedStyle(element).fontSize) * Math.abs(element.getScreenCTM().a) >= 8)"));
            await region.FocusAsync();
            await session.Page.Keyboard.PressAsync("ArrowRight");
            await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-cfx-html-sizing=preserve-size]').scrollLeft > 0");
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = Path.Combine(captures, "minimum-lengths-" + (dark ? "dark" : "light") + "-" + width + (preserveSize ? "-preserve" : "-fit"));
        File.WriteAllText(name + ".svg", svg);
        File.WriteAllBytes(name + ".native.png", png);
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = name + ".browser.png", FullPage = true });
    }

    private static int Rank(TopologyNode node) => int.Parse(node.Metadata["layer"], CultureInfo.InvariantCulture);
    private static double ReadPngDimension(byte[] png, int offset) =>
        (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];
}
