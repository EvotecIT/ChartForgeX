using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyStyledTableFontSnapshotCausalTests {
    [Theory]
    [InlineData(8d)] [InlineData(18d)]
    public void AuthoredTableFontAppliesOnceAcrossFittedSnapshotLoadSecondPrepareAndFreshViewport(double authored) {
        var chart = Source(authored); var original = chart.Prepare().Visual;
        var fitted = chart.Prepare(Context(390, 420), new TopologyRenderOptions { FitContentToViewport = true }); var saved = fitted.ToArtifact().ToInterchangeEnvelope();
        Capture(original, "table-font-" + authored + "-original"); Capture(fitted, "table-font-" + authored + "-fitted");
        var scale = saved.Nodes[0].Width!.Value / original.ToArtifact().ToInterchangeEnvelope().Nodes[0].Width!.Value;
        Assert.InRange(scale, .001, .999); Assert.Equal(authored, chart.Nodes[0].TextStyle!.FontSize);
        Assert.Equal(authored * scale, saved.Nodes[0].Topology!.TextStyle!.FontSize!.Value, 8);
        Assert.Equal(scale, saved.Nodes[0].Topology!.TableContent!.PresentationScale, 8);
        var loaded = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
        var restored = loaded.Prepare().Visual; var again = loaded.Prepare().Visual;
        Capture(restored, "table-font-" + authored + "-loaded"); Capture(again, "table-font-" + authored + "-second-prepare");
        Assert.Equal(TableRows(fitted), TableRows(restored)); Assert.Equal(TableRows(restored), TableRows(again));
        Assert.Equal(Header(fitted), Header(restored), 8); Assert.Equal(Header(restored), Header(again), 8);
        Assert.Equal(4, restored.Scene.Regions.Count(region => region.Role == "table-cell"));
        Assert.Equal("0", loaded.Nodes[0].TableContent!.Rows[0].Cells[1].DisplayText); Assert.Equal("Number", loaded.Nodes[0].TableContent!.Rows[0].Cells[1].Metadata["raw.type"]);
        // A new output frame transforms the already captured graph; it never reapplies its saved table font scale.
        var resized = loaded.Prepare(Context(300, 340), new TopologyRenderOptions { FitContentToViewport = true }); var resizedFacts = resized.ToArtifact().ToInterchangeEnvelope();
        Capture(resized, "table-font-" + authored + "-fresh-viewport");
        var secondScale = resizedFacts.Nodes[0].Width!.Value / saved.Nodes[0].Width!.Value;
        var body = resized.Scene.Nodes.OfType<VisualSceneText>().Single(text => text.Role == "visual-text" && text.Text.Lines.Any(line => line.Text == "0"));
        Assert.Equal((11.5 / 12) * authored * scale * secondScale, body.Text.Size, 8);
        Assert.Equal(authored, chart.Nodes[0].TextStyle!.FontSize); Assert.NotEmpty(resized.ToPng());
    }
    [Fact]
    public void UnsetTableFontKeepsTheCapturedMinimumHeaderAndDefaultRoleOutput() {
        var chart = Source(null); var prepared = chart.Prepare(Context(390, 420), new TopologyRenderOptions { FitContentToViewport = true });
        var saved = VisualArtifactInterchangeEnvelope.FromJson(prepared.ToArtifact().ToInterchangeEnvelope().ToJson());
        Assert.Null(saved.Nodes[0].Topology!.TextStyle);
        var loaded = saved.ToTopologyChart(); var next = loaded.Prepare().Visual;
        Capture(prepared, "table-font-default-fitted"); Capture(next, "table-font-default-loaded");
        Assert.Equal(TableRows(prepared), TableRows(next)); Assert.Equal(Header(prepared), Header(next), 8);
        Assert.Equal(4, next.Scene.Regions.Count(region => region.Role == "table-cell")); Assert.NotEmpty(next.ToPng());
    }
    private static VisualRenderContext Context(int width, int height) => new(new VisualLayoutOptions(new VisualSize(width, height), 16),
        VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family));
    private static TopologyChart Source(double? font) {
        var chart = TopologyChart.Create().WithViewport(1000, 700, 24).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family))
            .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
            .AddAutoNode("facts", "Facts", width: 300, height: 240).AddAutoNode("next", "Continue", width: 260, height: 100)
            .AddEdge("next-step", "facts", "next");
        foreach (var node in chart.Nodes) { node.Kind = TopologyNodeKind.Generic; node.Shape = TopologyNodeShape.Rectangle; node.ShowStatusBadge = false; node.PreserveDisplayModeSize = true; }
        chart.Nodes[0].TextStyle = font.HasValue ? new TextStyleOverride { FontSize = font, FontFamily = BrowserProofFonts.Family } : null;
        chart.Nodes[0].FitLabelToContent = true;
        var table = TableArtifact.Create("readings").WithTitle("Captured readings").WithSubtitle("Reference values")
            .AddColumn("field", "Property").AddColumn("value", "Value").AddRow("/count", "count", "0").AddRow("/active", "active", "false");
        table.Rows[0].Cells[1].Metadata["raw.type"] = "Number"; table.Rows[1].Cells[1].Metadata["raw.type"] = "Boolean"; chart.Nodes[0].TableContent = table;
        return chart;
    }
    private static void Capture(PreparedVisual prepared, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), prepared.ToPng());
        File.WriteAllText(Path.Combine(output, name + ".json"), prepared.ToArtifact().ToInterchangeEnvelope().ToJson());
    }
    private static double Header(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Single(rect => rect.Role == "table-header").Bounds.Height;
    private static (string Role, string Text, double Size)[] TableRows(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>()
        .Where(text => text.Role is "visual-text" or "table-title" or "table-subtitle")
        .Select(text => (text.Role!, string.Join("\n", text.Text.Lines.Select(line => line.Text)), Math.Round(text.Text.Size, 7))).ToArray();
}
