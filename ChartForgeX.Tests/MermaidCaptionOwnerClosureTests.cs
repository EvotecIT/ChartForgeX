using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidCaptionOwnerClosureTests {
    [Theory]
    [InlineData(80, 40, false)]
    [InlineData(40, 108, false)]
    [InlineData(80, 108, false)]
    [InlineData(80, 108, true)]
    public async Task ActorPaintRetainsTheCanonicalAttachmentAndCaptionGeometry(int width, int height, bool compact) {
        var chart = TopologyChart.Create().WithId("actor-canonical-geometry").WithViewport(420, 280, 16)
            .AddNode("actor", "API", 80, 70, width: width, height: height)
            .AddNode("target", "UI", 280, 90, width: 80, height: 48)
            .AddEdge("association", "actor", "target");
        var actor = chart.Nodes[0]; actor.Shape = TopologyNodeShape.Actor;
        chart.WithNodeDisplay("target", TopologyNodeDisplayMode.Tile);
        actor.PreserveDisplayModeSize = !compact;
        if (compact) chart.WithNodeDisplay("actor", TopologyNodeDisplayMode.CompactCard);
        foreach (var node in chart.Nodes) node.ShowStatusBadge = false;
        chart.Edges[0].SourcePort = TopologyEdgePort.Right;
        var svg = chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false });
        await Observe(svg, chart.ToPng(new TopologyRenderOptions { IncludeLegend = false }), 420, 280,
            "actor-canonical-" + width + "-" + height + "-" + compact, true);
        var arm = TopologyNodeShapeGeometry.SceneParts(actor)[2].Path.Commands[1];
        var attachment = TopologyNodeShapeGeometry.BoundaryPoint(actor, 400, 100, TopologyEdgePort.Right, 0);
        Assert.Equal(attachment.X, arm.X, 3);
        Assert.Equal(attachment.Y, arm.Y, 3);
        Assert.Equal(actor.Y + 34 * Math.Min(1, Math.Min(width / 48d, height / 76d)), arm.Y, 7);
        var xml = XDocument.Parse(svg);
        var surface = xml.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-node" && (string?)element.Attribute("data-node-id") == "actor")
            .Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-node-surface").ElementAt(2);
        var paintedArm = Assert.Single(ChartMapPathParser.ParseSubpaths((string)surface.Attribute("d")!, 1)).Points;
        var edge = xml.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-line");
        var route = Assert.Single(ChartMapPathParser.ParseSubpaths((string)edge.Attribute("d")!, 1)).Points;
        Assert.Equal(paintedArm[1].Y, route[0].Y, 3);
    }

    [Theory]
    [InlineData("1e2px", 100)]
    [InlineData("+1.8e1px", 18)]
    [InlineData("+.18e2PX", 18)]
    [InlineData("1e0", 1)]
    [InlineData("2.56e2px", 256)]
    public void CssPixelNumbersApplySignedExponentGrammar(string value, double expected) {
        var rendered = MermaidRenderer.Render("%%{init: {'themeVariables':{'fontSize':'" + value + "'}}}%%\nflowchart LR\nA --> B");
        Assert.Empty(rendered.Diagnostics);
        Assert.Equal(expected, rendered.Document!.Configuration.FontSize);
        Assert.Equal(expected.ToString(CultureInfo.InvariantCulture), rendered.Artifact!.Metadata["mermaid.config.fontSize"]);
    }

    [Theory]
    [InlineData("1e3px")]
    [InlineData("-1e1px")]
    [InlineData("1e-2px")]
    [InlineData("1e309px")]
    public void CssPixelNumbersStillEnforceFiniteSupportedBounds(string value) {
        var rendered = MermaidRenderer.Render("%%{init: {'themeVariables':{'fontSize':'" + value + "'}}}%%\nflowchart LR\nA --> B");
        Assert.True(rendered.HasErrors);
        Assert.Null(rendered.Artifact);
        Assert.Equal(MermaidDiagnosticCodes.InvalidConfiguration, Assert.Single(rendered.Diagnostics).Code);
    }

    [Theory]
    [InlineData(false, false, 320)]
    [InlineData(false, true, 960)]
    [InlineData(true, false, 320)]
    [InlineData(true, true, 960)]
    public async Task MaximumFontKeepsLabelFreeEndpointMarkersInsideContent(bool graphite, bool branches, int width) {
        var theme = ChartTheme.Light(); theme.FontFamily = "Arial"; theme.DataLabelFontSize = 256; theme.UseGraphiteLayout = graphite;
        var block = GitGraphBlock.Create().WithSize(width, 460).WithTheme(theme).WithCard(false).WithCommitLabels(false)
            .AddBranch("main").AddCommit("base", "main")
            .AddCommit("pick", "main", new[] { "base" }, GitGraphCommitType.CherryPick, sourceCommitId: "base");
        block.ShowBranchLabels = branches;
        var svg = block.ToSvg(); var png = block.ToPng();
        var circles = XDocument.Parse(svg).Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "git-commit").ToArray();
        Assert.Equal(2, circles.Length);
        Assert.All(circles, circle => {
            var x = (double)circle.Attribute("cx")!; var radius = (double)circle.Attribute("r")! + .75;
            Assert.InRange(x - radius, block.Options.Padding.Left - .002, width);
            Assert.InRange(x + radius, 0, width - block.Options.Padding.Right + .002);
        });
        Assert.True((double)circles[1].Attribute("r")! > 7.5);
        await Observe(svg, png, width, 460, "git-label-free-" + graphite + "-" + branches + "-" + width, false);
    }

    [Theory]
    [InlineData(false, false, 320)]
    [InlineData(false, true, 320)]
    [InlineData(true, false, 960)]
    [InlineData(true, true, 960)]
    public async Task MaximumSourceFontFitsActorAndTileCaptionCells(bool dark, bool tile, int width) {
        var result = MermaidRenderer.Render("%%{init: {'theme':'" + (dark ? "dark" : "default")
            + "','fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\nusecase-beta\ndirection LR\nactor API\nactor UI");
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        chart.Title = ""; chart.Subtitle = ""; chart.LayoutMode = TopologyLayoutMode.Manual;
        chart.WithViewport(width, 360, 16);
        for (var i = 0; i < chart.Nodes.Count; i++) {
            var node = chart.Nodes[i];
            node.X = 32 + i * (width - 144); node.Y = 90; node.Width = 80; node.Height = tile ? 48 : 108;
            if (tile) { node.Shape = null; node.DisplayMode = TopologyNodeDisplayMode.Tile; node.Subtitle = "Ready"; }
        }
        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeTitle = false, IncludeTileSubtitles = true, FitContentToViewport = true };
        var svg = chart.ToSvg(options); var png = chart.ToPng(options);
        var labels = XDocument.Parse(svg).Descendants().Where(e => e.Name.LocalName == "text" && e.AncestorsAndSelf().Any(a => (string?)a.Attribute("data-cfx-role") == "topology-node-label")).ToArray();
        Assert.Equal(new[] { "API", "UI" }, labels.Select(e => e.Value));
        Assert.All(labels, e => {
            var style = new TextStyle { Font = new FontSpec { Family = "Arial", Weight = tile ? 700 : 600 }, FontSize = (double)e.Attribute("font-size")!, LineHeight = 1 };
            var metrics = TextLayoutEngine.Measure(e.Value, style);
            Assert.InRange(metrics.Width, .001, 80.002);
            Assert.InRange(TextLayoutEngine.Measure("Ag", style).LineHeight, .001, 14.002);
        });
        if (tile) Assert.Equal(2, XDocument.Parse(svg).Descendants().Count(e => e.Name.LocalName == "text" && e.AncestorsAndSelf().Any(a => (string?)a.Attribute("data-cfx-role") == "topology-subtitle-chip-text")));
        await Observe(svg, png, width, 360, "topology-captions-" + dark + "-" + tile + "-" + width, true);
    }

    private static async Task Observe(string svg, byte[] png, int width, int height, string name, bool captions) {
        Assert.NotEmpty(png);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) { Directory.CreateDirectory(captures); File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), png); }
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", width, height);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => [...document.querySelectorAll('svg text, [data-cfx-role=git-commit]')].every(t=>{const b=t.getBoundingClientRect(); return b.width>0&&b.left>=-.01&&b.right<=innerWidth+.01&&b.top>=-.01&&b.bottom<=innerHeight+.01;})"));
        if (captions) Assert.True(await session.Page.EvaluateAsync<bool>("() => {const boxes=[...document.querySelectorAll('[data-cfx-role=topology-node-label], [data-cfx-role=topology-subtitle-chip-text]')].map(t=>t.getBoundingClientRect());return boxes.every((a,i)=>boxes.slice(i+1).every(b=>Math.min(a.right,b.right)-Math.max(a.left,b.left)<=.1||Math.min(a.bottom,b.bottom)-Math.max(a.top,b.top)<=.1));}"));
        if (captions) Assert.True(await session.Page.EvaluateAsync<bool>("() => [...document.querySelectorAll('[data-cfx-role=topology-node]')].every(n=>{const caption=n.querySelector('[data-cfx-role=topology-node-label]'), surfaces=[...n.querySelectorAll('[data-cfx-role=topology-node-surface]')];return !caption||surfaces.every(s=>s.getBoundingClientRect().bottom+2<=caption.getBoundingClientRect().top);})"), "Node captions must sit below their painted surfaces.");
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        if (!string.IsNullOrWhiteSpace(captures)) await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png"), FullPage = true });
    }
}
