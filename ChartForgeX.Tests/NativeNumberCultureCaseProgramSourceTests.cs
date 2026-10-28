using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeNumberCultureCaseProgramSourceTests {
    [Theory] [InlineData("pie", "fr-number", 390, false)] [InlineData("donut", "sv-currency", 760, true)]
    [InlineData("pie", "ar-percent", 760, false)] [InlineData("donut", "fr-compact", 390, true)]
    [InlineData("treemap", "sentence", 390, true)] [InlineData("treemap", "toggle", 760, false)]
    [InlineData("pie", "literal-ms", 390, false)] [InlineData("donut", "sections", 760, true)]
    [InlineData("treemap", "scaled", 390, true)] [InlineData("pie", "permille", 760, false)]
    [InlineData("donut", "scientific", 390, true)] [InlineData("treemap", "escaped", 760, false)]
    [InlineData("donut", "literal-ms", 760, true)] [InlineData("pie", "sections", 390, false)]
    public void PublicProgramUsesTheRealNumericAndNativeCaptionOwnersWithExplicitHostFonts(string kind, string policy, int width, bool dark) {
        var program = ExampleRecipeSource.EmitNativeNumberCultureCase(kind, policy, width, dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        NativeRadialMotionBeforeControls.Save("number-culture-case-program-" + kind + "-" + policy + "-" + width + ".cs", program);
        Assert.DoesNotContain("{{", program, StringComparison.Ordinal); Assert.Contains("class NativeNumberCultureCaseRecipe", program, StringComparison.Ordinal);
        Assert.Contains("PrepareNativeTransitionJson", program, StringComparison.Ordinal); Assert.Contains("CultureInfo.GetCultureInfo", program, StringComparison.Ordinal); Assert.Contains("TextCaseTransform.SentenceCase", program, StringComparison.Ordinal);
        Assert.Contains("using ChartForgeX.Interactivity.Html;", program, StringComparison.Ordinal); Assert.DoesNotContain("OpenTypeTestFonts", program, StringComparison.Ordinal);
    }
}
