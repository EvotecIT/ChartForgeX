using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class BlockLayoutRecipeTests {
    [Theory]
    [InlineData("wide", VisualThemeMode.Light)]
    [InlineData("wide", VisualThemeMode.Dark)]
    [InlineData("compact", VisualThemeMode.Light)]
    [InlineData("compact", VisualThemeMode.Dark)]
    public void CompleteCoreProgramAndCompiledBlockFactoryRetainNativeHierarchy(string variant, VisualThemeMode mode) {
        var source = ExampleRecipeSource.EmitBlockLayout(variant, mode);
        Assert.Contains("public static class BlockLayoutDepthRecipe", source); Assert.Contains("BlockLayoutBlock.Create", source);
        Assert.Contains("model.ToSvg", source); Assert.Contains("model.ToPng", source); Assert.Contains("model.ToHtmlPage", source);
        Assert.DoesNotContain("ChartForgeX.Examples", source); Assert.DoesNotContain("{{", source);
        var block = BlockLayoutDepthRecipe.Create(variant, mode); Assert.Equal(mode, block.Options.ThemeMode);
        Assert.Equal(new[] { "services", "api", "worker", "storage", "queue", "data", "client" }, block.Items.Select(item => item.Id));
        Assert.Equal(7, XDocument.Parse(block.ToSvg()).Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "block-layout-node"));
        Assert.Equal(variant == "compact" ? 390 : 1000, block.RenderRgba().Width);
    }
}
