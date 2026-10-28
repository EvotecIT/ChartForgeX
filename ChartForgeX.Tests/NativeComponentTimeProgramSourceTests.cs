using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeComponentTimeProgramSourceTests {
    [Theory] [InlineData("candlestick", "elapsed", true, 390)] [InlineData("ohlc", "sessions", false, 760)] [InlineData("bubble", "ordinal", false, 390)] [InlineData("range", "calendar", true, 760)]
    public void PublicTimeComponentProgramsUseActualTypedDatesAndNativeTimePolicyOwners(string family, string policy, bool initial, int width) {
        var program = ExampleRecipeSource.EmitNativeComponentTime(family, policy, initial, width, width == 390 ? VisualThemeMode.Light : VisualThemeMode.Dark);
        NativeRadialMotionBeforeControls.Save("time-component-program-" + family + "-" + policy + ".cs", program); Assert.DoesNotContain("{{", program, StringComparison.Ordinal);
        Assert.Contains("DateTimeKind.Utc", program, StringComparison.Ordinal); Assert.Contains("WithXAxisTimeScale", program, StringComparison.Ordinal); Assert.Contains("CaptureNativeValueSource = true", program, StringComparison.Ordinal);
        Assert.Contains("PrepareNativeTransitionJson", program, StringComparison.Ordinal); Assert.Contains("FontRegistry.Register", program, StringComparison.Ordinal); Assert.DoesNotContain("requestAnimationFrame", program, StringComparison.Ordinal);
    }
}
