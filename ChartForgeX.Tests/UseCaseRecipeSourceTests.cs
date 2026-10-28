using System.Text.Json;
using System.Text.RegularExpressions;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class UseCaseRecipeSourceTests {
    [Theory]
    [InlineData("wide", VisualThemeMode.Light)] [InlineData("wide", VisualThemeMode.Dark)]
    [InlineData("compact", VisualThemeMode.Light)] [InlineData("compact", VisualThemeMode.Dark)]
    public void NativeCoreAndParsedSourceRecipesExposeTheSameAuthoredGraphFactsThroughSeparatePublicConsumers(string variant, VisualThemeMode mode) {
        var native = UseCaseSourceDepthRecipe.Create(variant, mode);
        native.WithTheme(native.Theme.WithFontFamily(BrowserProofFonts.Family), mode);
        var source = UseCaseSourceDepthFixture.Read(variant == "compact");
        var parsed = new MermaidParser().ParseFlowchart(source).Document!.ToTopologyChart();
        Assert.Equal(parsed.Nodes.Select(node => (node.Id, node.Label, node.Subtitle, node.Shape, node.GroupId)),
            native.Nodes.Select(node => (node.Id, node.Label, node.Subtitle, node.Shape, node.GroupId)));
        Assert.Equal(parsed.Edges.Select(edge => (edge.SourceNodeId, edge.TargetNodeId, edge.Label, edge.Kind, edge.Direction)),
            native.Edges.Select(edge => (edge.SourceNodeId, edge.TargetNodeId, edge.Label, edge.Kind, edge.Direction)));
        Assert.Equal(parsed.Edges[0].DashPattern, native.Edges[0].DashPattern);
        Assert.Equal((parsed.Edges[0].Id, parsed.Edges[0].Color, parsed.Edges[0].LabelColor, parsed.Edges[0].StrokeWidth, parsed.Edges[0].Opacity),
            (native.Edges[0].Id, native.Edges[0].Color, native.Edges[0].LabelColor, native.Edges[0].StrokeWidth, native.Edges[0].Opacity));
        Assert.Equal(parsed.Annotations.Select(note => (note.TargetId, note.Text)), native.Annotations.Select(note => (note.TargetId, note.Text)));
        MermaidUseCaseSourceDepthTests.AssertPainted(native.Prepare());
        var coreProgram = ExampleRecipeSource.Emit(ExampleRecipes.Named("usecase-source-depth"), variant, "Use cases", "Native authoring", mode,
            variant == "compact" ? 390 : 900, variant == "compact" ? 620 : 520, artifactKind: "Topology");
        Assert.DoesNotContain("MermaidRenderer", coreProgram); Assert.DoesNotContain("ChartForgeX.Mermaid", coreProgram);
        var sourceProgram = ExampleRecipeSource.EmitUseCaseSource(variant, mode);
        var literal = Regex.Match(sourceProgram, @"var source = (.+);"); Assert.True(literal.Success);
        Assert.Equal(source, JsonSerializer.Deserialize<string>(literal.Groups[1].Value)!.ReplaceLineEndings("\n"));
        Assert.Contains("MermaidRenderer.Render(source", sourceProgram); Assert.DoesNotContain("{{", sourceProgram);
        Assert.Contains("fonts/Carlito-Regular.ttf", ExampleRecipeSource.EmitUseCaseSource(variant, mode, galleryTypography: true));
    }
}
