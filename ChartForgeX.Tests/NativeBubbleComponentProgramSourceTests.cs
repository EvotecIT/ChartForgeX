using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeBubbleComponentProgramSourceTests {
    [Theory] [InlineData(false, false, 390)] [InlineData(false, true, 760)] [InlineData(true, false, 760)] [InlineData(true, true, 390)]
    public void PublicTypedComponentProgramsKeepNativeFactoriesFixedDomainsAndOwnedFont(bool initial, bool area, int width) {
        var program = ExampleRecipeSource.EmitNativeBubbleComponents(initial, area, width == 760, width, width == 390 ? VisualThemeMode.Light : VisualThemeMode.Dark);
        NativeRadialMotionBeforeControls.Save("bubble-program-" + initial + "-" + area + "-" + width + ".cs", program);
        Assert.DoesNotContain("{{", program, StringComparison.Ordinal); Assert.Contains("ChartSeries.CreateBubble", program, StringComparison.Ordinal);
        Assert.Contains("WithSizeDomain(0, 100)", program, StringComparison.Ordinal); Assert.Contains("CaptureNativeValueSource = true", program, StringComparison.Ordinal);
        Assert.Contains("PrepareNativeTransitionJson", program, StringComparison.Ordinal); Assert.Contains("FontRegistry.Register", program, StringComparison.Ordinal);
        Assert.DoesNotContain("requestAnimationFrame", program, StringComparison.Ordinal);
    }
}
