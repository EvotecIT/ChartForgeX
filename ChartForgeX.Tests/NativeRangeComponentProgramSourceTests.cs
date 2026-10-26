using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeRangeComponentProgramSourceTests {
    [Theory] [InlineData(false, false, 390)] [InlineData(false, true, 760)] [InlineData(true, false, 760)] [InlineData(true, true, 390)]
    public void PublicRangeProgramsUseTypedIntervalFactoriesNativeValuesAndCallerFonts(bool initial, bool area, int width) {
        var program = ExampleRecipeSource.EmitNativeRangeComponents(initial, area, width, width == 390 ? VisualThemeMode.Light : VisualThemeMode.Dark);
        NativeRadialMotionBeforeControls.Save("range-program-" + initial + "-" + area + "-" + width + ".cs", program);
        Assert.DoesNotContain("{{", program, StringComparison.Ordinal); Assert.Contains("ChartSeries.CreateRangeArea", program, StringComparison.Ordinal); Assert.Contains("ChartSeries.CreateRangeBand", program, StringComparison.Ordinal);
        Assert.Contains("CaptureNativeValueSource = true", program, StringComparison.Ordinal); Assert.Contains("PrepareNativeTransitionJson", program, StringComparison.Ordinal); Assert.Contains("FontRegistry.Register", program, StringComparison.Ordinal);
        Assert.Contains("HtmlNativeTransitionEffect.ValueFlash", program, StringComparison.Ordinal); Assert.Contains("output/visual.flash.html", program, StringComparison.Ordinal);
        Assert.Contains("output/visual.flash.transition.json", program, StringComparison.Ordinal);
        Assert.DoesNotContain("WithSizeDomain", program, StringComparison.Ordinal); Assert.DoesNotContain("requestAnimationFrame", program, StringComparison.Ordinal);
    }
}
