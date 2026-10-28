using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ExampleRecipeTests {
    [Theory]
    [InlineData("gradient-colors", VisualThemeMode.Light)] [InlineData("gradient-colors", VisualThemeMode.Dark)]
    [InlineData("compact-gradient-colors", VisualThemeMode.Light)] [InlineData("compact-gradient-colors", VisualThemeMode.Dark)]
    public void EndpointGradientRecipeUsesOneCompiledSourceAndOrdinaryPackageProgram(string variant, VisualThemeMode mode) {
        var selection = new ExampleFamilySelection(ChartSeriesKind.Sankey, variant);
        Assert.Contains(selection, ExampleRecipes.FamilySelections(ChartSeriesKind.Sankey));
        Assert.Contains(selection, ExampleRecipes.FamilySelections(ChartSeriesKind.Sankey, curated: true));
        Assert.Contains(selection, ExampleRecipes.StandaloneFamilySelections());
        var recipe = selection.Recipe; Assert.Equal(nameof(SankeyPaintRecipe), recipe.Factory);
        var source = Source(recipe, variant); Assert.Contains("public static class SankeyPaintRecipe", source);
        Assert.Contains("ChartSankeyFlowColorMode.Gradient", source); Assert.Contains("ChartSankeyLabelStyle.Outlined", source);
        Assert.DoesNotContain("ChartForgeX.Examples", source); Assert.DoesNotContain("{{", source);
        var program = ExampleRecipeSource.EmitChart(recipe, "sankey-gradient", variant, mode);
        Assert.Contains("SaveSvg", program); Assert.Contains("SavePng", program); Assert.Contains("SaveHtml", program); Assert.DoesNotContain("{{", program);
        var chart = Assert.IsType<Chart>(recipe.Create(variant, mode)); var width = variant.StartsWith("compact-", StringComparison.Ordinal) ? 390 : 1100;
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, 460)), VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode);
        var prepared = chart.Prepare(context); MermaidSankeyPaintTests.Capture("recipe-sankey-" + variant + "-" + mode, prepared);
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualSceneGradient>().Count(node => node.Role == "sankey-ribbon"));
        Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "sankey-ribbon");
        Assert.Equal(new[] { 60d, 20d, 80d }, chart.Series[0].FlowLinks.Select(link => link.Value));
        var image = RasterImageDecoder.Decode(prepared.ToPng()); Assert.Equal(width, image.Width); Assert.Equal(460, image.Height);
        Assert.DoesNotContain(XDocument.Parse(prepared.ToSvg()).Descendants(), node => node.Name.LocalName is "script" or "image" or "foreignObject");
    }
}
