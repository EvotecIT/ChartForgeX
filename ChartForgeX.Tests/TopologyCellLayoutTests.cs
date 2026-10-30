using ChartForgeX.Markup;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the terminal-cell projection that text output and ConsoleMonster draw from.</summary>
public sealed class TopologyCellLayoutTests {
    internal static TopologyChart Example(string name) {
        var path = Path.Combine(TestRepository.Root, "ChartForgeX.Examples", "Assets", "Architecture", name + ".md");
        var result = new MarkupArchitectureParser().Parse(File.ReadAllText(path));
        Assert.False(result.HasErrors, string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Code + " " + diagnostic.Message)));
        return result.Document!;
    }

    [Theory]
    [InlineData("ad-replication")]
    [InlineData("tierbridge-file-journey")]
    public void Create_ConsumerExamples_PlacesNodesApartAndRoutesEveryEdgeOrthogonallyBetweenItsNodes(string name) {
        var chart = Example(name);
        var layout = TopologyCellLayout.Create(chart.Prepare(), new TopologyCellLayoutOptions { MaximumColumns = 110 });

        Assert.Equal(chart.Nodes.Count, layout.Nodes.Count);
        Assert.Equal(chart.Groups.Count, layout.Groups.Count);
        Assert.Equal(chart.Edges.Count, layout.Edges.Count);
        for (var i = 0; i < layout.Nodes.Count; i++) {
            for (var j = i + 1; j < layout.Nodes.Count; j++) Assert.False(Overlap(layout.Nodes[i], layout.Nodes[j]), layout.Nodes[i].Id + " overlaps " + layout.Nodes[j].Id);
        }

        foreach (var node in layout.Nodes) Assert.True(node.Width >= node.Label.Length + 4, node.Id + " is too narrow for its label.");
        foreach (var edge in layout.Edges) {
            for (var i = 1; i < edge.Points.Count; i++) {
                Assert.True(edge.Points[i - 1].Column == edge.Points[i].Column || edge.Points[i - 1].Row == edge.Points[i].Row, edge.Id + " has a diagonal segment.");
            }

            Assert.True(Touches(layout.FindNode(edge.SourceNodeId)!, edge.Points[0]), edge.Id + " does not start at its source.");
            Assert.True(Touches(layout.FindNode(edge.TargetNodeId)!, edge.Points[edge.Points.Count - 1]), edge.Id + " does not end at its target.");
        }

        foreach (var group in layout.Groups) {
            foreach (var node in layout.Nodes.Where(node => node.GroupId == group.Id)) {
                Assert.True(node.Column > group.Column && node.Row > group.Row && node.Column + node.Width < group.Column + group.Width && node.Row + node.Height < group.Row + group.Height,
                    node.Id + " is not inside group " + group.Id + ".");
            }
        }
    }

    [Fact]
    public void Create_CompactToFit_DropsSubtitlesAndEdgeLabelsUntilTheLayoutFits() {
        var prepared = Example("ad-replication").Prepare();
        var full = TopologyCellLayout.Create(prepared, new TopologyCellLayoutOptions { MaximumColumns = 80 });
        var compact = TopologyCellLayout.Create(prepared, new TopologyCellLayoutOptions { MaximumColumns = 80, CompactToFit = true });

        Assert.True(full.Columns > 80);
        Assert.True(compact.Columns < full.Columns);
        Assert.All(compact.Nodes, node => Assert.Null(node.Subtitle));
        Assert.Contains(full.Nodes, node => node.Subtitle != null);
    }

    [Fact]
    public void Paint_AdReplication_DrawsBoxesGroupsLabelsArrowsAndLegend() {
        var text = TopologyCellLayout.Create(Example("ad-replication").Prepare(), new TopologyCellLayoutOptions { MaximumColumns = 110 }).Paint().ToText();

        foreach (var label in new[] { "Admin", "ADMIN-PC", "DC01", "DC02", "DC03", "DC04", "PDC emulator", "Site: HQ", "Site: Warsaw", "Site link", "LDAP" }) Assert.Contains(label, text, StringComparison.Ordinal);
        Assert.Contains("▶", text, StringComparison.Ordinal);
        Assert.Contains("╭╌", text, StringComparison.Ordinal);
        Assert.Contains("● Healthy   ▲ Warning", text, StringComparison.Ordinal);
        Assert.DoesNotContain('\t', text);
    }

    [Fact]
    public void Paint_Ascii_UsesOnlyAsciiCharacters() {
        var chart = Example("tierbridge-file-journey");
        chart.Nodes[0].Label = new string('A', 50);
        chart.Nodes[0].Subtitle = new string('B', 50);
        var layout = TopologyCellLayout.Create(chart.Prepare());
        var text = layout.Paint(new TopologyCellPaintOptions { Glyphs = TopologyCellGlyphSet.Ascii }).ToText();
        Assert.Contains("AAA", text, StringComparison.Ordinal);
        Assert.Contains("BBB", text, StringComparison.Ordinal);
        Assert.All(text.Split('\n'), line => Assert.True(line.TrimEnd('\r').Length <= layout.Columns));

        Assert.All(text, ch => Assert.True(ch < 128, "Non-ASCII character U+" + ((int)ch).ToString("X4") + " in ASCII output."));
        Assert.Contains("Quarantine", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Paint_ReversedStep_HighlightsTheEdgeAndPointsTheArrowBack() {
        var chart = TopologyScenarioPlaybackTests.ReplicationChart();
        var layout = TopologyCellLayout.Create(chart.Prepare());
        var step = chart.ResolveScenario("replicate").Steps[3];
        var canvas = layout.Paint(TopologyCellPaintOptions.ForStep(step));

        var edgeCells = Cells(canvas).Where(cell => cell.ElementId == "rpc").ToList();
        Assert.Contains(edgeCells, cell => cell.Role == TopologyCellRole.Edge && cell.Emphasis == TopologyCellEmphasis.Highlighted && cell.Glyph == "━");
        Assert.Contains(edgeCells, cell => cell.Role == TopologyCellRole.EdgeArrow && cell.Glyph == "◀");
        Assert.Contains(Cells(canvas), cell => cell.ElementId == "admin" && cell.Emphasis == TopologyCellEmphasis.Dimmed);
        Assert.Contains(Cells(canvas), cell => cell.ElementId == "dc02" && cell.Role == TopologyCellRole.NodeBorder && cell.Glyph == "┏");
    }

    [Fact]
    public void Paint_LiveValuesStatusAndFocus_ReplaceTheDetailLineAndBorders() {
        var chart = TopologyScenarioPlaybackTests.ReplicationChart();
        var layout = TopologyCellLayout.Create(chart.Prepare(), new TopologyCellLayoutOptions { ReservedValueWidth = 8 });
        var options = new TopologyCellPaintOptions { FocusNodeId = "dc01" };
        options.NodeValues["dc02"] = "98 %";
        options.NodeStatuses["dc02"] = TopologyHealthStatus.Critical;
        var canvas = layout.Paint(options);
        var text = canvas.ToText();

        Assert.Contains("98 %", text, StringComparison.Ordinal);
        Assert.Contains("■", text, StringComparison.Ordinal);
        Assert.Contains("╔", text, StringComparison.Ordinal);
        Assert.Contains("■ Critical", text, StringComparison.Ordinal);
        var node = layout.FindNode("dc01")!;
        Assert.Same(node, layout.NodeAt(node.Column + 1, node.Row + 1));
        Assert.True(layout.Nodes.All(item => item.Height >= 4));
    }

    [Fact]
    public void Paint_WideCharacters_KeepTheirTwoCells() {
        var chart = TopologyChart.Create().WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
            .AddAutoNode("a", "東京 DC").AddAutoNode("b", "Zürich").AddEdge("ab", "a", "b", "同期");
        var canvas = TopologyCellLayout.Create(chart.Prepare()).Paint();

        Assert.Contains("東京 DC", canvas.ToText(), StringComparison.Ordinal);
        Assert.Contains(Cells(canvas), cell => cell.Glyph == "東" && cell.Width == 2);
        Assert.Contains(Cells(canvas), cell => cell.Width == 0);
    }

    private static IEnumerable<TopologyCell> Cells(TopologyCellCanvas canvas) {
        for (var row = 0; row < canvas.Rows; row++) {
            for (var column = 0; column < canvas.Columns; column++) yield return canvas[column, row];
        }
    }

    private static bool Overlap(TopologyCellBox a, TopologyCellBox b) =>
        a.Column < b.Column + b.Width && b.Column < a.Column + a.Width && a.Row < b.Row + b.Height && b.Row < a.Row + a.Height;

    private static bool Touches(TopologyCellNode node, TopologyCellPoint point) => node.Contains(point.Column, point.Row);
}
