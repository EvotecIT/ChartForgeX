using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ExampleRecipeTests {
    [Theory]
    [InlineData("wide", VisualThemeMode.Light)]
    [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("meet", VisualThemeMode.Light)]
    [InlineData("slice", VisualThemeMode.Dark)]
    public void OrganizationPortraitRecipeContainsCompleteEmbeddedOrdinarySourceAndNativeMedia(string variant, VisualThemeMode mode) {
        var recipe = ExampleRecipes.Named("org-portraits"); var source = Source(recipe, variant);
        Assert.Equal(nameof(OrgPortraitsRecipe), recipe.Factory); Assert.Contains("public static class OrgPortraitsRecipe", source);
        Assert.Contains("TopologyIconArtwork.Portrait", source); Assert.DoesNotContain("ChartForgeX.Examples", source); Assert.DoesNotContain("{{", source);
        var chart = Assert.IsType<TopologyChart>(recipe.Create(variant, mode)); Assert.Equal(mode, chart.ThemeMode);
        Assert.Equal(new[] { "avery", "jordan", "morgan" }, chart.Nodes.Select(node => node.Id));
        Assert.All(chart.Nodes, node => { Assert.NotNull(node.Artwork); Assert.StartsWith("data:image/png;base64,", node.Artwork!.ImageHref!); });
        Assert.Equal(2, chart.Edges.Count);
        var width = variant == "compact" ? 390 : 900;
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 480)),
            VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode));
        Assert.Equal(3, prepared.Scene.Nodes.Count(node => node.Role == "topology-node-artwork"));
        Assert.Equal(3, prepared.Regions.Count(region => region.Role == "topology-node")); Assert.NotEmpty(prepared.ToSvg()); Assert.NotEmpty(prepared.ToPng());
    }
}
