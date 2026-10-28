using System.Text.Json;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyExternalCaptionRoleControlsTests {
    [Theory]
    [InlineData(TopologyNodeDisplayMode.Icon, 12d)] [InlineData(TopologyNodeDisplayMode.Icon, 14d)]
    [InlineData(TopologyNodeDisplayMode.Tile, 12d)] [InlineData(TopologyNodeDisplayMode.Tile, 14d)]
    public void NullNewRolePreservesActualBaseStyleRatiosAndLegacyFittingCells(TopologyNodeDisplayMode mode, double baseSize) {
        var chart = TopologyChart.Create().WithId("external-caption-role-control").WithViewport(640, 400, 24).WithLayout(TopologyLayoutMode.Manual)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).AddNode("node", "Literal caption", 200, 100, width: 180, height: 90);
        var node = chart.Nodes[0]; node.DisplayMode = mode; node.Shape = TopologyNodeShape.Rectangle; node.PreserveDisplayModeSize = true;
        node.IconShape = TopologyIconShape.Router; node.ShowStatusBadge = false; node.TextStyle = new TextStyleOverride { FontFamily = BrowserProofFonts.Family, FontSize = baseSize };
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false, FitContentToViewport = false, IncludeIconLabels = true });
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); var json = prepared.ToInterchangeEnvelope().ToJson();
        var labels = prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().Where(item => item.Role == "topology-node-label").ToArray();
        var name = "native-caption-role-control-" + mode + "-" + baseSize;
        var output = Environment.GetEnvironmentVariable("CFX_EXTERNAL_UNIT_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); var prefix = Path.Combine(output, name);
            File.WriteAllText(prefix + ".svg", svg); File.WriteAllBytes(prefix + ".native.png", png); File.WriteAllText(prefix + ".json", json);
            File.WriteAllText(prefix + ".font-facts.json", JsonSerializer.Serialize(labels.Select(item => new { item.Text.Size, item.Text.Metrics.LineHeight, item.Text.Metrics.Width })));
        }
        var caption = Assert.Single(labels);
        if (mode == TopologyNodeDisplayMode.Icon) Assert.Equal(10.5 * baseSize / 11, caption.Text.Size, 9);
        else {
            var ordinaryLine = new TextMeasurementContext(BrowserProofFonts.Family).MeasureLineHeight(11, true);
            Assert.InRange(caption.Text.Size, double.Epsilon, baseSize);
            Assert.True(caption.Text.Metrics.LineHeight <= Math.Max(14, ordinaryLine) + .00001);
        }
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }
}
