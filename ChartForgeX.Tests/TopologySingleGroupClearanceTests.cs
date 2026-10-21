using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Automatic group shells reserve their headers and padding around unrelated nodes.</summary>
public sealed class TopologySingleGroupClearanceTests {
    [Theory]
    [InlineData(TopologyLayoutDirection.LeftToRight, 560)]
    [InlineData(TopologyLayoutDirection.RightToLeft, 560)]
    [InlineData(TopologyLayoutDirection.TopToBottom, 560)]
    [InlineData(TopologyLayoutDirection.BottomToTop, 560)]
    [InlineData(TopologyLayoutDirection.LeftToRight, 240)]
    [InlineData(TopologyLayoutDirection.RightToLeft, 240)]
    [InlineData(TopologyLayoutDirection.TopToBottom, 240)]
    [InlineData(TopologyLayoutDirection.BottomToTop, 240)]
    public void PublicNativePreparationKeepsTheWholeShellAwayFromForeignRankPeers(TopologyLayoutDirection direction, int viewportHeight) {
        var chart = TopologyChart.Create().WithViewport(960, viewportHeight, 28).WithLayout(TopologyLayoutMode.Layered, direction)
            .AddAutoGroup("Services", "Services")
            .AddAutoNode("User", "User", groupId: "Services", width: 180, height: 138)
            .AddAutoNode("Admin", "Admin", groupId: "Services", width: 180, height: 82)
            .AddAutoNode("Session", "Session", width: 180, height: 82)
            .AddEdge("inherits", "Admin", "User").WithEdgeLayoutHints("inherits", minimumRankSpan: 2)
            .AddEdge("opens", "Session", "User", "opens").WithEdgeLayoutHints("opens", minimumRankSpan: 2);
        foreach (var node in chart.Nodes) { node.Shape = TopologyNodeShape.Rectangle; node.PreserveDisplayModeSize = true; }
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false });
        AssertClearance(prepared.ToInterchangeEnvelope(), direction);
        var report = prepared.Analyze();
        Assert.Empty(report.Collisions);
        Assert.All(report.Edges, edge => { Assert.True(edge.SourceAttached); Assert.True(edge.TargetAttached); });
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData("LR", false, TopologyLayoutDirection.LeftToRight)]
    [InlineData("LR", true, TopologyLayoutDirection.LeftToRight)]
    [InlineData("RL", false, TopologyLayoutDirection.RightToLeft)]
    [InlineData("RL", true, TopologyLayoutDirection.RightToLeft)]
    [InlineData("TB", false, TopologyLayoutDirection.TopToBottom)]
    [InlineData("TB", true, TopologyLayoutDirection.TopToBottom)]
    [InlineData("BT", false, TopologyLayoutDirection.BottomToTop)]
    [InlineData("BT", true, TopologyLayoutDirection.BottomToTop)]
    public void NamespaceOutsiderRemainsOutsideItsShellThroughDirectAndFencedExports(string direction, bool dark, TopologyLayoutDirection nativeDirection) {
        var source = (dark ? "%%{init: {'theme':'dark'}}%%\n" : "") + "classDiagram\n"
            + (direction == "LR" ? "" : "direction " + direction + "\n") + Body;
        var direct = MermaidRenderer.Render(source);
        Assert.Empty(direct.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(direct.Artifact);
        var facts = artifact.ToInterchangeEnvelope();
        AssertClearance(facts, nativeDirection);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var fenceArtifact = Assert.Single(fenced.Artifacts);
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        Assert.Equal(svg, fenceArtifact.ToSvg());
        Assert.Equal(png, fenceArtifact.ToPng());
        var raster = ChartForgeX.Raster.PngReader.Decode(png);
        Assert.Equal((int)Math.Ceiling(facts.Width!.Value), raster.Width);
        Assert.Equal((int)Math.Ceiling(facts.Height!.Value), raster.Height);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = Path.Combine(captures, "namespace-outsider-" + direction + (dark ? "-dark" : "-light"));
        File.WriteAllText(name + ".svg", svg);
        File.WriteAllBytes(name + ".native.png", png);
        File.WriteAllText(name + ".facts.json", facts.ToJson());
        File.WriteAllText(name + ".mmd", source);
    }

    [Fact]
    public void AuthoredManualGroupAndNodeCoordinatesRemainIntact() {
        var chart = TopologyChart.Create().WithViewport(720, 420, 24).WithLayout(TopologyLayoutMode.Manual)
            .AddGroup("fixed", "Manual", 100, 80, 420, 260)
            .AddNode("member", "Member", 140, 170, groupId: "fixed")
            .AddNode("outside", "Outside", 580, 120, width: 80);
        var facts = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false }).ToInterchangeEnvelope();
        var group = Assert.Single(facts.Groups);
        Assert.Equal((100d, 80d, 420d, 260d), (group.X, group.Y, group.Width, group.Height));
        Assert.Equal(new[] { (140d, 170d), (580d, 120d) }, facts.Nodes.Select(node => (node.X!.Value, node.Y!.Value)));
    }

    private static void AssertClearance(VisualArtifactInterchangeEnvelope facts, TopologyLayoutDirection direction) {
        var group = Assert.Single(facts.Groups);
        var shell = Bounds(group.X, group.Y, group.Width, group.Height);
        var user = facts.Nodes.Single(node => node.Id == "User");
        var admin = facts.Nodes.Single(node => node.Id == "Admin");
        var session = facts.Nodes.Single(node => node.Id == "Session");
        Assert.Null(session.GroupId);
        foreach (var member in new[] { user, admin }) {
            var bounds = Bounds(member.X, member.Y, member.Width, member.Height);
            Assert.True(bounds.Left >= shell.Left && bounds.Top >= shell.Top && bounds.Right <= shell.Right && bounds.Bottom <= shell.Bottom);
        }
        var outside = Bounds(session.X, session.Y, session.Width, session.Height);
        Assert.True(outside.Right <= shell.Left || outside.Left >= shell.Right || outside.Bottom <= shell.Top || outside.Top >= shell.Bottom,
            $"Session {outside} must remain outside the complete Services shell {shell}.");
        foreach (var predecessor in new[] { admin, session }) {
            Assert.True(direction switch {
                TopologyLayoutDirection.LeftToRight => predecessor.X + predecessor.Width < user.X,
                TopologyLayoutDirection.RightToLeft => user.X + user.Width < predecessor.X,
                TopologyLayoutDirection.TopToBottom => predecessor.Y + predecessor.Height < user.Y,
                _ => user.Y + user.Height < predecessor.Y
            }, "Packing must preserve relationship rank order.");
        }
        static ChartRect Bounds(double? x, double? y, double? width, double? height) => new(x!.Value, y!.Value, width!.Value, height!.Value);
    }

    private const string Body = "namespace Services {\nclass User {\n+string name\n+save() void\n}\nclass Admin\n}\n<<interface>> User\nUser <|-- Admin\nUser \"1\" o-- \"0..*\" Session : opens\n";
}
