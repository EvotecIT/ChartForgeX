using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeFallbackProgramSourceTests {
    [Theory] [InlineData(false, 390)] [InlineData(true, 760)]
    public void NormalConsumerProgramOwnsItsExplicitFontInputsAndReusesTheNativeRecipe(bool donut, int width) {
        var program = ExampleRecipeSource.EmitNativeFallbackRadial(donut, width, VisualThemeMode.Light);
        NativeRadialMotionBeforeControls.Save("fallback-program-" + donut + "-" + width + ".cs", program);
        Assert.DoesNotContain("{{", program, StringComparison.Ordinal); Assert.Contains("class NativeRadialMotionRecipe", program, StringComparison.Ordinal);
        Assert.Contains("FontRegistry.Register", program, StringComparison.Ordinal); Assert.Contains("Asia 世界", program, StringComparison.Ordinal);
        Assert.Contains("PrepareNativeTransitionJson", program, StringComparison.Ordinal); Assert.DoesNotContain("OpenTypeTestFonts", program, StringComparison.Ordinal);
    }
}
