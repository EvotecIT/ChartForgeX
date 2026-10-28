using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeErrorComponentProgramSourceTests {
    [Theory] [InlineData("x", HtmlNativeTransitionEffect.Values, 390)] [InlineData("y", HtmlNativeTransitionEffect.InitialValues, 760)] [InlineData("xy", HtmlNativeTransitionEffect.Values, 760)] [InlineData("xy", HtmlNativeTransitionEffect.ValueFlash, 390)]
    public void PublicErrorProgramsUseRealNullableTypedBoundsAndExistingNativeTransitionEntry(string form, HtmlNativeTransitionEffect effect, int width) {
        var program = ExampleRecipeSource.EmitNativeErrorComponents(form, effect, width, width == 390 ? VisualThemeMode.Light : VisualThemeMode.Dark);
        NativeRadialMotionBeforeControls.Save("error-program-" + form + "-" + effect + "-" + width + ".cs", program); Assert.DoesNotContain("{{", program, StringComparison.Ordinal);
        Assert.Contains("ChartSeries.CreateErrorBar", program, StringComparison.Ordinal); Assert.Contains("new ChartErrorBounds", program, StringComparison.Ordinal); Assert.Contains("CaptureNativeValueSource = true", program, StringComparison.Ordinal);
        Assert.Contains("PrepareNativeTransitionJson", program, StringComparison.Ordinal); Assert.Contains("FontRegistry.Register", program, StringComparison.Ordinal); Assert.DoesNotContain("requestAnimationFrame", program, StringComparison.Ordinal);
    }
}
