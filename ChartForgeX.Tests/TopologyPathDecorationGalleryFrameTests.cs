using System.Text.Json;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyPathDecorationGalleryFrameTests {
    private const string Title = "Independent maintenance boundary";
    private const string Subtitle = "One native path decorates the graph without adding source nodes or relationships";
    [Theory]
    [InlineData("wide", false)] [InlineData("wide", true)]
    [InlineData("plain", false)] [InlineData("plain", true)]
    [InlineData("compact", false)] [InlineData("compact", true)]
    [InlineData("compact-plain", false)] [InlineData("compact-plain", true)]
    public void TheActualRecipeAndDownloadFrameAdmitEveryVariantWithoutChangingAuthoredWideInk(string variant, bool dark) {
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light; var chart = TopologyPathDecorationRecipe.Create(variant, mode);
        var compact = variant.StartsWith("compact", StringComparison.Ordinal); var width = compact ? 390 : 760; var height = variant == "wide" ? 420 : 400;
        var input = chart.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)).ToArray();
        var prepared = chart.Prepare(Context(width, height, mode)); Capture(variant + "-" + dark, prepared);
        Assert.Equal(width, prepared.Size.Width); Assert.Equal(height, prepared.Size.Height); Assert.Equal(input, chart.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)));
        Assert.Equal(compact ? 390 : 760, chart.Viewport.Width); Assert.Equal(400, chart.Viewport.Height);
        Assert.Equal(2, chart.Nodes.Count); Assert.Single(chart.Edges);
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneText>().SelectMany(mark => mark.Text.Lines), line => line.Text.Contains("maintenance", StringComparison.Ordinal));
        if (variant.EndsWith("plain", StringComparison.Ordinal)) Assert.Empty(chart.PathDecorations);
        else {
            Assert.Single(chart.PathDecorations); var ink = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), mark => mark.Role == "topology-path-decoration-surface");
            if (!compact) { Assert.Equal(3, ink.StrokeWidth); Assert.Equal(new[] { 8d, 4d }, ink.Dash); }
        }
        if (!compact) Assert.DoesNotContain(prepared.Diagnostics, item => item.Code == "topology.content-fitted");
        Assert.NotEmpty(prepared.ToPng());
    }
    [Fact]
    public void TheFormerFixedGalleryRequestRemainsAnHonestAdmissionFailureRatherThanImplicitlyFitting() {
        var chart = TopologyPathDecorationRecipe.Create("wide");
        var failure = Record.Exception(() => chart.Prepare(Context(760, 400, VisualThemeMode.Light)));
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "path-decoration-old-fixed-frame.failure.json"),
                JsonSerializer.Serialize(new { Type = failure?.GetType().FullName, failure?.Message }));
        }
        var error = Assert.IsType<NotSupportedException>(failure); Assert.Contains("fixed viewport", error.Message, StringComparison.Ordinal);
        Assert.Null(chart.DefaultRenderOptions); Assert.Equal(400, chart.Viewport.Height);
    }
    private static VisualRenderContext Context(int width, int height, VisualThemeMode mode) => new(
        new VisualLayoutOptions(new VisualSize(width, height)), VisualTheme.Graphite(), mode,
        new VisualFrame(Title, Subtitle, showLegend: null), FontSpec.FromFamily(BrowserProofFonts.Family));
    private static void Capture(string name, PreparedVisual prepared) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var stem = Path.Combine(output, "path-decoration-gallery-frame-" + name);
        File.WriteAllText(stem + ".svg", prepared.ToSvg()); File.WriteAllBytes(stem + ".png", prepared.ToPng());
        File.WriteAllText(stem + ".json", prepared.ToArtifact().ToInterchangeJson());
    }
}
