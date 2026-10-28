using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeFinancialComponentProgramSourceTests {
    [Theory] [InlineData(false, HtmlNativeTransitionEffect.Values, 390)] [InlineData(true, HtmlNativeTransitionEffect.InitialValues, 760)] [InlineData(false, HtmlNativeTransitionEffect.ValueFlash, 760)] [InlineData(true, HtmlNativeTransitionEffect.ValueFlash, 390)]
    public void PublicFinancialProgramsKeepNativeTypedPricesDirectionsAndExistingTransitionLifetime(bool ohlc, HtmlNativeTransitionEffect effect, int width) {
        var program = ExampleRecipeSource.EmitNativeFinancialComponents(ohlc, effect, width, width == 390 ? VisualThemeMode.Light : VisualThemeMode.Dark);
        NativeRadialMotionBeforeControls.Save("financial-program-" + ohlc + "-" + effect + "-" + width + ".cs", program); Assert.DoesNotContain("{{", program, StringComparison.Ordinal);
        Assert.Contains("new ChartCandlestick", program, StringComparison.Ordinal); Assert.Contains("ConfigureFinancialValue", program, StringComparison.Ordinal); Assert.Contains("CaptureNativeValueSource = true", program, StringComparison.Ordinal);
        Assert.Contains("PrepareNativeTransitionJson", program, StringComparison.Ordinal); Assert.Contains("FontRegistry.Register", program, StringComparison.Ordinal); Assert.DoesNotContain("requestAnimationFrame", program, StringComparison.Ordinal);
    }
}
