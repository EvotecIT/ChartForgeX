using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MixedCaptionRecipeSourceTests {
    [Theory]
    [InlineData("native", "compact", VisualThemeMode.Light)]
    [InlineData("native", "compact", VisualThemeMode.Dark)]
    [InlineData("native", "wide", VisualThemeMode.Light)]
    [InlineData("native", "wide", VisualThemeMode.Dark)]
    [InlineData("flowchart", "wide", VisualThemeMode.Dark)]
    [InlineData("requirement", "compact", VisualThemeMode.Light)]
    [InlineData("requirement", "compact", VisualThemeMode.Dark)]
    [InlineData("mindmap", "wide", VisualThemeMode.Light)]
    public void CompleteOrdinaryProgramUsesTheSameCompiledRecipeAndEditableNativeOwner(string family, string variant, VisualThemeMode mode) {
        var emitted = ExampleRecipeSource.EmitMixedCaptions(family, variant, mode);
        var sourcePath = family == "native"
            ? Path.Combine(TestRepository.Root, "ChartForgeX.Examples", "Recipes", "MixedCaptionRecipe.cs")
            : Path.Combine(TestRepository.Root, "ChartForgeX.Examples", "MermaidMixedCaptionSourceExample.cs");
        var source = File.ReadAllText(sourcePath).TrimEnd();
        Assert.Contains(source, emitted, StringComparison.Ordinal); Assert.DoesNotContain("{{", emitted, StringComparison.Ordinal);
        Assert.Contains("File.WriteAllText(\"caption.svg\"", emitted, StringComparison.Ordinal); Assert.Contains("File.WriteAllBytes(\"caption.png\"", emitted, StringComparison.Ordinal);
        if (family == "native") {
            Assert.DoesNotContain("ChartForgeX.Mermaid", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ChartForgeX.Mermaid", emitted, StringComparison.Ordinal);
            Assert.Contains("using ChartForgeX.Typography;", emitted, StringComparison.Ordinal);
            Assert.Contains("fonts/Carlito-Regular.ttf", emitted, StringComparison.Ordinal);
            Assert.Contains("fonts/Carlito-Bold.ttf", emitted, StringComparison.Ordinal);
            Assert.Contains("FontRegistry.Register(captionFamily", emitted, StringComparison.Ordinal);
            Assert.Contains("VisualThemeMode." + mode + ", captionFamily)", emitted, StringComparison.Ordinal);
        } else {
            Assert.Contains("ChartForgeX.Mermaid.MermaidRenderer.Render", source, StringComparison.Ordinal);
            Assert.Contains("MermaidMixedCaptionSourceExample.Create", emitted, StringComparison.Ordinal);
        }
        var artifact = family == "native"
            ? MixedCaptionRecipe.Create(variant, mode, BrowserProofFonts.Family)
            : MermaidMixedCaptionSourceExample.Create(family, variant, mode, BrowserProofFonts.Family);
        if (family == "requirement") {
            var prepared = Assert.IsType<TopologyChart>(artifact.Model).Prepare();
            Assert.Equal(variant == "compact" ? 390 : 1100, prepared.Width);
            var painted = prepared.Visual.Scene.Nodes.OfType<VisualSceneText>()
                .Where(text => text.Role == "inline-text-run").ToArray();
            var visibleRows = painted.GroupBy(text => text.Baseline).OrderBy(row => row.Key)
                .Select(row => string.Concat(row.OrderBy(text => text.X)
                    .SelectMany(text => text.Text.Lines).Select(line => line.Text)));
            Assert.Equal("text Review carefully and record accepted work", string.Join(" ", visibleRows));
            Assert.Contains(painted, text => text.Text.Style.Font.Weight >= 700
                && text.Text.Lines.Any(line => line.Text == "carefully"));
            Assert.Contains(painted, text => text.Text.Style.Font.Italic
                && text.Text.Lines.Any(line => line.Text == "record"));
        }
        NativeInlineContentTests.AssertResources(artifact.ToSvg()); Assert.NotEmpty(artifact.ToPng());
        var restored = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).ToTopologyChart();
        Assert.Contains(restored.Nodes, node => node.LabelContent.IsStructured || node.Details.Any(detail => detail.ValueContent.IsStructured));
    }
}
