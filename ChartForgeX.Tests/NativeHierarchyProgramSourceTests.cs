using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeHierarchyProgramSourceTests {
    [Theory] [InlineData("treemap", "values", 390, false)] [InlineData("sunburst-leaf", "initial", 760, true)]
    [InlineData("treemap", "ellipsis", 390, false)] [InlineData("treemap", "ellipsis", 760, true)]
    [InlineData("sunburst-total", "subtree", 390, true)] [InlineData("sunburst-total", "root", 760, false)]
    public void PublicProgramUsesCompiledCoreHierarchyAndExistingAdapterCapture(string family, string intent, int width, bool dark) {
        var program = ExampleRecipeSource.EmitNativeHierarchyMotion(family, intent, width, dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        NativeRadialMotionBeforeControls.Save("hierarchy-program-" + family + "-" + intent + "-" + width + ".cs", program);
        Assert.DoesNotContain("{{", program, StringComparison.Ordinal); Assert.Contains("class NativeHierarchyMotionRecipe", program, StringComparison.Ordinal);
        Assert.Contains("PrepareNativeTransitionJson", program, StringComparison.Ordinal); Assert.Contains("ChartVisibilityTarget.HierarchyItem", program, StringComparison.Ordinal);
        Assert.Contains("ChartHierarchyView", program, StringComparison.Ordinal); Assert.Contains("using ChartForgeX.Interactivity.Html;", program, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenTypeTestFonts", program, StringComparison.Ordinal);
    }
}
