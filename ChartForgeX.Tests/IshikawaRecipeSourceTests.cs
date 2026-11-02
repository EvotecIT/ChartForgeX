using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class IshikawaRecipeSourceTests {
    [Theory]
    [InlineData("wide", VisualThemeMode.Light)] [InlineData("wide", VisualThemeMode.Dark)]
    [InlineData("compact", VisualThemeMode.Light)] [InlineData("compact", VisualThemeMode.Dark)]
    public void CompleteOrdinaryProgramUsesTheCompiledCoreFishboneFactory(string variant, VisualThemeMode mode) {
        var program = ExampleRecipeSource.EmitIshikawa(variant, mode); var block = IshikawaDepthRecipe.Create(variant, mode);
        Assert.Contains("IshikawaDepthRecipe.Create(\"" + variant + "\", mode)", program);
        Assert.Contains("File.WriteAllText(\"fishbone.svg\", model.ToSvg());", program); Assert.Contains("File.WriteAllBytes(\"fishbone.png\", model.ToPng());", program);
        Assert.Contains("File.WriteAllText(\"fishbone.html\", artifact.ToHtmlPage());", program); Assert.Contains("public static class IshikawaDepthRecipe", program);
        Assert.Contains("artifact.HtmlSizing = VisualArtifactHtmlSizing.PreserveSize;", program); Assert.DoesNotContain("{{", program); Assert.DoesNotContain("ChartForgeX.Mermaid", program); Assert.DoesNotContain("FontRegistry.Register", program);
        Assert.Equal(28, FishboneNaturalLayoutTests.Facts(block).Count); Assert.Contains(".WithNaturalSize(", program); Assert.NotEmpty(block.ToPng());
        var exact = ExampleRecipeSource.EmitIshikawa(variant, mode, galleryTypography: true);
        Assert.Contains("fonts/Carlito-Regular.ttf", exact); Assert.Contains("fonts/Carlito-Bold.ttf", exact);
        Assert.Contains(".WithFontFamily(\"CFX Proof Carlito\")", exact);
        var root = TestRepository.Root; Assert.Equal(File.ReadAllText(Path.Combine(root, "tests", "mermaid-conformance", "fixtures", "ishikawa-native-depth.mmd")),
            File.ReadAllText(Path.Combine(root, "ChartForgeX.Examples", "Assets", "Mermaid", "ishikawa-native-depth.mmd")));
    }
}
