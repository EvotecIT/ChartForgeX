using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Grid panels reserve the whole card grid before neighboring panels are positioned.</summary>
public sealed partial class TopologyDenseGridContainmentTests {
    public static IEnumerable<object[]> NativeCases() {
        foreach (var direction in new[] { TopologyLayoutDirection.LeftToRight, TopologyLayoutDirection.RightToLeft,
            TopologyLayoutDirection.TopToBottom, TopologyLayoutDirection.BottomToTop }) {
            yield return new object[] { direction, 2, 0, 0 };
            yield return new object[] { direction, 5, 0, 0 };
            yield return new object[] { direction, 2, 190, 0 };
        }
        // Five groups create two panel rows in these directions. The declared width
        // comfortably fits three cards, isolating height expansion after row packing.
        yield return new object[] { TopologyLayoutDirection.TopToBottom, 5, 500, 170 };
        yield return new object[] { TopologyLayoutDirection.BottomToTop, 5, 500, 170 };
    }

    [Theory]
    [MemberData(nameof(NativeCases))]
    public void PublicPreparationPreservesCardsInsideDisjointAutomaticGridPanels(TopologyLayoutDirection direction,
        int firstGroupCount, int declaredWidth, int declaredHeight) {
        var chart = NativeGrid(direction, firstGroupCount, declaredWidth, declaredHeight);
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false });
        var facts = prepared.ToInterchangeEnvelope();
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, $"native-grid-{direction}-{firstGroupCount}-{declaredWidth}-{declaredHeight}.facts.json"), facts.ToJson());
        }
        AssertPanels(facts, 132);
        Assert.Equal(5, facts.Groups.Count);
        if (declaredHeight > 0) Assert.True(facts.Groups.Select(group => group.Y).Distinct().Count() > 1);
        Assert.NotEmpty(prepared.ToSvg());
    }

    [Theory]
    [InlineData("kanban", false, 3, 180)]
    [InlineData("kanban", true, 3, 180)]
    [InlineData("eventmodeling", false, 5, 144)]
    [InlineData("eventmodeling", true, 5, 144)]
    public void MaintainedBoardsKeepColumnsDisjointInDirectAndFencedSvgAndPng(string family, bool dark,
        int expectedGroups, int expectedCardWidth) {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "Website", "static", "site", "atlas", family + ".mmd"));
        var topology = new MermaidTopologyRenderOptions { Width = family == "kanban" ? 760 : 1142, Height = 690 };
        var options = new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light },
            Kanban = topology, EventModeling = topology
        };
        var direct = MermaidRenderer.Render(source, options);
        Assert.Empty(direct.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(direct.Artifact);
        var facts = artifact.ToInterchangeEnvelope();
        Assert.Equal(expectedGroups, facts.Groups.Count);
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var fenceArtifact = Assert.Single(fenced.Artifacts);
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        Assert.Equal(svg, fenceArtifact.ToSvg());
        Assert.Equal(png, fenceArtifact.ToPng());
        var pixels = RasterImageDecoder.Decode(png);
        var size = artifact.MeasureRasterSize();
        Assert.Equal((int)size.Width, pixels.Width);
        Assert.Equal((int)size.Height, pixels.Height);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            var name = Path.Combine(output, "dense-grid-" + family + (dark ? "-dark" : "-light"));
            File.WriteAllText(name + ".mmd", source);
            File.WriteAllText(name + ".svg", svg);
            File.WriteAllBytes(name + ".native.png", png);
            File.WriteAllText(name + ".facts.json", facts.ToJson());
        }
        AssertPanels(facts, expectedCardWidth, allowMeasuredCardGrowth: family == "eventmodeling");
    }

    [Fact]
    public void ExplicitGroupAnchorsRemainAuthored() {
        var chart = NativeGrid(TopologyLayoutDirection.LeftToRight, 2, 0, 0);
        chart.Groups[0].X = 80; chart.Groups[0].Y = 120;
        chart.Groups[0].Width = 500; chart.Groups[0].Height = 170;
        var fixedGroup = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false })
            .ToInterchangeEnvelope().Groups.Single(group => group.Id == "g0");
        Assert.Equal((80d, 120d, 500d, 170d), (fixedGroup.X, fixedGroup.Y, fixedGroup.Width, fixedGroup.Height));
    }

    private static TopologyChart NativeGrid(TopologyLayoutDirection direction, int firstGroupCount, int width, int height) {
        var chart = TopologyChart.Create().WithViewport(760, 570, 28).WithLayout(TopologyLayoutMode.DenseGrouped, direction);
        for (var groupIndex = 0; groupIndex < 5; groupIndex++) {
            var id = "g" + groupIndex;
            chart.AddAutoGroup(id, "Column " + groupIndex).WithGroupLayout(id, TopologyGroupLayoutPolicy.Grid);
            chart.Groups[groupIndex].Width = width; chart.Groups[groupIndex].Height = height;
            for (var nodeIndex = 0; nodeIndex < (groupIndex == 0 ? firstGroupCount : 1); nodeIndex++)
                chart.AddAutoNode(id + "-n" + nodeIndex, "Card " + nodeIndex, groupId: id, width: 132, height: 58);
        }
        return chart;
    }

    private static void AssertPanels(VisualArtifactInterchangeEnvelope facts, double expectedCardWidth, bool allowMeasuredCardGrowth = false) {
        var groups = facts.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        foreach (var node in facts.Nodes) {
            // Event Modeling now lays out complete typed data cards through measured swimlanes.
            // Its authored 144px width is a minimum; native grids and Kanban keep exact-width controls.
            if (allowMeasuredCardGrowth) Assert.True(node.Width!.Value >= expectedCardWidth);
            else Assert.Equal(expectedCardWidth, node.Width);
            var shell = groups[node.GroupId!];
            var card = Bounds(node.X, node.Y, node.Width, node.Height);
            var panel = Bounds(shell.X, shell.Y, shell.Width, shell.Height);
            Assert.True(card.Left >= panel.Left && card.Top >= panel.Top && card.Right <= panel.Right && card.Bottom <= panel.Bottom,
                $"Card {node.Id} {Describe(card)} must remain inside panel {shell.Id} {Describe(panel)}.");
        }
        var panels = facts.Groups.ToArray();
        for (var first = 0; first < panels.Length; first++) {
            for (var second = first + 1; second < panels.Length; second++) {
                var a = panels[first]; var b = panels[second];
                var left = Bounds(a.X, a.Y, a.Width, a.Height); var right = Bounds(b.X, b.Y, b.Width, b.Height);
                Assert.True(left.Right <= right.Left || right.Right <= left.Left || left.Bottom <= right.Top || right.Bottom <= left.Top,
                    $"Automatic grid panels {a.Id} {Describe(left)} and {b.Id} {Describe(right)} must remain disjoint.");
            }
        }
        static ChartRect Bounds(double? x, double? y, double? width, double? height) => new(x!.Value, y!.Value, width!.Value, height!.Value);
        static string Describe(ChartRect bounds) => $"({bounds.X}, {bounds.Y}, {bounds.Width}, {bounds.Height})";
    }
}
