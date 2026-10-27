using System.Text.Json;
using System.Text.RegularExpressions;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class UseCaseAdvancedActorRecipeTests {
    [Theory]
    [InlineData("wide", VisualThemeMode.Light)] [InlineData("wide", VisualThemeMode.Dark)]
    [InlineData("compact", VisualThemeMode.Light)] [InlineData("compact", VisualThemeMode.Dark)]
    public void OrdinaryCoreRecipeAndSharedParsedProgramRetainTheSameActorKindsStereotypesAndLinkedIdentity(string variant, VisualThemeMode mode) {
        var source = UseCaseAdvancedActorFixture.Read(variant == "compact"); var parsed = new MermaidParser().ParseFlowchart(source); Assert.Empty(parsed.Diagnostics);
        var lowered = parsed.Document!.ToTopologyChart(); var native = UseCaseAdvancedActorsRecipe.Create(variant, mode);
        Assert.Equal(lowered.Nodes.Select(n => (n.Id, n.Label, n.Subtitle, n.Kind, n.Shape, n.IconShape, n.IconId)), native.Nodes.Select(n => (n.Id, n.Label, n.Subtitle, n.Kind, n.Shape, n.IconShape, n.IconId)));
        Assert.Equal(lowered.Edges.Select(e => (e.Id, e.SourceNodeId, e.TargetNodeId, e.Kind, e.Direction)), native.Edges.Select(e => (e.Id, e.SourceNodeId, e.TargetNodeId, e.Kind, e.Direction)));
        Assert.Equal(lowered.Annotations.Select(n => (n.TargetId, n.Text)), native.Annotations.Select(n => (n.TargetId, n.Text)));
        native.WithTheme(native.Theme.WithFontFamily(BrowserProofFonts.Family), mode);
        MermaidUseCaseAdvancedActorTests.AssertPaint(native.ToSvg()); Assert.NotEmpty(native.ToPng());
        var emitted = ExampleRecipeSource.Emit(ExampleRecipes.Named("usecase-advanced-actors"), variant, "Native actors", "Finite native authoring", mode,
            variant == "compact" ? 390 : 1100, variant == "compact" ? 1160 : 680, artifactKind: "Topology");
        Assert.DoesNotContain("ChartForgeX.Mermaid", emitted); Assert.DoesNotContain("MermaidRenderer", emitted); Assert.DoesNotContain("{{", emitted);
        var program = ExampleRecipeSource.EmitAdvancedActorSource(variant, mode); var literal = Regex.Match(program, @"var source = (.+);"); Assert.True(literal.Success);
        Assert.Equal(source, JsonSerializer.Deserialize<string>(literal.Groups[1].Value)); Assert.Contains("MermaidRenderer.Render(source", program); Assert.DoesNotContain("{{", program);
        Assert.Contains("fonts/Carlito-Regular.ttf", ExampleRecipeSource.EmitAdvancedActorSource(variant, mode, galleryTypography: true));
    }

    [Theory]
    [InlineData("compact", VisualThemeMode.Light)] [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)] [InlineData("wide", VisualThemeMode.Dark)]
    public void OrdinaryNativeSampleKeepsAuthoredGeometryInsteadOfShrinkingItsActorsToTheHostWidth(string variant, VisualThemeMode mode) {
        var model = UseCaseAdvancedActorsRecipe.Create(variant, mode);
        model.WithTheme(model.Theme.WithFontFamily(BrowserProofFonts.Family), mode);
        var prepared = model.Prepare(); var facts = prepared.ToInterchangeEnvelope();
        if (Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var name = Path.Combine(output, "ordinary-readable-actors-" + variant + "-" + mode);
            File.WriteAllText(name + ".svg", prepared.ToSvg()); File.WriteAllBytes(name + ".png", prepared.ToPng());
            File.WriteAllText(name + ".json", facts.ToJson());
        }
        Assert.False(model.DefaultRenderOptions!.FitContentToViewport);
        foreach (var source in model.Nodes) {
            var node = Assert.Single(facts.Nodes, item => item.Id == source.Id);
            Assert.Equal(source.Width, node.Width!.Value, 6); Assert.Equal(source.Height, node.Height!.Value, 6);
            Assert.InRange(node.X!.Value, 0, prepared.Width - node.Width.Value);
            Assert.InRange(node.Y!.Value, 0, prepared.Height - node.Height.Value);
        }
    }
}
