using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyActorSubtitleTests {
    [Theory]
    [InlineData(12, false)] [InlineData(18, false)] [InlineData(12, true)] [InlineData(18, true)]
    public void ActorSubtitleUsesTheSameCapturedTextForPaintFootprintAndRouteObstacles(int size, bool structured) {
        var chart = Fixture(); var actor = chart.Nodes[0]; actor.Subtitle = "«primary actor»";
        actor.TextStyle = new TextStyleOverride { FontSize = size, FontFamily = BrowserProofFonts.Family };
        if (structured) actor.LabelContent = TextContent.FromRuns(new[] {
            new TextContentRun("Release", TextInlineEmphasis.Bold), new TextContentRun(" coordinator")
        });
        var prepared = chart.Prepare(); var rows = ActorRows(prepared.Visual.Scene).ToArray(); Assert.NotEmpty(rows);
        var words = string.Join(" ", rows.SelectMany(row => row.Text.Lines.Select(line => line.Text)));
        Assert.Contains("Release", words); Assert.Contains("coordinator", words); Assert.Contains("primary actor", words);
        if (structured) {
            Assert.Contains(rows, row => row.Text.Lines.Any(line => line.Text.Contains("Release", StringComparison.Ordinal)) && row.Text.Style.Font.Weight >= 700);
            Assert.Contains(rows, row => row.Text.Lines.Any(line => line.Text.Contains("coordinator", StringComparison.Ordinal)) && row.Text.Style.Font.Weight == 400);
        }
        var envelope = prepared.ToInterchangeEnvelope(); var edge = Assert.Single(envelope.Edges);
        var frame = new ChartRect(0, 0, prepared.Width, prepared.Height);
        foreach (var row in rows) {
            var box = CartesianReversalLabelTests.Bounds(row); Assert.True(LabelPlacementService.Contains(frame, box));
            Assert.False(TopologyAlignmentReviewCausalTests.CrossesBody(edge,
                new VisualArtifactInterchangeNode { Id = "actual-actor-caption", X = box.X, Y = box.Y, Width = box.Width, Height = box.Height }));
        }
        Assert.Equal("«primary actor»", envelope.Nodes.Single(node => node.Id == "actor").Subtitle);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); actor.Subtitle = "changed"; actor.Label = "changed"; actor.TextStyle.FontSize = 60;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void NoSubtitleActorsKeepTheirOrdinaryNativePathAndEmptySubtitleDoesNotChangeItsBytes() {
        var chart = Fixture(); var ordinary = chart.Prepare();
        Assert.DoesNotContain(ordinary.Visual.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Id == "actor-subtitle");
        chart.Nodes[0].Subtitle = ""; var empty = chart.Prepare();
        Assert.Equal(ordinary.ToSvg(), empty.ToSvg()); Assert.Equal(ordinary.ToPng(), empty.ToPng());
        Assert.Equal(ordinary.ToInterchangeEnvelope().Nodes.Select(node => (node.X, node.Y, node.Width, node.Height)),
            empty.ToInterchangeEnvelope().Nodes.Select(node => (node.X, node.Y, node.Width, node.Height)));
    }

    private static IEnumerable<VisualSceneText> ActorRows(VisualScene scene) {
        var groups = new Stack<VisualSceneGroup>();
        foreach (var node in scene.Nodes) {
            if (node is VisualSceneGroup group) groups.Push(group);
            else if (node is VisualSceneEndGroup) groups.Pop();
            else if (node is VisualSceneText text && groups.Any(group => group.Id is "actor-label" or "actor-subtitle")) yield return text;
        }
    }
    private static TopologyChart Fixture() {
        var chart = TopologyChart.Create().WithViewport(700, 520, 24).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family))
            .WithLayout(TopologyLayoutMode.Manual).AddNode("actor", "Release coordinator", 220, 50, width: 180, height: 110)
            .AddNode("work", "Publish", 220, 350, width: 180, height: 80)
            .AddEdge("publish", "actor", "work", routing: TopologyEdgeRouting.Straight);
        chart.Nodes[0].Shape = TopologyNodeShape.Actor;
        foreach (var node in chart.Nodes) { node.ShowStatusBadge = false; node.DisplayMode = TopologyNodeDisplayMode.Card; node.PreserveDisplayModeSize = true; }
        chart.DefaultRenderOptions = new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false,
            AllowMultilineNodeLabels = true, WrapNodeLabels = true, MaxNodeLabelLines = 8, MaxNodeSubtitleLines = 8,
            TextMeasurementMode = TextMeasurementMode.InstalledFonts };
        return chart;
    }
}
