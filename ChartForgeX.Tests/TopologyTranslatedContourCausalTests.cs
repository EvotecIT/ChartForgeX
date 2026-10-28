using System.Text.Json;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyTranslatedContourCausalTests {
    [Theory]
    [InlineData(TopologyNodeShape.Rounded, false)] [InlineData(TopologyNodeShape.Rounded, true)]
    [InlineData(TopologyNodeShape.Rectangle, false)] [InlineData(TopologyNodeShape.Rectangle, true)]
    public void FrameTranslationRetainsTheActualNativeContourThroughPortableReplay(TopologyNodeShape shape, bool heading) {
        var chart = Source(shape, heading);
        var before = chart.Prepare(); Capture(shape + "-" + heading + "-direct-before-wire", before.Visual);
        var saved = before.ToInterchangeEnvelope(); var json = saved.ToJson();
        var restored = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart(); var after = restored.Prepare();
        Capture(shape + "-" + heading + "-restored", after.Visual);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output)
            File.WriteAllText(Path.Combine(output, "translated-contour-" + shape + "-" + heading + ".json"), json);
        Assert.Equal(160d, saved.Nodes[0].Width!.Value); Assert.Equal(64d, saved.Nodes[0].Height!.Value);
        Assert.Equal(TopologyStaticPaintObservation.Paint(before.ToSvg()), TopologyStaticPaintObservation.Paint(after.ToSvg()));
        Assert.Equal(before.ToPng(), after.ToPng());
        Assert.Equal(chart.Id, saved.Id); Assert.Equal(TopologyLayoutMode.Manual, restored.LayoutMode);
        Assert.Equal(199.95, chart.Nodes[0].Y); Assert.Equal(shape, chart.Nodes[0].Shape);
    }

    internal static TopologyChart Source(TopologyNodeShape shape = TopologyNodeShape.Rounded, bool heading = true) {
        var chart = TopologyChart.Create().WithId("translated-native-contour").WithTitle(heading ? "Projection heading" : "")
            .WithLegend(null).WithViewport(692, 440, 28).WithLayout(TopologyLayoutMode.Manual)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family))
            .AddNode("node", "Node", 56, 199.95, width: 160, height: 64);
        var node = chart.Nodes[0]; node.Shape = shape; node.PreserveDisplayModeSize = true;
        node.ShowLabel = false; node.ShowStatusBadge = false; node.BackgroundColor = "#ececff"; node.BorderColor = "#9370db";
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeTitle = heading, IncludeLegend = false, FitContentToViewport = false });
        return chart;
    }

    internal static void Capture(string name, PreparedVisual value) {
        TopologyPortableFontObservation.Capture("translated-contour-" + name, value);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "translated-contour-" + name + ".scene.json"), JsonSerializer.Serialize(
            value.Scene.Nodes.OfType<VisualScenePath>().Where(mark => mark.Role == "topology-node-surface")
                .Select(mark => new { mark.StrokeWidth, mark.Commands })));
    }
}
