using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class WardleyRecipeSourceTests {
    [Theory]
    [InlineData("compact", VisualThemeMode.Light)] [InlineData("compact", VisualThemeMode.Dark)]
    [InlineData("wide", VisualThemeMode.Light)] [InlineData("wide", VisualThemeMode.Dark)]
    public void OrdinaryProgramCompilesTheSameNativeSourceFactory(string variant, VisualThemeMode mode) {
        var program = ExampleRecipeSource.EmitWardley(variant, mode); var map = WardleyDepthRecipe.Create(variant, mode);
        Assert.Contains("WardleyDepthRecipe.Create(\"" + variant + "\", mode)", program); Assert.Contains("public static class WardleyDepthRecipe", program);
        Assert.Contains("File.WriteAllText(\"visual.svg\", model.ToSvg());", program); Assert.Contains("File.WriteAllBytes(\"visual.png\", model.ToPng());", program);
        Assert.Contains("File.WriteAllText(\"visual.html\", model.ToHtmlPage());", program); Assert.DoesNotContain("{{", program); Assert.DoesNotContain("ChartForgeX.Mermaid", program);
        Assert.Equal(5, map.Nodes.Count); Assert.Equal(2, map.Annotations.Count); Assert.NotEmpty(map.ToPng()); Assert.Contains("wardley-annotation-box", map.ToSvg());
        Assert.Contains("fonts/Carlito-Regular.ttf", ExampleRecipeSource.EmitWardley(variant, mode, galleryTypography: true));
        Assert.Equal(File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "wardley-native-overlays.mmd")), File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Examples", "Assets", "Mermaid", "wardley-native-overlays.mmd")));
    }
}
