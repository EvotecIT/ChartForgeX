using System.Xml.Linq;
using ChartForgeX.Topology;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyTableRecipeSourceTests {
    [Theory]
    [InlineData("wide", VisualThemeMode.Light)] [InlineData("wide", VisualThemeMode.Dark)]
    [InlineData("compact", VisualThemeMode.Light)] [InlineData("compact", VisualThemeMode.Dark)]
    public void NativeRecipeAndCompleteParsedSourceProgramKeepOrdinaryPackageBoundaries(string variant, VisualThemeMode mode) {
        var native = TopologyTableContentRecipe.Create(variant, mode);
        Assert.Equal("Release coordinator".Length, native.Nodes.Single(node => node.Id == "User").MaximumLabelCharacters);
        FullActorCaption(native.ToSvg());
        var original = native.ToVisualArtifact().ToInterchangeJson(); var edited = TopologyTableContentRecipe.LoadAndEdit(original);
        Assert.Contains("2.00", edited.ToSvg()); Assert.Equal("1.00", native.Nodes.Single(node => node.Id == "Details").TableContent!.Rows[0].Cells[1].DisplayText);
        var recipe = ExampleRecipes.Named("topology-table-content");
        var code = ExampleRecipeSource.Emit(recipe, variant, "Approve a captured release", "The source table remains part of its graph vertex", mode,
            variant == "compact" ? 390 : 1000, variant == "compact" ? 1000 : 680, false, "Topology");
        Assert.Contains("public static TopologyChart LoadAndEdit", code); Assert.Contains("ToTopologyChart()", code);
        Assert.DoesNotContain("ChartForgeX.Mermaid", code); Assert.DoesNotContain("ProjectReference", code);
        var source = UseCaseJsonTableFixture.Read(); var parsed = MermaidUseCaseJsonSourceExample.Create(source, variant, mode);
        Assert.Contains("1.00", parsed.ToSvg()); Assert.NotEmpty(parsed.ToPng());
        FullActorCaption(parsed.ToSvg());
        var sourceProgram = ExampleRecipeSource.EmitUseCaseJson(variant, mode);
        Assert.Contains("MermaidUseCaseJsonSourceExample.Create", sourceProgram); Assert.Contains("MermaidRenderer.Render", sourceProgram);
        Assert.Contains("json Details", sourceProgram); Assert.Contains("ToInterchangeJson()", sourceProgram);
        Assert.DoesNotContain("MermaidFixtureSources", sourceProgram); Assert.DoesNotContain("ProjectReference", sourceProgram);
        Assert.Equal(parsed.ToSvg(), MermaidUseCaseJsonSourceExample.Create(source, variant, mode).ToSvg());
    }
    private static void FullActorCaption(string svg) {
        var actor = XDocument.Parse(svg).Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "topology-node" && (string?)node.Attribute("data-node-id") == "User");
        var painted = string.Join(" ", actor.Descendants().Where(node => node.Name.LocalName == "text").Select(node => node.Value));
        Assert.Contains("Release", painted); Assert.Contains("coordinator", painted); Assert.DoesNotContain("...", painted);
    }
}
