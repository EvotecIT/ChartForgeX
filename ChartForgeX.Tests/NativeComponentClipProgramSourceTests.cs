using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeComponentClipProgramSourceTests {
    [Theory] [InlineData("bubble", 390)] [InlineData("range", 760)] [InlineData("error", 390)] [InlineData("candlestick", 760)]
    public void PublicClippedComponentProgramsKeepActualRawValuesAndNativeTransitionOwner(string family, int width) {
        var program = ExampleRecipeSource.EmitNativeComponentClip(family, width, width == 390 ? VisualThemeMode.Light : VisualThemeMode.Dark); NativeRadialMotionBeforeControls.Save("component-clip-program-" + family + ".cs", program); Assert.DoesNotContain("{{", program, StringComparison.Ordinal);
        Assert.Contains("120d", program, StringComparison.Ordinal); Assert.Contains("CaptureNativeValueSource = true", program, StringComparison.Ordinal); Assert.Contains("PrepareNativeTransitionJson", program, StringComparison.Ordinal); Assert.Contains("FontRegistry.Register", program, StringComparison.Ordinal); Assert.DoesNotContain("requestAnimationFrame", program, StringComparison.Ordinal);
    }
}
