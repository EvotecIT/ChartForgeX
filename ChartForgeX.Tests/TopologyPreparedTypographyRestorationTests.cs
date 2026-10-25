using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyPreparedTypographyRestorationTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ManualImportRetainsFittedBoundsBeforeOptionalUniformOutputFitting(bool fit) {
        var saved = Source().Prepare().ToInterchangeEnvelope();
        var imported = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
        var authored = imported.Nodes.Select(node => (node.X, node.Y, node.Width, node.Height)).ToArray();
        var options = new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = fit };
        var after = fit
            ? imported.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(420, 310), 28),
                frame: new VisualFrame(showLegend: false)), options).SemanticInterchange!
            : imported.Prepare(options).ToInterchangeEnvelope();
        if (!fit) {
            Assert.Equal(saved.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)),
                after.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)));
        } else {
            var scale = after.Nodes[0].Width!.Value / saved.Nodes[0].Width!.Value;
            Assert.InRange(scale, .001, .999);
            for (var index = 0; index < saved.Nodes.Count; index++) {
                var before = saved.Nodes[index]; var actual = after.Nodes[index];
                Assert.Equal(before.Width!.Value * scale, actual.Width!.Value, 6);
                Assert.Equal(before.Height!.Value * scale, actual.Height!.Value, 6);
                Assert.Equal((before.X!.Value - saved.Nodes[0].X!.Value) * scale,
                    actual.X!.Value - after.Nodes[0].X!.Value, 6);
                Assert.Equal((before.Y!.Value - saved.Nodes[0].Y!.Value) * scale,
                    actual.Y!.Value - after.Nodes[0].Y!.Value, 6);
            }
            Assert.Equal(420, after.Width); Assert.Equal(310, after.Height);
        }
        Assert.Equal(authored, imported.Nodes.Select(node => (node.X, node.Y, node.Width, node.Height)));
        Assert.All(imported.Nodes, node => {
            Assert.True(node.FitLabelToContent); Assert.Equal(160, node.MaximumTextWidth);
            Assert.Equal(10, node.ContentPadding); Assert.Equal(18, node.TextStyle!.FontSize);
        });
    }

    [Fact]
    public void SelectingFreshAutomaticLayoutStillFitsAuthoredTypographyWithCurrentWrapOptions() {
        var saved = Source().Prepare().ToInterchangeEnvelope();
        var imported = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
        imported.LayoutMode = TopologyLayoutMode.Layered;
        foreach (var node in imported.Nodes) {
            node.X = 0; node.Y = 0; node.HasPositionOverride = false;
            node.Width = 40; node.Height = 24;
            node.Label = "Fresh native layout keeps all source words in the edited caption " + node.Id;
            node.TextStyle!.FontSize = 36;
        }
        var authored = imported.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height,
            node.Label, node.TextStyle!.FontSize, node.MaximumTextWidth, node.ContentPadding)).ToArray();
        var prepared = imported.Prepare(new TopologyRenderOptions {
            IncludeLegend = false, WrapNodeLabels = true, MaxNodeLabelLines = 32, MaxNodeSubtitleLines = 32
        });
        var after = prepared.ToInterchangeEnvelope();
        Assert.Equal(TopologyLayoutMode.Layered, after.Topology!.LayoutMode);
        Assert.All(after.Nodes, node => {
            Assert.True(node.Width!.Value > 40 || node.Height!.Value > 24);
            Assert.True(node.X!.Value != 0 || node.Y!.Value != 0);
            Assert.True(node.Topology!.FitLabelToContent); Assert.Equal(160, node.Topology.MaximumTextWidth);
            Assert.Equal(10, node.Topology.ContentPadding); Assert.Equal(36, node.Topology.TextStyle!.FontSize);
            var rows = prepared.Visual.Scene.Nodes.OfType<VisualSceneText>()
                .Where(text => text.Role == "topology-node-label" && text.Id == node.Id + "-label").ToArray();
            Assert.True(rows.Length > 1);
            Assert.Equal(node.Label, string.Join(" ", rows.SelectMany(row => row.Text.Lines).Select(line => line.Text)));
            Assert.All(rows, row => {
                Assert.True(row.Text.Size >= 36 - .001); Assert.True(row.Text.Metrics.Width <= 160.001);
            });
        });
        Assert.Equal(authored, imported.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height,
            node.Label, node.TextStyle!.FontSize, node.MaximumTextWidth, node.ContentPadding)));
    }

    private static TopologyChart Source() {
        var chart = TopologyChart.Create().WithViewport(1100, 700, 28).WithLegend(null)
            .WithLayout(TopologyLayoutMode.GroupGrid)
            .AddAutoNode("first", "Publication preparation preserves every complete source word", width: 200, height: 76)
            .AddAutoNode("second", "Delivery coordination retains the complete measured caption", width: 200, height: 76);
        foreach (var node in chart.Nodes) {
            node.Shape = TopologyNodeShape.Rounded; node.PreserveDisplayModeSize = true;
            node.FitLabelToContent = true; node.MaximumTextWidth = 160; node.ContentPadding = 10;
            node.ShowStatusBadge = false;
            node.TextStyle = new TextStyleOverride { FontFamily = BrowserProofFonts.Family, FontSize = 18 };
        }
        return chart.WithRenderOptions(new TopologyRenderOptions {
            IncludeLegend = false, WrapNodeLabels = true, MaxNodeLabelLines = 32, MaxNodeSubtitleLines = 32
        });
    }
}
